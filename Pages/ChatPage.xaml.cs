using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjetoINT2026.Pages;

public partial class ChatPage : ContentPage
{
	private const string DefaultAiNotesFolder = "Anotacoes IA";
	private readonly IChatResponder chatResponder = new EchoChatResponder();
	private string? pendingNoteResponse;
	private string selectedSaveFolderName = DefaultAiNotesFolder;

	public ChatPage()
	{
		InitializeComponent();
		BindingContext = this;

		Messages.Add(ChatMessage.FromAssistant("Ola! Envie uma mensagem para testar o fluxo do chat."));
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
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		MessageEntry.Text = string.Empty;
		Messages.Add(ChatMessage.FromUser(text));
		await ScrollToLatestMessageAsync();

		var response = await chatResponder.GetResponseAsync(text);
		Messages.Add(ChatMessage.FromAssistant(response));
		await ScrollToLatestMessageAsync();
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

public interface IChatResponder
{
	Task<string> GetResponseAsync(string message);
}

public sealed class EchoChatResponder : IChatResponder
{
	public Task<string> GetResponseAsync(string message)
	{
		return Task.FromResult(message);
	}
}

public sealed class ChatMessage : INotifyPropertyChanged
{
	private bool isSelected;

	private ChatMessage(string sender, string text, bool isUser)
	{
		Sender = sender;
		Text = text;
		CanSaveAsNote = !isUser;
		Column = isUser ? 1 : 0;
		BubbleColor = isUser ? Color.FromArgb("#AFC0A3") : Colors.White;
		TextColor = isUser ? Colors.White : Color.FromArgb("#343A32");
		SenderColor = isUser ? Colors.White : Color.FromArgb("#66745C");
	}

	public string Sender { get; }

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Text { get; }

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
