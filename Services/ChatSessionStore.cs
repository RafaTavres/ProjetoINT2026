using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProjetoINT2026.Services;

public static class ChatSessionStore
{
	private const string ChatHistoryKey = "florence_chat_history_v1";
	private const int MaxStoredMessages = 40;

	public static IReadOnlyList<ChatHistoryItem> Load()
	{
		var json = Preferences.Default.Get(ChatHistoryKey, string.Empty);
		if (string.IsNullOrWhiteSpace(json))
		{
			return [];
		}

		try
		{
			return JsonSerializer.Deserialize<List<ChatHistoryItem>>(json) ?? [];
		}
		catch
		{
			return [];
		}
	}

	public static void Save(IEnumerable<ChatHistoryItem> messages)
	{
		var compactMessages = messages
			.Where(message => !string.IsNullOrWhiteSpace(message.Text))
			.TakeLast(MaxStoredMessages)
			.ToList();

		Preferences.Default.Set(ChatHistoryKey, JsonSerializer.Serialize(compactMessages));
	}
}

public sealed record ChatHistoryItem(
	[property: JsonPropertyName("isUser")] bool IsUser,
	[property: JsonPropertyName("text")] string Text);
