using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml.Linq;

namespace ProjetoINT2026.Services;

public interface IHealthContentService
{
	Task<IReadOnlyList<HealthPost>> GetPostsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default);
}

public sealed partial class HealthContentService(HttpClient httpClient) : IHealthContentService
{
	private const string CacheKey = "home_health_feed_cache";
	private int refreshCounter;

	private static readonly FeedSource[] RssSources =
	[
		new("Medical Xpress", "https://medicalxpress.com/rss-feed/"),
		new("Medical Xpress", "https://medicalxpress.com/rss-feed/critical-care-medicine-news/"),
		new("ScienceDaily", "https://www.sciencedaily.com/rss/top/health.xml"),
		new("Nursing Times", "https://www.nursingtimes.net/feed/"),
		new("Drugs.com", "https://www.drugs.com/feeds/headline_news.xml"),
		new("Ministerio da Saude", "https://www.gov.br/saude/pt-br/assuntos/noticias/agencia-saude/RSS"),
		new("NIH", "https://www.nih.gov/news-events/news-releases/feed"),
		new("OMS", "https://www.who.int/rss-feeds/news-english.xml")
	];

	private static readonly string[] MedlinePlusTerms =
	[
		"nursing care",
		"vital signs",
		"wound care",
		"medication safety"
	];

	public async Task<IReadOnlyList<HealthPost>> GetPostsAsync(bool forceRefresh = false, CancellationToken cancellationToken = default)
	{
		var cached = LoadCache();
		if (!forceRefresh && cached.Count > 0)
		{
			return SelectFeedWindow(cached.Select(EnsurePostImage).ToList(), false);
		}

		using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutSource.CancelAfter(TimeSpan.FromSeconds(6));

		IEnumerable<HealthPost> posts = [];
		try
		{
			var feedTasks = RssSources
				.Select(source => TryReadRssAsync(source, timeoutSource.Token))
				.Concat(MedlinePlusTerms.Select(term => TryReadMedlinePlusAsync(term, timeoutSource.Token)));

			var feedResults = await Task.WhenAll(feedTasks).WaitAsync(TimeSpan.FromSeconds(7), cancellationToken);
			posts = feedResults.SelectMany(result => result);
		}
		catch
		{
			posts = [];
		}

		var candidates = posts
			.GroupBy(post => post.Url)
			.Select(group => group.First())
			.Select(EnsurePostImage)
			.OrderByDescending(post => post.PublishedAt)
			.Take(48)
			.ToList();

		if (candidates.Count > 0)
		{
			SaveCache(candidates);
			return SelectFeedWindow(candidates, forceRefresh);
		}

		return cached.Count > 0 ? SelectFeedWindow(cached.Select(EnsurePostImage).ToList(), forceRefresh) : GetFallbackPosts();
	}

	private async Task<IReadOnlyList<HealthPost>> TryReadRssAsync(FeedSource source, CancellationToken cancellationToken)
	{
		try
		{
			var xml = await httpClient.GetStringAsync(source.Url, cancellationToken);
			var doc = XDocument.Parse(xml);

			var posts = doc.Descendants()
				.Where(element => element.Name.LocalName is "item" or "entry")
				.Select(item => new HealthPost(
					CleanText(GetElementValue(item, "title")),
					CleanText(GetElementValue(item, "description") ?? GetElementValue(item, "summary") ?? GetElementValue(item, "encoded")),
					source.Name,
					NormalizeUrl(GetLinkValue(item), source.Url),
					ParseDate(GetElementValue(item, "pubDate") ?? GetElementValue(item, "published") ?? GetElementValue(item, "updated") ?? GetElementValue(item, "date")),
					NormalizeUrl(ExtractImageUrl(item), source.Url),
					IsVideoItem(item)))
				.Where(post => !string.IsNullOrWhiteSpace(post.Title) && !string.IsNullOrWhiteSpace(post.Url))
				.Take(8)
				.ToList();

			return posts
				.Select(EnsurePostImage)
				.ToList();
		}
		catch
		{
			return [];
		}
	}

	private async Task<IReadOnlyList<HealthPost>> TryReadMedlinePlusAsync(string term, CancellationToken cancellationToken)
	{
		try
		{
			var url = $"https://wsearch.nlm.nih.gov/ws/query?db=healthTopics&term={Uri.EscapeDataString(term)}";
			var xml = await httpClient.GetStringAsync(url, cancellationToken);
			var doc = XDocument.Parse(xml);

			return doc.Descendants("document")
				.Select(document =>
				{
					var title = GetContent(document, "title");
					var summary = GetContent(document, "snippet");
					var link = document.Attribute("url")?.Value ?? GetContent(document, "url");

					return new HealthPost(
						CleanText(title),
						CleanText(summary),
						"MedlinePlus",
						CleanText(link),
						DateTimeOffset.Now,
						PickFallbackImage(title, summary),
						false);
				})
				.Where(post => !string.IsNullOrWhiteSpace(post.Title) && !string.IsNullOrWhiteSpace(post.Url))
				.Take(2)
				.ToList();
		}
		catch
		{
			return [];
		}
	}

	private static string GetContent(XElement document, string name)
	{
		return document
			.Elements("content")
			.FirstOrDefault(element => string.Equals(element.Attribute("name")?.Value, name, StringComparison.OrdinalIgnoreCase))
			?.Value ?? string.Empty;
	}

	private static string? GetElementValue(XElement item, string name)
	{
		return item.Elements().FirstOrDefault(element => element.Name.LocalName == name)?.Value;
	}

	private static string GetLinkValue(XElement item)
	{
		var linkElement = item.Elements().FirstOrDefault(element => element.Name.LocalName == "link");
		return linkElement?.Attribute("href")?.Value ?? linkElement?.Value ?? string.Empty;
	}

	private static string ExtractImageUrl(XElement item)
	{
		var mediaUrl = item.Descendants()
			.FirstOrDefault(element => element.Name.LocalName is "content" or "thumbnail" &&
				(element.Name.NamespaceName.Contains("search.yahoo.com/mrss", StringComparison.OrdinalIgnoreCase) ||
				 element.Name.NamespaceName.Contains("media", StringComparison.OrdinalIgnoreCase)))
			?.Attribute("url")?.Value;

		if (!string.IsNullOrWhiteSpace(mediaUrl))
		{
			return mediaUrl;
		}

		var enclosureUrl = item.Elements("enclosure")
			.FirstOrDefault(element => element.Attribute("type")?.Value.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
			?.Attribute("url")?.Value;

		if (!string.IsNullOrWhiteSpace(enclosureUrl))
		{
			return enclosureUrl;
		}

		var html = string.Join(" ", item.Elements()
			.Where(element => element.Name.LocalName is "description" or "summary" or "encoded" or "content")
			.Select(element => element.Value));
		var match = ImageSourceRegex().Match(html ?? string.Empty);
		return match.Success ? WebUtility.HtmlDecode(match.Groups["src"].Value) : string.Empty;
	}

	private static bool IsVideoItem(XElement item)
	{
		var text = $"{item.Element("title")?.Value} {item.Element("description")?.Value}";
		return text.Contains("video", StringComparison.OrdinalIgnoreCase) ||
			item.Elements("enclosure").Any(element => element.Attribute("type")?.Value.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true);
	}

	private static DateTimeOffset ParseDate(string? value)
	{
		return DateTimeOffset.TryParse(value, out var parsed) ? parsed : DateTimeOffset.Now;
	}

	private static string CleanText(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var decoded = WebUtility.HtmlDecode(value);
		var withoutTags = HtmlTagRegex().Replace(decoded, string.Empty);
		return WhitespaceRegex().Replace(withoutTags, " ").Trim();
	}

	private static string NormalizeUrl(string? value, string baseUrl)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return string.Empty;
		}

		var url = value.Trim();
		if (url.StartsWith("//", StringComparison.Ordinal))
		{
			return $"https:{url}";
		}

		if (Uri.TryCreate(url, UriKind.Absolute, out _))
		{
			return url;
		}

		return Uri.TryCreate(new Uri(baseUrl), url, out var absoluteUri) ? absoluteUri.ToString() : url;
	}

	private static HealthPost EnsurePostImage(HealthPost post)
	{
		return post.HasImage ? post : post with { ImageUrl = PickFallbackImage(post.Title, post.Summary) };
	}

	private static string PickFallbackImage(string title, string? summary = null)
	{
		var text = $"{title} {summary}".ToLowerInvariant();

		if (HasAny(text, "medic", "drug", "pharma", "dose", "insulin", "vaccine", "remedio", "medicamento"))
		{
			return "https://images.unsplash.com/photo-1587854692152-cbe660dbde88?auto=format&fit=crop&w=900&q=80";
		}

		if (HasAny(text, "wound", "injur", "surgery", "infection", "ferida", "curativo", "cirurgia"))
		{
			return "https://images.unsplash.com/photo-1581595220892-b0739db3ba8c?auto=format&fit=crop&w=900&q=80";
		}

		if (HasAny(text, "heart", "blood pressure", "cardio", "vital", "pressao", "sinais vitais"))
		{
			return "https://images.unsplash.com/photo-1584982751601-97dcc096659c?auto=format&fit=crop&w=900&q=80";
		}

		if (HasAny(text, "respir", "lung", "oxygen", "asthma", "pneumonia", "pulmonary"))
		{
			return "https://images.unsplash.com/photo-1588776814546-1ffcf47267a5?auto=format&fit=crop&w=900&q=80";
		}

		if (HasAny(text, "mental", "psych", "stress", "depression", "anxiety"))
		{
			return "https://images.unsplash.com/photo-1505751172876-fa1923c5c528?auto=format&fit=crop&w=900&q=80";
		}

		return "https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=900&q=80";
	}

	private static bool HasAny(string text, params string[] terms)
	{
		return terms.Any(text.Contains);
	}

	private IReadOnlyList<HealthPost> SelectFeedWindow(IReadOnlyList<HealthPost> posts, bool rotate)
	{
		if (posts.Count <= 18)
		{
			return posts;
		}

		var offset = rotate ? Interlocked.Increment(ref refreshCounter) * 7 : 0;
		return posts
			.Skip(offset % posts.Count)
			.Concat(posts.Take(offset % posts.Count))
			.Take(18)
			.ToList();
	}

	private static void SaveCache(IReadOnlyList<HealthPost> posts)
	{
		Preferences.Default.Set(CacheKey, JsonSerializer.Serialize(posts));
	}

	private static IReadOnlyList<HealthPost> LoadCache()
	{
		var json = Preferences.Default.Get(CacheKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			return [];
		}

		try
		{
			return JsonSerializer.Deserialize<List<HealthPost>>(json) ?? [];
		}
		catch
		{
			return [];
		}
	}

	private static IReadOnlyList<HealthPost> GetFallbackPosts()
	{
		return
		[
			new("Seguranca na administracao de medicamentos", "Revise os certos da administracao e registre eventos relevantes durante o cuidado.", "Florence", "https://medlineplus.gov/", DateTimeOffset.Now, "https://images.unsplash.com/photo-1587854692152-cbe660dbde88?auto=format&fit=crop&w=900&q=80", false),
			new("Sinais vitais no raciocinio clinico", "Use PA, FC, FR, temperatura e saturacao para priorizar condutas em simulacoes.", "Florence", "https://medlineplus.gov/", DateTimeOffset.Now.AddMinutes(-5), "https://images.unsplash.com/photo-1576091160550-2173dba999ef?auto=format&fit=crop&w=900&q=80", false),
			new("Curativos e avaliacao de feridas", "Observe exsudato, bordas, odor, dor e sinais flogisticos antes de propor cuidado.", "Florence", "https://medlineplus.gov/", DateTimeOffset.Now.AddMinutes(-10), "https://images.unsplash.com/photo-1581595220892-b0739db3ba8c?auto=format&fit=crop&w=900&q=80", false)
		];
	}

	[GeneratedRegex("<.*?>")]
	private static partial Regex HtmlTagRegex();

	[GeneratedRegex("\\s+")]
	private static partial Regex WhitespaceRegex();

	[GeneratedRegex("<img[^>]+src=[\"'](?<src>[^\"']+)[\"']", RegexOptions.IgnoreCase)]
	private static partial Regex ImageSourceRegex();
}

public sealed record HealthPost(
	string Title,
	string Summary,
	string Source,
	string Url,
	DateTimeOffset PublishedAt,
	string ImageUrl,
	bool IsVideo)
{
	public string SourceLine => $"{Source} - {PublishedAt:dd/MM/yyyy}";

	public bool HasImage => !string.IsNullOrWhiteSpace(ImageUrl);

	public bool HasNoImage => !HasImage;

	public string TypeLabel => IsVideo || Source.Contains("Video", StringComparison.OrdinalIgnoreCase)
		? "Video"
		: Source is "Medical Xpress" or "ScienceDaily" or "Nursing Times" or "Drugs.com" or "Ministerio da Saude" or "NIH" or "OMS"
			? "Noticia"
			: "Artigo";
}

public sealed record FeedSource(string Name, string Url);
