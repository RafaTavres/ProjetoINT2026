using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProjetoINT2026.Pages;

public static class NoteStore
{
	public static ObservableCollection<FolderItem> Folders { get; } =
	[
		new("Estudos"),
		new("Procedimentos"),
		new("Plantao"),
		new("Respostas IA")
	];

	public static ObservableCollection<NoteItem> Notes { get; } =
	[
		new("Anatomia 01", "# Anatomia Humana\n\nO sistema circulatorio e composto pelo coracao e vasos sanguineos...\n\n## Pontos Importantes:\n- Ventriculo Esquerdo\n- Aorta Ascendente\n- Sistole e Diastole", "Estudos"),
		new("Farmacologia", "# Farmacologia\n\nAnote indicacoes, contraindicacoes e calculos de dose.\n\n## Revisar\n- Diluicao\n- Intervalo entre doses\n- Cuidados antes da administracao", "Estudos"),
		new("Triagem", "# Triagem\n\nFluxo rapido para classificar prioridade e registrar sinais vitais.\n\n## Checar\n- PA\n- FC\n- FR\n- Temperatura", "Procedimentos"),
		new("Estagio Q3", "# Estagio Q3\n\nResumo do turno, tarefas pendentes e pontos para estudar depois.", "Plantao")
	];

	public static NoteItem AddBlankNote(string folderName)
	{
		var note = new NoteItem($"Nota {Notes.Count + 1:00}", "# Nova Nota\n\nEscreva aqui seus pontos principais.\n\n## Checklist\n- ", folderName);
		Notes.Add(note);
		return note;
	}

	public static NoteItem AddFromChatResponse(string response, string? folderName = null)
	{
		const string defaultChatFolderName = "Anotacoes IA";
		var targetFolderName = string.IsNullOrWhiteSpace(folderName) ? defaultChatFolderName : folderName.Trim();
		if (!Folders.Any(folder => string.Equals(folder.Name, targetFolderName, StringComparison.OrdinalIgnoreCase)))
		{
			Folders.Add(new FolderItem(targetFolderName));
		}

		var title = $"Resposta Florence {Notes.Count + 1:00}";
		var content = $"# Resposta da Florence\n\n{response}";
		var note = new NoteItem(title, content, targetFolderName);

		Notes.Add(note);
		return note;
	}
}

public sealed class FolderItem(string name) : INotifyPropertyChanged
{
	private bool isSelected;

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Name { get; } = name;

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
			OnPropertyChanged(nameof(TextColor));
			OnPropertyChanged(nameof(StrokeColor));
		}
	}

	public Color BackgroundColor => IsSelected ? Color.FromArgb("#DDE7DA") : Colors.Transparent;

	public Color TextColor => IsSelected ? Color.FromArgb("#343A32") : Color.FromArgb("#66745C");

	public Color StrokeColor => IsSelected ? Color.FromArgb("#AFC0A3") : Color.FromArgb("#E2E6DF");

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed class NoteItem : INotifyPropertyChanged
{
	private string title;
	private string content;
	private string folderName;
	private bool isSelected;

	public NoteItem(string title, string content, string folderName = "Estudos")
	{
		this.title = title;
		this.content = content;
		this.folderName = folderName;
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

	public string FolderName
	{
		get => folderName;
		set
		{
			if (folderName == value)
			{
				return;
			}

			folderName = value;
			OnPropertyChanged();
		}
	}

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

	public Color StrokeColor => IsSelected ? Color.FromArgb("#AFC0A3") : Color.FromArgb("#E7E9E4");

	public Color TextColor => IsSelected ? Color.FromArgb("#343A32") : Color.FromArgb("#66745C");

	public string FileName => Title.EndsWith(".md", StringComparison.OrdinalIgnoreCase) ? Title : $"{Title}.md";

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
