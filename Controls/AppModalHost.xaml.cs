using Microsoft.Maui.Controls.Shapes;

namespace ProjetoINT2026.Controls;

public partial class AppModalHost : ContentView
{
	private TaskCompletionSource<object?>? completionSource;
	private ModalMode mode;

	public AppModalHost()
	{
		InitializeComponent();
	}

	public async Task ShowMessageAsync(string title, string message, string buttonText = "OK")
	{
		Configure(title, message, ModalMode.Message);
		CancelButton.IsVisible = false;
		PrimaryButton.Text = buttonText;
		Grid.SetColumn(PrimaryButton, 0);
		Grid.SetColumnSpan(PrimaryButton, 2);
		await ShowAsync();
	}

	public async Task ShowCaseFeedbackAsync(string title, string message, string buttonText = "Entendi")
	{
		Configure(title, message, ModalMode.Message, true);
		CancelButton.IsVisible = false;
		PrimaryButton.Text = buttonText;
		Grid.SetColumn(PrimaryButton, 0);
		Grid.SetColumnSpan(PrimaryButton, 2);
		await ShowAsync();
	}

	public async Task<bool> ShowConfirmationAsync(string title, string message, string confirmText = "Confirmar", string cancelText = "Cancelar")
	{
		Configure(title, message, ModalMode.Confirmation);
		CancelButton.IsVisible = true;
		CancelButton.Text = cancelText;
		PrimaryButton.Text = confirmText;
		Grid.SetColumn(PrimaryButton, 1);
		Grid.SetColumnSpan(PrimaryButton, 1);

		var result = await ShowAsync();
		return result is true;
	}

	public async Task<string?> ShowOptionsAsync(string title, string message, IEnumerable<string> options, string cancelText = "Cancelar")
	{
		Configure(title, message, ModalMode.Options);
		OptionsScroll.IsVisible = true;
		CancelButton.IsVisible = true;
		CancelButton.Text = cancelText;
		PrimaryButton.IsVisible = false;
		OptionsStack.Children.Clear();

		foreach (var option in options)
		{
			OptionsStack.Children.Add(CreateOptionRow(option));
		}

		var result = await ShowAsync();
		return result as string;
	}

	public async Task<string?> ShowPromptAsync(
		string title,
		string message,
		string confirmText = "Criar",
		string cancelText = "Cancelar",
		string placeholder = "",
		int maxLength = 100)
	{
		Configure(title, message, ModalMode.Prompt);
		InputEntry.IsVisible = true;
		InputEntry.Text = string.Empty;
		InputEntry.Placeholder = placeholder;
		InputEntry.MaxLength = maxLength;
		CancelButton.IsVisible = true;
		CancelButton.Text = cancelText;
		PrimaryButton.IsVisible = true;
		PrimaryButton.Text = confirmText;
		Grid.SetColumn(PrimaryButton, 1);
		Grid.SetColumnSpan(PrimaryButton, 1);

		var resultTask = ShowAsync();
		await Task.Delay(120);
		InputEntry.Focus();

		return await resultTask as string;
	}

	private void Configure(string title, string message, ModalMode modalMode, bool useStructuredMessage = false)
	{
		mode = modalMode;
		TitleLabel.Text = title;
		BuildMessageContent(message, useStructuredMessage);
		MessageScroll.IsVisible = !string.IsNullOrWhiteSpace(message);
		InputEntry.IsVisible = false;
		OptionsScroll.IsVisible = false;
		OptionsStack.Children.Clear();
		PrimaryButton.IsVisible = true;
		CancelButton.IsVisible = true;
		Grid.SetColumn(PrimaryButton, 1);
		Grid.SetColumnSpan(PrimaryButton, 1);
	}

	private Task<object?> ShowAsync()
	{
		completionSource = new TaskCompletionSource<object?>();
		IsVisible = true;
		return completionSource.Task;
	}

	private void BuildMessageContent(string message, bool useStructuredMessage)
	{
		MessageStack.Children.Clear();

		if (string.IsNullOrWhiteSpace(message))
		{
			MessageLabel.Text = string.Empty;
			return;
		}

		if (!useStructuredMessage)
		{
			MessageLabel.Text = message.Trim();
			MessageStack.Children.Add(MessageLabel);
			return;
		}

		var blocks = message
			.Replace("\r\n", "\n", StringComparison.Ordinal)
			.Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		for (var index = 0; index < blocks.Length; index++)
		{
			var section = CreateFeedbackSection(blocks[index], index);
			MessageStack.Children.Add(section);
		}
	}

	private static Border CreateFeedbackSection(string block, int index)
	{
		var title = GetFallbackSectionTitle(index);
		var body = block.Trim();

		if (TrySplitSection(block, out var parsedTitle, out var parsedBody))
		{
			title = NormalizeSectionTitle(parsedTitle);
			body = parsedBody;
		}

		var isMain = index == 0;
		var isNextStep =
			title.Contains("proximo", StringComparison.OrdinalIgnoreCase) ||
			title.Contains("próximo", StringComparison.OrdinalIgnoreCase);
		var accentColor = isNextStep ? "#6E8468" : isMain ? "#8FAA87" : "#C8D8C2";
		var backgroundColor = isMain ? "#EEF4EC" : isNextStep ? "#F1F6EF" : "#FAFBF8";

		var accent = new BoxView
		{
			Color = Color.FromArgb(accentColor),
			WidthRequest = 4,
			HorizontalOptions = LayoutOptions.Fill,
			VerticalOptions = LayoutOptions.Fill
		};

		var textStack = new VerticalStackLayout
		{
			Spacing = 4,
			Children =
			{
				new Label
				{
					Text = title.ToUpperInvariant(),
					TextColor = Color.FromArgb("#536B50"),
					FontSize = 11,
					FontAttributes = FontAttributes.Bold,
					LineBreakMode = LineBreakMode.WordWrap
				},
				new Label
				{
					Text = body,
					TextColor = Color.FromArgb(isMain ? "#343A32" : "#626B5F"),
					FontSize = isMain ? 14 : 13,
					LineBreakMode = LineBreakMode.WordWrap
				}
			}
		};
		Grid.SetColumn(textStack, 1);

		return new Border
		{
			BackgroundColor = Color.FromArgb(backgroundColor),
			Stroke = Color.FromArgb(isMain ? "#DDE7DA" : "#E7EAE3"),
			StrokeThickness = 1,
			Padding = new Thickness(0),
			StrokeShape = new RoundRectangle { CornerRadius = 14 },
			Content = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(new GridLength(4)),
					new ColumnDefinition(GridLength.Star)
				},
				ColumnSpacing = 12,
				Padding = new Thickness(0, 12, 14, 12),
				Children =
				{
					accent,
					textStack
				}
			}
		};
	}

	private static bool TrySplitSection(string block, out string title, out string body)
	{
		title = string.Empty;
		body = string.Empty;

		var separatorIndex = block.IndexOf(':', StringComparison.Ordinal);
		if (separatorIndex <= 0 || separatorIndex > 32)
		{
			return false;
		}

		title = block[..separatorIndex].Trim();
		body = block[(separatorIndex + 1)..].Trim();
		return !string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(body);
	}

	private static string GetFallbackSectionTitle(int index)
	{
		return index switch
		{
			1 => "Evolução do paciente",
			2 => "Análise",
			_ => "Resultado"
		};
	}

	private static string NormalizeSectionTitle(string title)
	{
		if (title.Contains("Exame", StringComparison.OrdinalIgnoreCase))
		{
			return "Ação realizada";
		}

		if (title.Contains("Hipotese", StringComparison.OrdinalIgnoreCase) ||
			title.Contains("Hipótese", StringComparison.OrdinalIgnoreCase))
		{
			return "Hipótese";
		}

		if (title.Contains("Conduta", StringComparison.OrdinalIgnoreCase))
		{
			return "Conduta";
		}

		if (title.Contains("Leitura", StringComparison.OrdinalIgnoreCase))
		{
			return "Análise do caso";
		}

		if (title.Contains("Proximo", StringComparison.OrdinalIgnoreCase) ||
			title.Contains("Próximo", StringComparison.OrdinalIgnoreCase))
		{
			return "Próximo passo";
		}

		if (title.Contains("Efeito", StringComparison.OrdinalIgnoreCase) ||
			title.Contains("mudou", StringComparison.OrdinalIgnoreCase))
		{
			return "Impacto no paciente";
		}

		return title;
	}

	private Border CreateOptionRow(string option)
	{
		var row = new Border
		{
			BackgroundColor = Color.FromArgb("#FAFBF8"),
			Stroke = Color.FromArgb("#E0E3DC"),
			StrokeThickness = 1,
			Padding = new Thickness(14, 12),
			StrokeShape = new RoundRectangle { CornerRadius = 12 },
			Content = new Label
			{
				Text = option,
				TextColor = Color.FromArgb("#343A32"),
				FontSize = 14,
				LineBreakMode = LineBreakMode.WordWrap
			}
		};

		row.GestureRecognizers.Add(new TapGestureRecognizer
		{
			Command = new Command(() => Complete(option))
		});

		return row;
	}

	private void OnCancelClicked(object sender, EventArgs e)
	{
		Complete(mode == ModalMode.Confirmation ? false : null);
	}

	private void OnPrimaryClicked(object sender, EventArgs e)
	{
		object? result = mode switch
		{
			ModalMode.Prompt => InputEntry.Text?.Trim(),
			_ => true
		};

		Complete(result);
	}

	private void Complete(object? result)
	{
		IsVisible = false;
		InputEntry.Unfocus();
		completionSource?.TrySetResult(result);
		completionSource = null;
	}

	private enum ModalMode
	{
		Message,
		Confirmation,
		Options,
		Prompt
	}
}
