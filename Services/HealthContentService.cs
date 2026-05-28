using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ProjetoINT2026.Services;

public interface IHealthContentService
{
	Task<IReadOnlyList<HealthPost>> GetPostsAsync(CancellationToken cancellationToken = default);
}

public sealed partial class HealthContentService(HttpClient httpClient) : IHealthContentService
{
	private const string CacheKey = "home_health_feed_cache";

	private static readonly FeedSource[] RssSources =
	[
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

	public async Task<IReadOnlyList<HealthPost>> GetPostsAsync(CancellationToken cancellationToken = default)
	{
		var posts = new List<HealthPost>();

		foreach (var source in RssSources)
		{
			posts.AddRange(await TryReadRssAsync(source, cancellationToken));
		}

		foreach (var term in MedlinePlusTerms)
		{
			posts.AddRange(await TryReadMedlinePlusAsync(term, cancellationToken));
		}

		var result = posts
			.GroupBy(post => post.Url)
			.Select(group => group.First())
			.OrderByDescending(post => post.HasImage)
			.ThenByDescending(post => post.PublishedAt)
			.Take(12)
			.ToList();

		if (result.Count > 0)
		{
			SaveCache(result);
			return result;
		}

		var cached = LoadCache();
		return cached.Count > 0 ? cached : GetFallbackPosts();
	}

	private async Task<IReadOnlyList<HealthPost>> TryReadRssAsync(FeedSource source, CancellationToken cancellationToken)
	{
		try
		{
			var xml = await httpClient.GetStringAsync(source.Url, cancellationToken);
			var doc = XDocument.Parse(xml);

			return doc.Descendants("item")
				.Select(item => new HealthPost(
					CleanText(item.Element("title")?.Value),
					CleanText(item.Element("description")?.Value),
					source.Name,
					CleanText(item.Element("link")?.Value),
					ParseDate(item.Element("pubDate")?.Value),
					ExtractImageUrl(item),
					IsVideoItem(item)))
				.Where(post => !string.IsNullOrWhiteSpace(post.Title) && !string.IsNullOrWhiteSpace(post.Url))
				.Take(6)
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
						string.Empty,
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

		var html = item.Element("description")?.Value ?? item.Descendants().FirstOrDefault(element => element.Name.LocalName == "encoded")?.Value;
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

	public string TypeLabel => IsVideo ? "Video" : "Artigo";
}

public sealed record FeedSource(string Name, string Url);
