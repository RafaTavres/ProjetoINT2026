using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoINT2026.Services;

public interface IChatResponder
{
	Task<string> GetResponseAsync(string message, IReadOnlyList<ChatHistoryItem>? history = null);
}

public sealed class ChatApiOptions
{
	public string ApiKey { get; init; } = string.Empty;

	public string Model { get; init; } = "gemini-2.5-flash-lite";
}

public sealed class GeminiChatResponder(HttpClient httpClient, ChatApiOptions options) : IChatResponder
{
	public async Task<string> GetResponseAsync(string message, IReadOnlyList<ChatHistoryItem>? history = null)
	{
		if (string.IsNullOrWhiteSpace(options.ApiKey))
		{
			return "Chave da Gemini nao configurada.";
		}

		try
		{
			var model = string.IsNullOrWhiteSpace(options.Model) ? "gemini-2.5-flash-lite" : options.Model;
			var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(options.ApiKey)}";
			var geminiRequest = new GeminiGenerateRequest(
				[
					new GeminiContent(
					[
						new GeminiPart(BuildPrompt(message, history))
					])
				],
				new GeminiGenerationConfig(0.35, 520));

			var response = await httpClient.PostAsJsonAsync(endpoint, geminiRequest);
			var responseText = await response.Content.ReadAsStringAsync();

			if (!response.IsSuccessStatusCode)
			{
				return GetGeminiErrorText(responseText, response.StatusCode);
			}

			var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateResponse>(responseText);
			var reply = geminiResponse?.Candidates?
				.FirstOrDefault()?
				.Content?
				.Parts?
				.FirstOrDefault()?
				.Text;

			return string.IsNullOrWhiteSpace(reply)
				? GetEmptyGeminiResponseText(responseText)
				: reply.Trim();
		}
		catch (Exception exception)
		{
			return $"Erro ao chamar Gemini: {exception.Message}";
		}
	}

	private static string GetGeminiErrorText(string responseText, System.Net.HttpStatusCode statusCode)
	{
		if (string.IsNullOrWhiteSpace(responseText))
		{
			return $"Gemini retornou HTTP {(int)statusCode}.";
		}

		try
		{
			var errorResponse = JsonSerializer.Deserialize<GeminiErrorResponse>(responseText);
			if (!string.IsNullOrWhiteSpace(errorResponse?.Error?.Message))
			{
				return $"Gemini: {errorResponse.Error.Message}";
			}
		}
		catch
		{
		}

		return responseText.Trim();
	}

	private static string GetEmptyGeminiResponseText(string responseText)
	{
		return string.IsNullOrWhiteSpace(responseText)
			? "Gemini retornou uma resposta vazia."
			: responseText.Trim();
	}

	private static string BuildPrompt(string userMessage, IReadOnlyList<ChatHistoryItem>? history)
	{
		var recentHistory = BuildRecentHistory(history, userMessage);
		return $$"""
		Voce e Florence, uma assistente educacional especializada em enfermagem.
		Responda em portugues do Brasil, com linguagem clara, objetiva e segura.

		Regras importantes:
		- Responda somente o conteudo final para o usuario.
		- Nunca mostre pensamentos internos, raciocinio oculto, analise passo a passo ou bastidores.
		- Nao use frases como "estou pensando", "meu raciocinio" ou "vou analisar".
		- Seja amigavel e natural, como uma tutora de enfermagem.
		- Seja curto, mas responda a pergunta: em geral, 100 a 160 palavras.
		- Nao force secoes fixas.
		- Use bullets apenas quando deixarem a resposta mais facil de ler.
		- Nao use markdown pesado, tabelas ou asteriscos.
		- Nao cumprimente com "Ola" ou "Oi" se ja existir historico de conversa.
		- Continue a conversa usando o contexto recente quando houver.
		- Se o usuario pedir algo "em geral", de uma explicacao geral em vez de pedir mais contexto.
		- Se a pergunta tiver um termo amplo, explique o conceito e cite 2 a 4 pontos importantes.
		- Peca mais contexto somente quando a pergunta envolver conduta individual, dose, emergencia, paciente real ou risco clinico.
		- Nao substitua avaliacao profissional, prescricao medica ou protocolos institucionais.
		- Nao incentive uso de dados reais de pacientes.
		- Inclua alerta/observacao somente quando for necessario para seguranca.
		- Evite diagnostico definitivo sem contexto clinico suficiente.

		Formato desejado:
		Responda em 1 paragrafo curto ou 2 a 4 bullets.
		Se a pergunta for ampla, responda de forma ampla e didatica.
		Se realmente faltar contexto para seguranca, peca uma informacao especifica de forma gentil.
		Nao escreva "Resposta:", "Avaliar:", "Conduta:", "Alerta:" ou "Observacao:" sem necessidade.

		Historico recente:
		{{recentHistory}}

		Pergunta do usuario:
		{{userMessage}}
		""";
	}

	private static string BuildRecentHistory(IReadOnlyList<ChatHistoryItem>? history, string userMessage)
	{
		if (history is null || history.Count == 0)
		{
			return "Sem historico anterior.";
		}

		var context = history;
		if (history.LastOrDefault() is { IsUser: true } lastMessage &&
			string.Equals(lastMessage.Text, userMessage, StringComparison.Ordinal))
		{
			context = history.Take(history.Count - 1).ToList();
		}

		if (context.Count == 0)
		{
			return "Sem historico anterior.";
		}

		return string.Join('\n', context
			.TakeLast(8)
			.Where(message => !IsLowValueContextMessage(message))
			.Select(message => $"{(message.IsUser ? "Usuario" : "Florence")}: {message.Text.Trim()}"));
	}

	private static bool IsLowValueContextMessage(ChatHistoryItem message)
	{
		return !message.IsUser &&
			message.Text.Contains("Consegue me dar um pouco mais de contexto", StringComparison.OrdinalIgnoreCase);
	}

	private static string CleanAiReply(string reply, bool removeGreeting)
	{
		var blockedPrefixes = new[] { "pensamento:", "raciocinio:", "raciocínio:", "analise:", "análise:", "thought:", "reasoning:" };
		var lines = reply
			.Replace("**", string.Empty)
			.Split('\n')
			.Select(line => line.TrimEnd())
			.Where(line => !blockedPrefixes.Any(prefix => line.TrimStart().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)));

		var cleaned = string.Join('\n', lines).Trim();
		return removeGreeting ? RemoveLeadingGreeting(cleaned) : cleaned;
	}

	private static string RemoveLeadingGreeting(string value)
	{
		var greetings = new[] { "Ola! ", "Olá! ", "Oi! ", "Ola, ", "Olá, ", "Oi, " };
		foreach (var greeting in greetings)
		{
			if (value.StartsWith(greeting, StringComparison.OrdinalIgnoreCase))
			{
				return value[greeting.Length..].TrimStart();
			}
		}

		return value;
	}
}

public sealed class NursingChatResponder : IChatResponder
{
	public Task<string> GetResponseAsync(string message, IReadOnlyList<ChatHistoryItem>? history = null)
	{
		var normalized = Normalize(ResolveContextMessage(message, history));
		var response = BuildResponse(message, normalized, history);
		return Task.FromResult(response);
	}

	private static string BuildResponse(string originalMessage, string message, IReadOnlyList<ChatHistoryItem>? history)
	{
		if (IsExclusionFollowUp(message, history))
		{
			return "Além de AVC, vale lembrar de outras causas neurológicas importantes, como crise convulsiva, hipoglicemia, enxaqueca com aura, infecção do sistema nervoso e trauma craniano. Em triagem, compare início dos sintomas, nível de consciência, glicemia, fala, força, pupilas e sinais vitais. Se houver alteração súbita, rebaixamento, convulsão ou déficit focal, trate como prioridade.";
		}

		if (HasAny(message, "pressao", "hipertensao", "pa alta", "pa baixa", "sinais vitais"))
		{
			return "A pressão isolada não diz tudo. O ideal é confirmar a técnica da medida e olhar junto com FC, FR, temperatura, SpO2 e sintomas. Dor torácica, falta de ar, confusão, cefaleia intensa ou alteração neurológica mudam a prioridade e devem ser comunicadas rapidamente.";
		}

		if (HasAny(message, "ferida", "curativo", "lesao", "ulcera", "cicatrizacao"))
		{
			return "Em feridas, observe a evolução: tamanho, bordas, tecido, exsudato, odor, dor e vermelhidão ao redor. O mais útil é comparar com o curativo anterior e registrar mudanças. Febre, secreção purulenta, necrose ou piora rápida merecem comunicação para a equipe.";
		}

		if (HasAny(message, "medicamento", "medicacao", "administracao", "dose", "via", "horario"))
		{
			return "Na administração de medicamentos, pense primeiro em segurança: paciente certo, prescrição, dose, via, horário e alergias. Se algo não bater ou surgir reação inesperada, é melhor pausar e comunicar do que tentar resolver no improviso.";
		}

		if (HasAny(message, "diabetes", "glicemia", "hipoglicemia", "hiperglicemia", "insulina"))
		{
			return "Na glicemia, o número precisa conversar com os sintomas. Veja última refeição, uso de insulina/antidiabético, sudorese, tremores, confusão, sonolência e sinais vitais. Rebaixamento, convulsão ou vômitos tornam a situação mais urgente.";
		}

		if (HasAny(message, "sonda", "vesical", "cateter", "urina", "diurese"))
		{
			return "Com sonda vesical, o foco é prevenir infecção e acompanhar a diurese. Mantenha o sistema fechado, bolsa abaixo da bexiga e observe volume, cor, odor, dor ou febre. Ausência de drenagem, sangue importante ou dor suprapúbica precisam ser comunicados.";
		}

		if (HasAny(message, "respiratorio", "dispneia", "saturacao", "spo2", "oxigenio", "tosse"))
		{
			return "Em queixa respiratória, olhe rápido para SpO2, FR, esforço para respirar, fala entrecortada e ausculta. Posicionar em semi-Fowler costuma ajudar se o paciente tolerar. Cianose, confusão ou queda de saturação são sinais de prioridade.";
		}

		if (HasAny(message, "febre", "infeccao", "sepse", "calafrio"))
		{
			return "Febre sozinha não fecha muita coisa. Veja sinais vitais, estado geral e possível foco: respiratório, urinário, ferida, acesso venoso ou abdominal. Confusão, hipotensão, taquicardia importante ou queda de saturação deixam o caso mais preocupante.";
		}

		if (HasAny(message, "dor", "analgesia", "escala de dor"))
		{
			return "Dor precisa ser medida e acompanhada. Pergunte local, intensidade, início, tipo da dor e o que melhora ou piora. Depois da conduta prescrita, reavalie e registre. Dor torácica, abdominal intensa ou piora súbita muda a prioridade.";
		}

		if (HasAny(message, "avc", "derrame", "acidente vascular cerebral", "neurologico", "neurologica"))
		{
			return "AVC é uma alteração súbita da circulação no cérebro. Pode ser isquêmico, quando falta sangue, ou hemorrágico, quando há sangramento. Na prática, fique atento a boca torta, fala enrolada, fraqueza em um lado, confusão, perda de equilíbrio e horário de início. Suspeita de AVC é prioridade.";
		}

		if (IsGeneralQuestion(message))
		{
			return "De forma geral, eu olharia para três coisas: o que está acontecendo, quais sinais mostram gravidade e qual cuidado de enfermagem ajuda a manter o paciente seguro. Se você me disser o tema específico, eu consigo explicar com exemplos mais úteis.";
		}

		return "Consigo te ajudar melhor com um pouco mais de detalhe. Voce quer uma explicacao geral, cuidados de enfermagem, sinais de alerta ou conduta em um caso de treino?";
	}

	private static string ResolveContextMessage(string message, IReadOnlyList<ChatHistoryItem>? history)
	{
		if (!IsContinuation(message) || history is null)
		{
			return message;
		}

		var previousUserMessage = history
			.Reverse()
			.FirstOrDefault(item => item.IsUser && !IsContinuation(item.Text));

		return previousUserMessage is null ? message : $"{previousUserMessage.Text} {message}";
	}

	private static bool IsContinuation(string message)
	{
		var normalized = Normalize(message).Trim();
		return normalized is "em geral" or "geral" or "isso" or "sobre isso" or "sim";
	}

	private static bool IsGeneralQuestion(string message)
	{
		return HasAny(message, "em geral", "geral", "o que e", "oque e", "me fale", "fale sobre", "quero saber", "explique", "sobre");
	}

	private static bool IsExclusionFollowUp(string message, IReadOnlyList<ChatHistoryItem>? history)
	{
		if (!HasAny(message, "tirando", "alem de", "fora", "sem contar", "exceto"))
		{
			return false;
		}

		var previousUserText = history?
			.Reverse()
			.FirstOrDefault(item => item.IsUser)
			?.Text ?? string.Empty;

		return HasAny(Normalize($"{message} {previousUserText}"), "avc", "derrame", "neurologic");
	}

	private static bool HasAny(string message, params string[] terms)
	{
		return terms.Any(message.Contains);
	}

	private static string Normalize(string value)
	{
		var formD = value.ToLowerInvariant().Normalize(NormalizationForm.FormD);
		var builder = new StringBuilder(formD.Length);

		foreach (var character in formD)
		{
			if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
			{
				builder.Append(character);
			}
		}

		return builder.ToString().Normalize(NormalizationForm.FormC);
	}
}

public sealed record ChatRequest([property: JsonPropertyName("message")] string Message);

public sealed record ChatResponse([property: JsonPropertyName("reply")] string Reply);

public sealed record GeminiGenerateRequest(
	[property: JsonPropertyName("contents")] IReadOnlyList<GeminiContent> Contents,
	[property: JsonPropertyName("generationConfig")] GeminiGenerationConfig GenerationConfig);

public sealed record GeminiContent(
	[property: JsonPropertyName("parts")] IReadOnlyList<GeminiPart> Parts);

public sealed record GeminiPart(
	[property: JsonPropertyName("text")] string Text);

public sealed record GeminiGenerationConfig(
	[property: JsonPropertyName("temperature")] double Temperature,
	[property: JsonPropertyName("maxOutputTokens")] int MaxOutputTokens);

public sealed record GeminiGenerateResponse(
	[property: JsonPropertyName("candidates")] IReadOnlyList<GeminiCandidate>? Candidates);

public sealed record GeminiCandidate(
	[property: JsonPropertyName("content")] GeminiContentResponse? Content);

public sealed record GeminiContentResponse(
	[property: JsonPropertyName("parts")] IReadOnlyList<GeminiPartResponse>? Parts);

public sealed record GeminiPartResponse(
	[property: JsonPropertyName("text")] string? Text);

public sealed record GeminiErrorResponse(
	[property: JsonPropertyName("error")] GeminiError? Error);

public sealed record GeminiError(
	[property: JsonPropertyName("code")] int Code,
	[property: JsonPropertyName("message")] string? Message,
	[property: JsonPropertyName("status")] string? Status);
