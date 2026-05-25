using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjetoINT2026.Pages;

public partial class NotesPage : ContentPage
{
	private readonly ObservableCollection<NoteItem> notes =
	[
		new("Anatomia 01", "# Anatomia Humana\n\nO sistema circulatorio e composto pelo coracao e vasos sanguineos...\n\n## Pontos Importantes:\n- Ventriculo Esquerdo\n- Aorta Ascendente\n- Sistole e Diastole"),
		new("Farmacologia", "# Farmacologia\n\nAnote indicacoes, contraindicacoes e calculos de dose.\n\n## Revisar\n- Diluicao\n- Intervalo entre doses\n- Cuidados antes da administracao"),
		new("Triagem", "# Triagem\n\nFluxo rapido para classificar prioridade e registrar sinais vitais.\n\n## Checar\n- PA\n- FC\n- FR\n- Temperatura"),
		new("Estagio Q3", "# Estagio Q3\n\nResumo do turno, tarefas pendentes e pontos para estudar depois.")
	];

	private NoteItem? selectedNote;
	private bool isLoadingNote;

	public NotesPage()
	{
		InitializeComponent();

		NotesCollection.ItemsSource = notes;
		SelectNote(notes[0]);
	}

	private void SelectNote(NoteItem note)
	{
		isLoadingNote = true;
		selectedNote = note;
		NotesCollection.SelectedItem = note;
		TitleEntry.Text = note.Title;
		NoteEditor.Text = note.Content;
		StatusLabel.Text = $"Editando {note.Title}";
		isLoadingNote = false;
	}

	private void OnNoteSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (e.CurrentSelection.FirstOrDefault() is NoteItem note && note != selectedNote)
		{
			SelectNote(note);
		}
	}

	private void OnAddNoteTapped(object sender, TappedEventArgs e)
	{
		var noteNumber = notes.Count + 1;
		var note = new NoteItem($"Nota {noteNumber:00}", "# Nova Nota\n\nEscreva aqui seus pontos principais.\n\n## Checklist\n- ");

		notes.Add(note);
		SelectNote(note);
		TitleEntry.Focus();
	}

	private async void OnShareNoteTapped(object sender, TappedEventArgs e)
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

	private async void OnDeleteNoteTapped(object sender, TappedEventArgs e)
	{
		if (selectedNote is null)
		{
			return;
		}

		var shouldDelete = await DisplayAlert("Apagar nota", $"Deseja apagar {selectedNote.Title}?", "Apagar", "Cancelar");
		if (!shouldDelete)
		{
			return;
		}

		var index = notes.IndexOf(selectedNote);
		notes.Remove(selectedNote);

		if (notes.Count == 0)
		{
			var emptyNote = new NoteItem("Nota 01", "# Nova Nota\n\n");
			notes.Add(emptyNote);
			SelectNote(emptyNote);
			return;
		}

		SelectNote(notes[Math.Min(index, notes.Count - 1)]);
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

public sealed class NoteItem : INotifyPropertyChanged
{
	private string title;
	private string content;

	public NoteItem(string title, string content)
	{
		this.title = title;
		this.content = content;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Title
	{
		get => title;
		set
		{
			if (title == value)
			{
				return;
			}

			title = value;
			OnPropertyChanged();
			OnPropertyChanged(nameof(FileName));
		}
	}

	public string Content
	{
		get => content;
		set
		{
			if (content == value)
			{
				return;
			}

			content = value;
			OnPropertyChanged();
		}
	}

	public string FileName => Title.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? Title : $"{Title}.md";

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
