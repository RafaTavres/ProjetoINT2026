namespace ProjetoINT2026.Pages;

public partial class NotesPage : ContentPage
{
	private readonly System.Collections.ObjectModel.ObservableCollection<NoteItem> visibleNotes = [];
	private NoteItem? selectedNote;
	private FolderItem? selectedFolder;
	private bool isLoadingNote;

	public NotesPage()
	{
		InitializeComponent();

		FoldersCollection.ItemsSource = NoteStore.Folders;
		NotesCollection.ItemsSource = visibleNotes;
		SelectFolder(NoteStore.Folders[0]);
	}

	private void SelectNote(NoteItem note)
	{
		isLoadingNote = true;
		selectedNote = note;

		foreach (var item in visibleNotes)
		{
			item.IsSelected = item == note;
		}

		TitleEntry.Text = note.Title;
		NoteEditor.Text = note.Content;
		StatusLabel.Text = $"Editando {note.Title}";
		isLoadingNote = false;
	}

	private void SelectFolder(FolderItem folder)
	{
		selectedFolder = folder;

		foreach (var item in NoteStore.Folders)
		{
			item.IsSelected = item == folder;
		}

		RefreshVisibleNotes();
		FolderContextLabel.Text = $"Pasta: {folder.Name}";

		if (visibleNotes.Count > 0)
		{
			SelectNote(visibleNotes[0]);
			return;
		}

		var note = NoteStore.AddBlankNote(folder.Name);
		RefreshVisibleNotes();
		SelectNote(note);
	}

	private void RefreshVisibleNotes()
	{
		visibleNotes.Clear();

		if (selectedFolder is null)
		{
			return;
		}

		foreach (var note in NoteStore.Notes.Where(note => note.FolderName == selectedFolder.Name))
		{
			visibleNotes.Add(note);
		}
	}

	private void OnFolderTapped(object sender, TappedEventArgs e)
	{
		if (sender is Border { BindingContext: FolderItem folder } && folder != selectedFolder)
		{
			SelectFolder(folder);
		}
	}

	private void OnNoteTapped(object sender, TappedEventArgs e)
	{
		if (sender is Border { BindingContext: NoteItem note } && note != selectedNote)
		{
			SelectNote(note);
		}
	}

	private void OnAddNoteClicked(object sender, TappedEventArgs e)
	{
		var note = NoteStore.AddBlankNote(selectedFolder?.Name ?? "Estudos");
		RefreshVisibleNotes();
		SelectNote(note);
		TitleEntry.Focus();
	}

	private async void OnAddFolderClicked(object sender, TappedEventArgs e)
	{
		var folderName = await ModalHost.ShowPromptAsync("Nova pasta", "Nome da pasta", "Criar", "Cancelar", "Ex: Procedimentos", 28);
		if (string.IsNullOrWhiteSpace(folderName))
		{
			return;
		}

		folderName = folderName.Trim();
		var existingFolder = NoteStore.Folders.FirstOrDefault(folder => string.Equals(folder.Name, folderName, StringComparison.OrdinalIgnoreCase));
		if (existingFolder is not null)
		{
			SelectFolder(existingFolder);
			return;
		}

		var folder = new FolderItem(folderName);
		NoteStore.Folders.Add(folder);
		SelectFolder(folder);
	}

	private async void OnDeleteFolderClicked(object sender, TappedEventArgs e)
	{
		if (selectedFolder is null)
		{
			return;
		}

		var folderName = selectedFolder.Name;
		var notesInFolder = NoteStore.Notes.Count(note => note.FolderName == folderName);
		var shouldDelete = await ModalHost.ShowConfirmationAsync("Apagar pasta", $"Apagar a pasta {folderName} e {notesInFolder} nota(s)?", "Apagar", "Cancelar");
		if (!shouldDelete)
		{
			return;
		}

		var folderIndex = NoteStore.Folders.IndexOf(selectedFolder);
		foreach (var note in NoteStore.Notes.Where(note => note.FolderName == folderName).ToList())
		{
			NoteStore.Notes.Remove(note);
		}

		NoteStore.Folders.Remove(selectedFolder);
		if (NoteStore.Folders.Count == 0)
		{
			NoteStore.Folders.Add(new FolderItem("Estudos"));
		}

		SelectFolder(NoteStore.Folders[Math.Min(folderIndex, NoteStore.Folders.Count - 1)]);
	}

	private async void OnShareNoteClicked(object sender, TappedEventArgs e)
	{
		if (selectedNote is null)
		{
			return;
		}

		await Share.Default.RequestAsync(new ShareTextRequest
		{
			Title = selectedNote.FileName,
			Text = $"{selectedNote.FileName}\n\n{selectedNote.Content}"
		});
	}

	private async void OnDeleteNoteClicked(object sender, TappedEventArgs e)
	{
		if (selectedNote is null)
		{
			return;
		}

		var shouldDelete = await ModalHost.ShowConfirmationAsync("Apagar nota", $"Deseja apagar {selectedNote.Title}?", "Apagar", "Cancelar");
		if (!shouldDelete)
		{
			return;
		}

		var visibleIndex = visibleNotes.IndexOf(selectedNote);
		NoteStore.Notes.Remove(selectedNote);
		RefreshVisibleNotes();

		if (NoteStore.Notes.Count == 0)
		{
			var emptyNote = new NoteItem("Nota 01", "# Nova Nota\n\n", selectedFolder?.Name ?? "Estudos");
			NoteStore.Notes.Add(emptyNote);
			RefreshVisibleNotes();
			SelectNote(emptyNote);
			return;
		}

		if (visibleNotes.Count == 0)
		{
			var note = NoteStore.AddBlankNote(selectedFolder?.Name ?? "Estudos");
			RefreshVisibleNotes();
			SelectNote(note);
			return;
		}

		SelectNote(visibleNotes[Math.Min(visibleIndex, visibleNotes.Count - 1)]);
	}

	private void OnTitleChanged(object sender, TextChangedEventArgs e)
	{
		if (isLoadingNote || selectedNote is null)
		{
			return;
		}

		selectedNote.Title = string.IsNullOrWhiteSpace(e.NewTextValue) ? "Sem titulo" : e.NewTextValue.Trim();
		StatusLabel.Text = $"Editando {selectedNote.Title}";
	}

	private void OnContentChanged(object sender, TextChangedEventArgs e)
	{
		if (isLoadingNote || selectedNote is null)
		{
			return;
		}

		selectedNote.Content = e.NewTextValue ?? string.Empty;
	}
}
