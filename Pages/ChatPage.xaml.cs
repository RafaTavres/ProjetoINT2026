using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ProjetoINT2026.Services;

namespace ProjetoINT2026.Pages;

public partial class ChatPage : ContentPage
{
	private const string DefaultAiNotesFolder = "Anotacoes IA";
	private readonly IChatResponder chatResponder;
	private ChatAttachment? pendingAttachment;
	private string? pendingNoteResponse;
	private string selectedSaveFolderName = DefaultAiNotesFolder;

	public ChatPage()
		: this(MauiProgram.Services.GetRequiredService<IChatResponder>())
	{
	}

	public ChatPage(IChatResponder chatResponder)
	{
		InitializeComponent();
		this.chatResponder = chatResponder;
		BindingContext = this;

		LoadSavedMessages();
	}

	public ObservableCollection<ChatMessage> Messages { get; } = [];

	public ObservableCollection<SaveFolderOption> SaveFolderOptions { get; } = [];

	private async void OnSendClicked(object sender, EventArgs e)
	{
		await SendCurrentMessageAsync();
	}

	private async void OnMessageCompleted(object sender, EventArgs e)
	{
		await SendCurrentMessageAsync();
	}

	private async Task SendCurrentMessageAsync()
	{
		var text = MessageEntry.Text?.Trim();
		if (string.IsNullOrWhiteSpace(text) && pendingAttachment is null)
		{
			return;
		}

		var historyBeforeMessage = GetHistorySnapshot();
		var messageForUser = BuildUserMessageText(text, pendingAttachment);
		var messageForAi = BuildAiMessageText(text, pendingAttachment);

		MessageEntry.Text = string.Empty;
		ClearAttachment();
		Messages.Add(ChatMessage.FromUser(messageForUser));
		SaveMessages();
		await ScrollToLatestMessageAsync();

		var response = await chatResponder.GetResponseAsync(messageForAi, historyBeforeMessage);
		Messages.Add(ChatMessage.FromAssistant(response));
		SaveMessages();
		await ScrollToLatestMessageAsync();
	}

	private static string BuildUserMessageText(string? text, ChatAttachment? attachment)
	{
		var message = string.IsNullOrWhiteSpace(text) ? "Analise o arquivo anexado." : text.Trim();
		return attachment is null ? message : $"{message}\n\nArquivo: {attachment.FileName}";
	}

	private static string BuildAiMessageText(string? text, ChatAttachment? attachment)
	{
		var message = string.IsNullOrWhiteSpace(text)
			? "Analise o arquivo anexado e destaque os pontos uteis para enfermagem."
			: text.Trim();

		if (attachment is null)
		{
			return message;
		}

		return $"""
		{message}

		Arquivo anexado:
		Nome: {attachment.FileName}
		Tipo: {attachment.ContentType}
		Conteudo extraido:
		{attachment.TextPreview}
		""";
	}

	private void LoadSavedMessages()
	{
		var savedMessages = ChatSessionStore.Load();
		if (savedMessages.Count == 0)
		{
			Messages.Add(ChatMessage.FromAssistant("Ola! Sou a Florence. Me diga sua duvida de enfermagem que eu te ajudo de forma objetiva."));
			SaveMessages();
			return;
		}

		foreach (var message in savedMessages)
		{
			Messages.Add(message.IsUser ? ChatMessage.FromUser(message.Text) : ChatMessage.FromAssistant(message.Text));
		}
	}

	private IReadOnlyList<ChatHistoryItem> GetHistorySnapshot()
	{
		return Messages
			.Select(message => new ChatHistoryItem(message.IsUser, message.Text))
			.ToList();
	}

	private void SaveMessages()
	{
		ChatSessionStore.Save(GetHistorySnapshot());
	}

	private async void OnAttachFileTapped(object sender, TappedEventArgs e)
	{
		try
		{
			var file = await FilePicker.Default.PickAsync(new PickOptions
			{
				PickerTitle = "Escolha um arquivo para enviar"
			});

			if (file is null)
			{
				return;
			}

			pendingAttachment = await ChatAttachment.FromFileAsync(file);
			AttachmentLabel.Text = pendingAttachment.FileName;
			AttachmentChip.IsVisible = true;
		}
		catch
		{
			await DisplayAlert("Arquivo", "Nao foi possivel anexar esse arquivo.", "OK");
		}
	}

	private void OnRemoveAttachmentTapped(object sender, TappedEventArgs e)
	{
		ClearAttachment();
	}

	private void ClearAttachment()
	{
		pendingAttachment = null;
		AttachmentLabel.Text = string.Empty;
		AttachmentChip.IsVisible = false;
	}

	private async Task ScrollToLatestMessageAsync()
	{
		await Task.Delay(50);

		if (Messages.Count > 0)
		{
			MessagesCollection.ScrollTo(Messages[^1], position: ScrollToPosition.End, animate: true);
		}
	}

	private void OnSaveAsNoteTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: ChatMessage message } || !message.CanSaveAsNote)
		{
			return;
		}

		pendingNoteResponse = message.Text;
		SaveNotePreviewLabel.Text = message.Text;
		LoadSaveFolderOptions();
		SaveNotePopup.IsVisible = true;
	}

	private void OnMessageTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: ChatMessage tappedMessage } || !tappedMessage.CanSaveAsNote)
		{
			return;
		}

		foreach (var message in Messages)
		{
			message.IsSelected = message == tappedMessage && !message.IsSelected;
		}
	}

	private void LoadSaveFolderOptions()
	{
		selectedSaveFolderName = DefaultAiNotesFolder;
		SaveFolderOptions.Clear();
		SaveFolderOptions.Add(new SaveFolderOption(DefaultAiNotesFolder, true));

		foreach (var folder in NoteStore.Folders.Where(folder => !string.Equals(folder.Name, DefaultAiNotesFolder, StringComparison.OrdinalIgnoreCase)))
		{
			SaveFolderOptions.Add(new SaveFolderOption(folder.Name, false));
		}
	}

	private void OnSaveFolderTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: SaveFolderOption folderOption })
		{
			return;
		}

		selectedSaveFolderName = folderOption.Name;
		foreach (var option in SaveFolderOptions)
		{
			option.IsSelected = option == folderOption;
		}
	}

	private void OnCancelSaveNoteClicked(object sender, EventArgs e)
	{
		pendingNoteResponse = null;
		SaveNotePopup.IsVisible = false;
	}

	private async void OnConfirmSaveNoteClicked(object sender, EventArgs e)
	{
		if (string.IsNullOrWhiteSpace(pendingNoteResponse))
		{
			SaveNotePopup.IsVisible = false;
			return;
		}

		var note = NoteStore.AddFromChatResponse(pendingNoteResponse, selectedSaveFolderName);
		pendingNoteResponse = null;
		SaveNotePopup.IsVisible = false;
		await DisplayAlert("Nota salva", $"{note.Title} foi salva em {note.FolderName}.", "OK");
	}
}

public sealed class ChatAttachment
{
	private static readonly string[] TextExtensions = [".txt", ".md", ".csv", ".json", ".xml", ".xaml", ".cs", ".log"];

	private ChatAttachment(string fileName, string contentType, string textPreview)
	{
		FileName = fileName;
		ContentType = string.IsNullOrWhiteSpace(contentType) ? "arquivo" : contentType;
		TextPreview = textPreview;
	}

	public string FileName { get; }

	public string ContentType { get; }

	public string TextPreview { get; }

	public static async Task<ChatAttachment> FromFileAsync(FileResult file)
	{
		var extension = Path.GetExtension(file.FileName);
		if (!TextExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
		{
			return new ChatAttachment(file.FileName, file.ContentType, "Nao foi possivel ler o conteudo desse tipo de arquivo no MVP. Use a pergunta do usuario e o nome do arquivo como contexto.");
		}

		try
		{
			await using var stream = await file.OpenReadAsync();
			using var reader = new StreamReader(stream);
			var content = await reader.ReadToEndAsync();
			var preview = content.Length > 5000 ? $"{content[..5000]}\n\n[conteudo cortado]" : content;
			return new ChatAttachment(file.FileName, file.ContentType, preview);
		}
		catch
		{
			return new ChatAttachment(file.FileName, file.ContentType, "Nao foi possivel ler o conteudo do arquivo.");
		}
	}
}

public sealed class ChatMessage : INotifyPropertyChanged
{
	private bool isSelected;

	private ChatMessage(string sender, string text, bool isUser)
	{
		Sender = sender;
		Text = text;
		IsUser = isUser;
		CanSaveAsNote = !isUser;
		Column = isUser ? 1 : 0;
		BubbleColor = isUser ? Color.FromArgb("#AFC0A3") : Colors.White;
		TextColor = isUser ? Colors.White : Color.FromArgb("#343A32");
		SenderColor = isUser ? Colors.White : Color.FromArgb("#66745C");
	}

	public string Sender { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Text { get; }

	public bool IsUser { get; }

	public bool CanSaveAsNote { get; }

	public bool IsSelected
	{
		get => isSelected;
		set
		{
			if (isSelected == value)
			{
				return;
			}

			isSelected = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(ShowSaveAsNote));
		}
	}

	public bool ShowSaveAsNote => CanSaveAsNote && IsSelected;

	public int Column { get; }

	public Color BubbleColor { get; }

	public Color TextColor { get; }

	public Color SenderColor { get; }

	public LayoutOptions BubbleHorizontalOptions => IsUser ? LayoutOptions.End : LayoutOptions.Start;

	public static ChatMessage FromUser(string text)
	{
		return new ChatMessage("Voce", text, true);
	}

	public static ChatMessage FromAssistant(string text)
	{
		return new ChatMessage("Florence", text, false);
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed class SaveFolderOption : INotifyPropertyChanged
{
	private bool isSelected;

	public SaveFolderOption(string name, bool isSelected)
	{
		Name = name;
		this.isSelected = isSelected;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Name { get; }

	public bool IsSelected
	{
		get => isSelected;
		set
		{
			if (isSelected == value)
			{
				return;
			}

			isSelected = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(BackgroundColor));
			OnPropertyChanged(nameof(StrokeColor));
			OnPropertyChanged(nameof(TextColor));
		}
	}

	public Color BackgroundColor => IsSelected ? Color.FromArgb("#DDE7DA") : Colors.White;

	public Color StrokeColor => IsSelected ? Color.FromArgb("#AFC0A3") : Color.FromArgb("#E0E3DC");

	public Color TextColor => IsSelected ? Color.FromArgb("#343A32") : Color.FromArgb("#66745C");

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
