using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Controls.Shapes;

namespace ProjetoINT2026.Pages;

public partial class PatientsPage : ContentPage
{
	private const string ActiveColor = "#8FAA87";
	private const string MutedColor = "#20251F";
	private const string HiddenLine = "#00FFFFFF";
	private static readonly TimeSpan ActionDelay = TimeSpan.FromSeconds(10);

	private readonly HashSet<PatientCase> runningActionTimers = [];
	private PatientCase? selectedPatient;
	private bool isLoadingPatientNotes;

	public PatientsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	private static readonly ObservableCollection<PatientCase> SharedPatients =
	[
		new(
			"Carlos Silva",
			22,
			"Masculino",
			"Média",
			"Dor abdominal, náusea e febre",
			"PA: 120/80 mmHg\nFC: 96 bpm | FR: 20 irpm\nTemp.: 38,1 C | SpO2: 98%",
			"Sem alergias conhecidas",
			"Monitorar dor, temperatura e sinais de piora abdominal.",
			"Apendicite",
			["Hemograma", "Exame físico abdominal", "Ultrassom abdominal"],
			["Gastroenterite", "Apendicite", "Cólica renal", "Infecção urinária"],
			["Manter jejum, acesso venoso e comunicar equipe médica", "Antibiótico sem avaliação", "Alta com analgésico", "Curativo simples"],
			"Manter jejum, acesso venoso e comunicar equipe médica",
			[
				new("08:30", "Paciente relata dor em fossa ilíaca direita, náusea e febre desde a madrugada."),
				new("09:10", "Dor piora ao caminhar. Sem vômitos no momento."),
				new("09:35", "Exame físico abdominal pode orientar a prioridade do caso.")
			],
			"Observe sinais de irritação peritoneal e evite fechar conduta sem exame físico."),
		new(
			"Mateus Oliveira",
			34,
			"Masculino",
			"Fácil",
			"Tosse produtiva e dispneia leve",
			"PA: 130/84 mmHg\nFC: 92 bpm | FR: 22 irpm\nTemp.: 37,8 C | SpO2: 94%",
			"Não informado",
			"Elevar cabeceira, observar padrão respiratório e saturação.",
			"Pneumonia",
			["Ausculta pulmonar", "Radiografia de tórax", "Oximetria seriada"],
			["Pneumonia", "Enxaqueca", "Apendicite", "Hipoglicemia"],
			["Monitorar SpO2 e comunicar piora respiratória", "Liberar sem orientação", "Reduzir ingesta hídrica", "Aplicar curativo compressivo"],
			"Monitorar SpO2 e comunicar piora respiratória",
			[
				new("09:00", "Relata tosse há cinco dias, com secreção amarelada."),
				new("10:20", "Ausculta com roncos difusos. Saturação limítrofe em ar ambiente."),
				new("11:00", "Refere cansaço aos esforços leves.")
			],
			"Priorize avaliação respiratória, saturação e sinais de desconforto."),
		new(
			"Ana Beatriz",
			28,
			"Feminino",
			"Média",
			"Cefaleia intensa e fotofobia",
			"PA: 118/76 mmHg\nFC: 84 bpm | FR: 17 irpm\nTemp.: 36,5 C | SpO2: 99%",
			"Dipirona",
			"Ambiente calmo, avaliar escala de dor e sinais neurológicos.",
			"Enxaqueca",
			["Escala de dor", "Avaliação neurológica", "Sinais de alarme"],
			["Meningite", "Enxaqueca", "Crise hipertensiva", "Sinusite"],
			["Reduzir estímulos, hidratar e reavaliar dor", "Antibiótico imediato", "Alta sem reavaliação", "Jejum absoluto prolongado"],
			"Reduzir estímulos, hidratar e reavaliar dor",
			[
				new("10:15", "Paciente refere cefaleia pulsátil há 6 horas, com fotofobia."),
				new("10:45", "Sem febre e sem rigidez de nuca relatada."),
				new("11:20", "Dor melhora parcialmente em ambiente escuro.")
			],
			"Diferencie enxaqueca de sinais de alarme neurológicos."),
		new(
			"Julia Costa",
			41,
			"Feminino",
			"Difícil",
			"Ferida operatória com hiperemia",
			"PA: 126/82 mmHg\nFC: 102 bpm | FR: 18 irpm\nTemp.: 37,9 C | SpO2: 98%",
			"Látex",
			"Inspecionar ferida, registrar aspecto e trocar curativo com técnica limpa.",
			"Infecção de sítio cirúrgico",
			["Inspeção da ferida", "Temperatura seriada", "Cultura se houver secreção"],
			["Dermatite por contato", "Infecção de sítio cirúrgico", "Enxaqueca", "Bronquite"],
			["Comunicar sinais infecciosos e reforçar cuidado com curativo", "Cobrir sem avaliar", "Retirar pontos sem indicação", "Ignorar alergia a látex"],
			"Comunicar sinais infecciosos e reforçar cuidado com curativo",
			[
				new("07:40", "Curativo com pequena secreção serosa e hiperemia ao redor."),
				new("09:00", "Paciente relata dor local crescente desde ontem."),
				new("11:30", "Temperatura em elevação discreta.")
			],
			"Observe secreção, bordas, odor, dor, febre e alergias antes da conduta.")
	];

	public ObservableCollection<PatientCase> Patients => SharedPatients;

	private void OnPatientTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: PatientCase patient })
		{
			return;
		}

		OpenPatient(patient);
	}

	private void OnGenerateCaseTapped(object sender, TappedEventArgs e)
	{
		var available = Patients.Where(patient => !patient.IsFinished).ToList();
		var next = available.Count > 0 ? available[Random.Shared.Next(available.Count)] : Patients[Random.Shared.Next(Patients.Count)];
		OpenPatient(next);
	}

	private void OpenPatient(PatientCase patient)
	{
		selectedPatient = patient;
		PatientNameLabel.Text = patient.Name;
		PatientMetaLabel.Text = $"{patient.Age} anos | {patient.Gender} | {patient.Difficulty}";
		VitalsLabel.Text = patient.Vitals;
		ComplaintLabel.Text = patient.Complaint;
		AllergiesLabel.Text = patient.Allergies;
		InitialCareLabel.Text = patient.InitialCarePlan;

		isLoadingPatientNotes = true;
		PatientNotesEditor.Text = patient.Notes;
		isLoadingPatientNotes = false;

		BuildHistory(patient);
		RefreshSelectedPatientUi();
		SetActiveTab(PatientTab.Summary);
		PatientListView.IsVisible = false;
		PatientDetailView.IsVisible = true;
	}

	private void RefreshSelectedPatientUi()
	{
		if (selectedPatient is null)
		{
			return;
		}

		CaseProgressLabel.Text = selectedPatient.StatusLine;
		HeaderStatusLabel.Text = selectedPatient.HeaderBadgeText;
		CaseScoreLabel.Text = selectedPatient.ScoreLine;
		CaseActionStateLabel.Text = selectedPatient.ActionState;
		CaseRewardLabel.Text = selectedPatient.RewardLine;
		CaseProgressBar.Progress = selectedPatient.ProgressValue;
		DiagnosisLabel.Text = selectedPatient.DisplayDiagnosis;
		CareLabel.Text = selectedPatient.DisplayCarePlan;
		PatientNotesEditor.IsReadOnly = selectedPatient.IsFinished;
		FloatingActionButton.BackgroundColor = Color.FromArgb(ActiveColor);
		FloatingActionButton.WidthRequest = selectedPatient.IsFinished ? 146 : 168;
		FloatingActionLabel.Text = selectedPatient.PrimaryActionLabel;
		FloatingActionLabel.TextColor = Colors.White;
		NotesStatusLabel.Text = selectedPatient.IsFinished
			? "Anotações bloqueadas junto com a revisão final"
			: "Registre hipóteses e sinais que sustentam sua decisão";
		FinalReportCard.IsVisible = selectedPatient.IsFinished;
		FinalGradeLabel.Text = selectedPatient.FinalGrade;
		FinalReportLabel.Text = selectedPatient.FinalReportSummary;
		ActionMenuStatusLabel.Text = selectedPatient.IsFinished
			? "Caso finalizado. As ações estão bloqueadas."
			: selectedPatient.HasPendingAction
				? "Aguarde a ação ficar pronta antes de tomar outra decisão."
				: selectedPatient.ActionState;
		ActionMenuLastActionLabel.Text = selectedPatient.LastActionLine;
		ActionProgressCard.IsVisible = selectedPatient.HasPendingAction;
		ActionOptionsGrid.Opacity = selectedPatient.HasPendingAction ? 0.45 : 1;
		ActionOptionsGrid.InputTransparent = selectedPatient.HasPendingAction;
		PendingActionTitleLabel.Text = selectedPatient.PendingActionTitle;
		PendingActionDetailLabel.Text = selectedPatient.PendingActionDetail;
		PendingActionCountdownLabel.Text = selectedPatient.PendingActionCountdownLine;
		PendingActionProgressBar.Progress = selectedPatient.PendingActionProgress;
		ApplyStepState(ExamStepChip, ExamStepLabel, selectedPatient.HasExamAction, "1");
		ApplyStepState(DiagnosisStepChip, DiagnosisStepLabel, selectedPatient.HasDiagnosisAction, "2");
		ApplyStepState(TreatmentStepChip, TreatmentStepLabel, selectedPatient.HasTreatmentAction, "3");
		RefreshActionCards(selectedPatient);
		BuildActionLog(selectedPatient);
	}

	private static void ApplyStepState(Border chip, Label label, bool isDone, string pendingText)
	{
		chip.BackgroundColor = Color.FromArgb(isDone ? "#DDE7DA" : "#FFFFFF");
		chip.Stroke = Color.FromArgb(isDone ? ActiveColor : "#DDE7DA");
		label.TextColor = Color.FromArgb(isDone ? "#536B50" : "#777D75");
		label.FontAttributes = isDone ? FontAttributes.Bold : FontAttributes.None;
		label.Text = isDone ? "\u2713" : pendingText;
	}

	private void RefreshActionCards(PatientCase patient)
	{
		ExamActionTitleLabel.Text = patient.ExamActionTitle;
		ExamActionSubtitleLabel.Text = patient.ExamActionSubtitle;
		DiagnosisActionTitleLabel.Text = patient.DiagnosisActionTitle;
		DiagnosisActionSubtitleLabel.Text = patient.DiagnosisActionSubtitle;
		TreatmentActionTitleLabel.Text = patient.TreatmentActionTitle;
		TreatmentActionSubtitleLabel.Text = patient.TreatmentActionSubtitle;
		FinishActionTitleLabel.Text = patient.FinishActionTitle;
		FinishActionSubtitleLabel.Text = patient.FinishActionSubtitle;

		ApplyActionCardState(ExamActionCard, ExamActionTitleLabel, ExamActionSubtitleLabel, patient.CanStartExam, false);
		ApplyActionCardState(DiagnosisActionCard, DiagnosisActionTitleLabel, DiagnosisActionSubtitleLabel, patient.CanStartDiagnosis, true);
		ApplyActionCardState(TreatmentActionCard, TreatmentActionTitleLabel, TreatmentActionSubtitleLabel, patient.CanStartTreatment, false);
		ApplyActionCardState(FinishActionCard, FinishActionTitleLabel, FinishActionSubtitleLabel, patient.CanFinishCase, false);
	}

	private static void ApplyActionCardState(Border card, Label title, Label subtitle, bool isAvailable, bool isPrimary)
	{
		card.InputTransparent = !isAvailable;
		card.Opacity = isAvailable ? 1 : 0.62;
		card.BackgroundColor = Color.FromArgb(isAvailable && isPrimary ? ActiveColor : isAvailable ? "#EEF4EC" : "#F4F6F2");
		card.Stroke = Color.FromArgb(isAvailable && isPrimary ? ActiveColor : isAvailable ? "#DDE7DA" : "#E1E5DD");
		card.StrokeThickness = isAvailable && isPrimary ? 0 : 1;
		title.TextColor = Color.FromArgb(isAvailable && isPrimary ? "#FFFFFF" : isAvailable ? "#536B50" : "#777D75");
		subtitle.TextColor = Color.FromArgb(isAvailable && isPrimary ? "#F7FBF5" : "#777D75");
	}

	private void BuildActionLog(PatientCase patient)
	{
		ActionLogStack.Children.Clear();
		ActionLogCountLabel.Text = $"{patient.ActionLog.Count} registro(s)";

		if (patient.ActionLog.Count == 0)
		{
			ActionLogStack.Children.Add(new Label
			{
				Text = "As escolhas feitas no caso aparecem aqui.",
				TextColor = Color.FromArgb("#777D75"),
				FontSize = 13
			});
			return;
		}

		foreach (var action in patient.ActionLog)
		{
			var textStack = new VerticalStackLayout
			{
				Spacing = 2,
				Children =
				{
					new Label { Text = action.Title, TextColor = Color.FromArgb("#536B50"), FontSize = 13, FontAttributes = FontAttributes.Bold },
					new Label { Text = action.Detail, TextColor = Color.FromArgb(MutedColor), FontSize = 12, LineBreakMode = LineBreakMode.WordWrap }
				}
			};
			var pointsLabel = new Label
			{
				Text = action.PointsLine,
				TextColor = Color.FromArgb(action.Points >= 0 ? "#536B50" : "#9A5B55"),
				FontSize = 12,
				FontAttributes = FontAttributes.Bold,
				VerticalOptions = LayoutOptions.Center,
				HorizontalOptions = LayoutOptions.End
			};
			Grid.SetColumn(pointsLabel, 1);

			ActionLogStack.Children.Add(new Border
			{
				BackgroundColor = Color.FromArgb("#FAFBF8"),
				Stroke = Color.FromArgb("#E1E5DD"),
				StrokeThickness = 1,
				Padding = new Thickness(12, 9),
				StrokeShape = new RoundRectangle { CornerRadius = 10 },
				Content = new Grid
				{
					ColumnDefinitions =
					{
						new ColumnDefinition(GridLength.Star),
						new ColumnDefinition(GridLength.Auto)
					},
					Children =
					{
						textStack,
						pointsLabel
					}
				}
			});
		}
	}

	private void BuildHistory(PatientCase patient)
	{
		HistoryStack.Children.Clear();
		for (var index = 0; index < patient.History.Count; index++)
		{
			var entry = patient.History[index];
			var dot = new Border
			{
				BackgroundColor = Color.FromArgb(ActiveColor),
				StrokeThickness = 0,
				HeightRequest = 14,
				WidthRequest = 14,
				StrokeShape = new RoundRectangle { CornerRadius = 7 },
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Start,
				Margin = new Thickness(0, 18, 0, 0)
			};

			var line = new BoxView
			{
				Color = Color.FromArgb(index == patient.History.Count - 1 ? "#00FFFFFF" : "#DDE7DA"),
				WidthRequest = 3,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Fill
			};
			Grid.SetRow(line, 0);
			Grid.SetRowSpan(line, 2);

			var timelineRail = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				},
				Children =
				{
					line,
					dot
				}
			};

			var card = new Border
			{
				BackgroundColor = Color.FromArgb("#F7FAF5"),
				Stroke = Color.FromArgb("#DDE7DA"),
				StrokeThickness = 1,
				Padding = new Thickness(16, 14),
				StrokeShape = new RoundRectangle { CornerRadius = 16 },
				Content = new VerticalStackLayout
				{
					Spacing = 7,
					Children =
					{
						new Label { Text = entry.Date, TextColor = Color.FromArgb("#536B50"), FontSize = 15, FontAttributes = FontAttributes.Bold },
						new Label { Text = entry.Text, TextColor = Color.FromArgb(MutedColor), FontSize = 14, LineBreakMode = LineBreakMode.WordWrap },
						new Label { Text = "Registro do caso", TextColor = Color.FromArgb("#777D75"), FontSize = 12 }
					}
				}
			};
			Grid.SetColumn(card, 1);

			HistoryStack.Children.Add(new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(new GridLength(28)),
					new ColumnDefinition(GridLength.Star)
				},
				ColumnSpacing = 8,
				Children =
				{
					timelineRail,
					card
				}
			});
		}
	}

	private void OnBackToPatientsTapped(object sender, TappedEventArgs e)
	{
		PatientDetailView.IsVisible = false;
		PatientListView.IsVisible = true;
		ActionMenu.IsVisible = false;
	}

	private void OnSummaryTabTapped(object sender, TappedEventArgs e) => SetActiveTab(PatientTab.Summary);

	private void OnHistoryTabTapped(object sender, TappedEventArgs e) => SetActiveTab(PatientTab.History);

	private void OnNotesTabTapped(object sender, TappedEventArgs e) => SetActiveTab(PatientTab.Notes);

	private void SetActiveTab(PatientTab tab)
	{
		SummaryContent.IsVisible = tab == PatientTab.Summary;
		HistoryContent.IsVisible = tab == PatientTab.History;
		NotesContent.IsVisible = tab == PatientTab.Notes;

		SummaryTabLabel.TextColor = Color.FromArgb(tab == PatientTab.Summary ? ActiveColor : MutedColor);
		HistoryTabLabel.TextColor = Color.FromArgb(tab == PatientTab.History ? ActiveColor : MutedColor);
		NotesTabLabel.TextColor = Color.FromArgb(tab == PatientTab.Notes ? ActiveColor : MutedColor);

		SummaryTabLine.Color = Color.FromArgb(tab == PatientTab.Summary ? ActiveColor : HiddenLine);
		HistoryTabLine.Color = Color.FromArgb(tab == PatientTab.History ? ActiveColor : HiddenLine);
		NotesTabLine.Color = Color.FromArgb(tab == PatientTab.Notes ? ActiveColor : HiddenLine);
	}

	private void OnPatientNotesChanged(object sender, TextChangedEventArgs e)
	{
		if (isLoadingPatientNotes || selectedPatient is null || selectedPatient.IsFinished)
		{
			return;
		}

		selectedPatient.Notes = e.NewTextValue ?? string.Empty;
		RefreshSelectedPatientUi();
	}

	private async void OnActionsTapped(object sender, TappedEventArgs e)
	{
		if (selectedPatient is null)
		{
			return;
		}

		if (selectedPatient.IsFinished)
		{
			await ModalHost.ShowMessageAsync("Caso finalizado", selectedPatient.FinalReport);
			return;
		}

		RefreshSelectedPatientUi();
		ActionMenu.IsVisible = true;
	}

	private void OnExamActionTapped(object sender, TappedEventArgs e) => OnExamClicked(sender, EventArgs.Empty);

	private void OnDiagnoseActionTapped(object sender, TappedEventArgs e) => OnDiagnoseClicked(sender, EventArgs.Empty);

	private void OnTreatmentActionTapped(object sender, TappedEventArgs e) => OnTreatmentClicked(sender, EventArgs.Empty);

	private void OnFinishActionTapped(object sender, TappedEventArgs e) => OnFinishCaseClicked(sender, EventArgs.Empty);

	private async void OnExamClicked(object sender, EventArgs e)
	{
		if (selectedPatient is null)
		{
			return;
		}

		if (await ShowFinishedCaseIfNeededAsync())
		{
			return;
		}

		if (await ShowPendingActionIfNeededAsync())
		{
			return;
		}

		if (!selectedPatient.CanStartExam)
		{
			await ModalHost.ShowMessageAsync("Exames", selectedPatient.ExamActionSubtitle);
			return;
		}

		var options = selectedPatient.GetExamOptions().ToArray();
		if (options.Length == 0)
		{
			await ModalHost.ShowMessageAsync("Exames", "Você já solicitou todos os exames disponíveis para este caso.");
			return;
		}

		var choice = await ModalHost.ShowOptionsAsync("Solicitar exame", "Escolha uma opção para registrar no caso.", options);
		if (string.IsNullOrWhiteSpace(choice))
		{
			return;
		}

		if (!selectedPatient.StartPendingAction(PatientActionType.Exam, choice, ActionDelay, out var message))
		{
			await ModalHost.ShowMessageAsync("Ação indisponível", message);
			return;
		}

		BeginActionTimer(selectedPatient);
		BuildHistory(selectedPatient);
		RefreshSelectedPatientUi();
		ActionMenu.IsVisible = true;
	}

	private async void OnDiagnoseClicked(object sender, EventArgs e)
	{
		if (selectedPatient is null)
		{
			return;
		}

		if (await ShowFinishedCaseIfNeededAsync())
		{
			return;
		}

		if (await ShowPendingActionIfNeededAsync())
		{
			return;
		}

		if (!selectedPatient.CanStartDiagnosis)
		{
			await ModalHost.ShowMessageAsync("Hipótese", selectedPatient.DiagnosisActionSubtitle);
			return;
		}

		var choice = await ModalHost.ShowOptionsAsync("Hipótese principal", "Escolha a hipótese que melhor explica o caso.", selectedPatient.DiagnosticOptions);
		if (string.IsNullOrWhiteSpace(choice))
		{
			return;
		}

		if (!selectedPatient.StartPendingAction(PatientActionType.Diagnosis, choice, ActionDelay, out var message))
		{
			await ModalHost.ShowMessageAsync("Ação indisponível", message);
			return;
		}

		BeginActionTimer(selectedPatient);
		BuildHistory(selectedPatient);
		RefreshSelectedPatientUi();
		ActionMenu.IsVisible = true;
	}

	private async void OnTreatmentClicked(object sender, EventArgs e)
	{
		if (selectedPatient is null)
		{
			return;
		}

		if (await ShowFinishedCaseIfNeededAsync())
		{
			return;
		}

		if (await ShowPendingActionIfNeededAsync())
		{
			return;
		}

		if (!selectedPatient.CanStartTreatment)
		{
			await ModalHost.ShowMessageAsync("Conduta", selectedPatient.TreatmentActionSubtitle);
			return;
		}

		var choice = await ModalHost.ShowOptionsAsync("Escolha a conduta", "Registre a conduta mais segura para este treino.", selectedPatient.TreatmentOptions);
		if (string.IsNullOrWhiteSpace(choice))
		{
			return;
		}

		if (!selectedPatient.StartPendingAction(PatientActionType.Treatment, choice, ActionDelay, out var message))
		{
			await ModalHost.ShowMessageAsync("Ação indisponível", message);
			return;
		}

		BeginActionTimer(selectedPatient);
		BuildHistory(selectedPatient);
		RefreshSelectedPatientUi();
		ActionMenu.IsVisible = true;
	}

	private async void OnFinishCaseClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		if (selectedPatient is null)
		{
			return;
		}

		if (await ShowFinishedCaseIfNeededAsync())
		{
			return;
		}

		if (await ShowPendingActionIfNeededAsync())
		{
			return;
		}

		if (!selectedPatient.CanFinishCase)
		{
			await ModalHost.ShowMessageAsync("Finalizar caso", selectedPatient.FinishActionSubtitle);
			return;
		}

		var shouldFinish = await ModalHost.ShowConfirmationAsync(
			"Encerrar caso",
			"Depois de finalizar, as anotações e ações ficam bloqueadas. Deseja gerar a nota final?",
			"Finalizar",
			"Voltar");

		if (!shouldFinish)
		{
			return;
		}

		var report = selectedPatient.FinishCase();
		RefreshSelectedPatientUi();
		SetActiveTab(PatientTab.Summary);
		await ModalHost.ShowMessageAsync("Relatório do caso", report);
	}

	private async Task<bool> ShowFinishedCaseIfNeededAsync()
	{
		if (selectedPatient?.IsFinished != true)
		{
			return false;
		}

		await ModalHost.ShowMessageAsync("Caso finalizado", "Esse caso já foi encerrado. Abra o relatório final para revisar sua nota.");
		RefreshSelectedPatientUi();
		return true;
	}

	private async Task<bool> ShowPendingActionIfNeededAsync()
	{
		if (selectedPatient?.HasPendingAction != true)
		{
			return false;
		}

		ActionMenu.IsVisible = true;
		RefreshSelectedPatientUi();
		await ModalHost.ShowMessageAsync("Ação em andamento", $"{selectedPatient.PendingActionTitle}\n{selectedPatient.PendingActionCountdownLine}");
		return true;
	}

	private void BeginActionTimer(PatientCase patient)
	{
		if (!runningActionTimers.Add(patient))
		{
			return;
		}

		_ = RunActionTimerAsync(patient);
	}

	private async Task RunActionTimerAsync(PatientCase patient)
	{
		try
		{
			while (patient.HasPendingAction)
			{
				await Task.Delay(500);
				PatientActionCompletion? completion = null;

				await MainThread.InvokeOnMainThreadAsync(() =>
				{
					completion = patient.CompletePendingActionIfReady();
					if (selectedPatient == patient)
					{
						BuildHistory(patient);
						RefreshSelectedPatientUi();
					}
				});

				if (completion is not null)
				{
					await MainThread.InvokeOnMainThreadAsync(async () =>
					{
						if (selectedPatient == patient && PatientDetailView.IsVisible && !ModalHost.IsVisible)
						{
							await ModalHost.ShowCaseFeedbackAsync("Retorno do caso", completion.Feedback);
						}
					});
					return;
				}
			}
		}
		finally
		{
			runningActionTimers.Remove(patient);
		}
	}

	private void OnCancelActionsClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
	}

	private enum PatientTab
	{
		Summary,
		History,
		Notes
	}
}

public sealed class PatientCase : INotifyPropertyChanged
{
	private readonly HashSet<string> requestedExams = [];
	private readonly List<PatientActionLogEntry> actionLog = [];
	private readonly List<PatientHistoryEntry> history;
	private int score;
	private string finalReport = string.Empty;
	private bool hasCorrectDiagnosis;
	private bool hasCorrectTreatment;
	private bool isFinished;
	private PendingPatientAction? pendingAction;
	private string? selectedDiagnosis;
	private string? selectedTreatment;
	private string notes;

	public PatientCase(
		string name,
		int age,
		string gender,
		string difficulty,
		string complaint,
		string vitals,
		string allergies,
		string initialCarePlan,
		string correctDiagnosis,
		IReadOnlyList<string> recommendedExams,
		IReadOnlyList<string> diagnosticOptions,
		IReadOnlyList<string> treatmentOptions,
		string correctTreatment,
		IReadOnlyList<PatientHistoryEntry> history,
		string notes)
	{
		Name = name;
		Age = age;
		Gender = gender;
		Difficulty = difficulty;
		Complaint = complaint;
		Vitals = vitals;
		Allergies = allergies;
		InitialCarePlan = initialCarePlan;
		CorrectDiagnosis = correctDiagnosis;
		RecommendedExams = recommendedExams;
		DiagnosticOptions = diagnosticOptions;
		TreatmentOptions = treatmentOptions;
		CorrectTreatment = correctTreatment;
		this.history = history.ToList();
		this.notes = notes;
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	public string Name { get; }

	public string FirstName => Name.Split(' ')[0];

	public int Age { get; }

	public string Gender { get; }

	public string Difficulty { get; }

	public string Complaint { get; }

	public string CaseSummary => $"{Age} anos | {Complaint}";

	public string Vitals { get; }

	public string Allergies { get; }

	public string InitialCarePlan { get; }

	public string CorrectDiagnosis { get; }

	public IReadOnlyList<string> RecommendedExams { get; }

	public IReadOnlyList<string> DiagnosticOptions { get; }

	public IReadOnlyList<string> TreatmentOptions { get; }

	public string CorrectTreatment { get; }

	public IReadOnlyList<PatientHistoryEntry> History => history;

	public IReadOnlyList<PatientActionLogEntry> ActionLog => actionLog;

	public bool IsFinished
	{
		get => isFinished;
		private set => SetField(ref isFinished, value);
	}

	public string Notes
	{
		get => notes;
		set
		{
			if (IsFinished)
			{
				return;
			}

			if (SetField(ref notes, value))
			{
				NotifyProgressChanged();
			}
		}
	}

	public string DisplayDiagnosis => string.IsNullOrWhiteSpace(selectedDiagnosis)
		? "Ainda não definido. Use Ações do caso > Registrar hipótese."
		: $"{selectedDiagnosis} {(hasCorrectDiagnosis ? "(correto)" : "(revisar)")}";

	public string DisplayCarePlan => string.IsNullOrWhiteSpace(selectedTreatment)
		? InitialCarePlan
		: selectedTreatment;

	public string StatusLine
	{
		get
		{
			if (IsFinished)
			{
				return hasCorrectDiagnosis ? "Caso concluído com diagnóstico correto" : "Caso concluído para revisão";
			}

			if (HasPendingAction)
			{
				return "Ação em andamento";
			}

			if (!string.IsNullOrWhiteSpace(selectedDiagnosis))
			{
				return string.IsNullOrWhiteSpace(selectedTreatment) ? "Hipótese registrada" : "Conduta registrada";
			}

			if (requestedExams.Count > 0)
			{
				return $"{requestedExams.Count} exame(s) solicitado(s)";
			}

			return "Novo caso";
		}
	}

	public string ScoreLine => $"{score}/100";

	public string RewardLine => IsFinished ? $"+{Math.Max(score, 0)} XP" : "XP ao encerrar";

	public double ProgressValue => score / 100d;

	public int CompletedSteps =>
		(HasExamAction ? 1 : 0) +
		(HasDiagnosisAction ? 1 : 0) +
		(HasTreatmentAction ? 1 : 0);

	public string StepSummary => IsFinished ? $"Nota {FinalGrade}" : $"{CompletedSteps}/3 etapas";

	public string PrimaryActionLabel => IsFinished ? "Ver resumo" : HasPendingAction ? "Em andamento" : "Ações do caso";

	public string HeaderBadgeText => IsFinished ? $"Nota {FinalGrade}" : HasPendingAction ? "Aguarde" : $"{CompletedSteps}/3";

	public bool HasPendingAction => pendingAction is not null;

	private int AvailableExamCount => GetExamOptions().Count();

	public bool CanStartExam => !IsFinished && !HasPendingAction && AvailableExamCount > 0;

	public bool CanStartDiagnosis => !IsFinished && !HasPendingAction && HasExamAction && !HasDiagnosisAction;

	public bool CanStartTreatment => !IsFinished && !HasPendingAction && HasDiagnosisAction && !HasTreatmentAction;

	public bool CanFinishCase => !IsFinished && !HasPendingAction && HasExamAction && HasDiagnosisAction && HasTreatmentAction;

	public string ExamActionTitle => HasPendingAction
		? "Aguarde retorno"
		: AvailableExamCount == 0
			? "Exames completos"
			: HasExamAction ? "Solicitar outro exame" : "Solicitar exame";

	public string ExamActionSubtitle => HasPendingAction
		? "há uma ação em andamento"
		: AvailableExamCount == 0
			? "nenhum exame disponível"
			: $"{AvailableExamCount} opção(ões) disponível(is)";

	public string DiagnosisActionTitle => HasDiagnosisAction ? "Hipótese registrada" : "Registrar hipótese";

	public string DiagnosisActionSubtitle => HasDiagnosisAction
		? "somente uma hipótese por caso"
		: !HasExamAction
			? "solicite um exame primeiro"
			: "+40 se correto";

	public string TreatmentActionTitle => HasTreatmentAction ? "Conduta definida" : "Definir conduta";

	public string TreatmentActionSubtitle => HasTreatmentAction
		? "somente uma conduta por caso"
		: !HasDiagnosisAction
			? "registre a hipótese primeiro"
			: "+30 se seguro";

	public string FinishActionTitle => IsFinished ? "Caso encerrado" : "Finalizar caso";

	public string FinishActionSubtitle => CanFinishCase
		? "gera nota final"
		: HasPendingAction
			? "aguarde a ação atual"
			: "complete exame, hipótese e conduta";

	public string PendingActionTitle => pendingAction?.Title ?? "Nenhuma ação em andamento";

	public string PendingActionDetail => pendingAction?.DetailLine ?? "Escolha uma ação para iniciar o treino.";

	public string PendingActionCountdownLine
	{
		get
		{
			if (pendingAction is null)
			{
				return "Pronto";
			}

			var seconds = Math.Max(0, (int)Math.Ceiling((pendingAction.ReadyAt - DateTime.UtcNow).TotalSeconds));
			return seconds == 0 ? "Finalizando..." : $"{seconds}s";
		}
	}

	public double PendingActionProgress
	{
		get
		{
			if (pendingAction is null)
			{
				return 0;
			}

			var total = Math.Max(1, (pendingAction.ReadyAt - pendingAction.StartedAt).TotalSeconds);
			var elapsed = Math.Clamp((DateTime.UtcNow - pendingAction.StartedAt).TotalSeconds, 0, total);
			return elapsed / total;
		}
	}

	public bool HasExamAction => requestedExams.Count > 0;

	public bool HasDiagnosisAction => !string.IsNullOrWhiteSpace(selectedDiagnosis);

	public bool HasTreatmentAction => !string.IsNullOrWhiteSpace(selectedTreatment);

	public string ListActionLabel => IsFinished ? "Revisar" : "Iniciar";

	public Color CardBackgroundColor => IsFinished ? Color.FromArgb("#F2F5F0") : Colors.White;

	public Color CardStrokeColor => IsFinished ? Color.FromArgb("#BFD0B8") : Color.FromArgb("#E1E5DD");

	public Color StatusTextColor => IsFinished ? Color.FromArgb("#536B50") : Color.FromArgb("#8FAA87");

	public Color ActionBadgeBackgroundColor => IsFinished ? Color.FromArgb("#DDE7DA") : Color.FromArgb("#EEF4EC");

	public Color CaseProgressColor => IsFinished ? Color.FromArgb("#536B50") : Color.FromArgb("#8FAA87");

	public string CaseIconText
	{
		get
		{
			if (CorrectDiagnosis == "Apendicite")
			{
				return "ABD";
			}

			if (CorrectDiagnosis == "Pneumonia")
			{
				return "PUL";
			}

			if (CorrectDiagnosis == "Enxaqueca")
			{
				return "CEF";
			}

			return "FER";
		}
	}

	public Color CaseIconBackgroundColor => CorrectDiagnosis switch
	{
		"Apendicite" => Color.FromArgb("#EEF4EC"),
		"Pneumonia" => Color.FromArgb("#EAF1F2"),
		"Enxaqueca" => Color.FromArgb("#F1EEF4"),
		_ => Color.FromArgb("#F3EFEA")
	};

	public Color CaseIconTextColor => CorrectDiagnosis switch
	{
		"Apendicite" => Color.FromArgb("#536B50"),
		"Pneumonia" => Color.FromArgb("#557276"),
		"Enxaqueca" => Color.FromArgb("#6B6174"),
		_ => Color.FromArgb("#786955")
	};

	public Color DifficultyBackgroundColor
	{
		get
		{
			if (Difficulty.StartsWith("F", StringComparison.OrdinalIgnoreCase))
			{
				return Color.FromArgb("#EEF4EC");
			}

			if (Difficulty.StartsWith("D", StringComparison.OrdinalIgnoreCase))
			{
				return Color.FromArgb("#F3EDEA");
			}

			return Color.FromArgb("#F0F2E8");
		}
	}

	public Color DifficultyTextColor
	{
		get
		{
			if (Difficulty.StartsWith("F", StringComparison.OrdinalIgnoreCase))
			{
				return Color.FromArgb("#536B50");
			}

			if (Difficulty.StartsWith("D", StringComparison.OrdinalIgnoreCase))
			{
				return Color.FromArgb("#815E56");
			}

			return Color.FromArgb("#6F754E");
		}
	}

	public string FinalGrade => score switch
	{
		>= 90 => "S",
		>= 75 => "A",
		>= 60 => "B",
		>= 45 => "C",
		_ => "D"
	};

	public string FinalReport => string.IsNullOrWhiteSpace(finalReport) ? "Caso ainda não finalizado." : finalReport;

	public string FinalReportSummary => IsFinished
		? $"{GetGradeTitle()}\n{score}/100 pontos | {RewardLine}\nDiagnóstico esperado: {CorrectDiagnosis}\nConduta esperada: {CorrectTreatment}"
		: "Finalize o caso para gerar a nota.";

	public string LastActionLine => HasPendingAction
		? $"Em andamento: {PendingActionTitle} - {pendingAction?.Detail}"
		: actionLog.Count == 0
			? "Nenhuma ação registrada ainda."
			: $"{actionLog[^1].Title}: {actionLog[^1].Detail}";

	public string ActionState
	{
		get
		{
			if (IsFinished)
			{
				return $"Finalizado | Nota {FinalGrade} | {score}/100";
			}

			if (HasPendingAction)
			{
				return $"{PendingActionTitle} em andamento | {PendingActionCountdownLine}";
			}

			var examState = requestedExams.Count == 0 ? "exame pendente" : $"{requestedExams.Count} exame(s)";
			var diagnosisState = HasDiagnosisAction ? "hipótese feita" : HasExamAction ? "hipótese pendente" : "hipótese bloqueada";
			var treatmentState = HasTreatmentAction ? "conduta feita" : HasDiagnosisAction ? "conduta pendente" : "conduta bloqueada";
			return $"{examState} | {diagnosisState} | {treatmentState}";
		}
	}

	public bool StartPendingAction(PatientActionType actionType, string detail, TimeSpan duration, out string message)
	{
		if (IsFinished)
		{
			message = "Caso finalizado. Revise o relatório final.";
			return false;
		}

		if (HasPendingAction)
		{
			message = "Já existe uma ação em andamento. Aguarde ela ficar pronta.";
			return false;
		}

		if (actionType == PatientActionType.Exam && requestedExams.Contains(detail))
		{
			message = "Esse exame já foi solicitado.";
			return false;
		}

		if (actionType == PatientActionType.Diagnosis && !HasExamAction)
		{
			message = "Solicite pelo menos um exame antes de registrar a hipótese.";
			return false;
		}

		if (actionType == PatientActionType.Diagnosis && !string.IsNullOrWhiteSpace(selectedDiagnosis))
		{
			message = "Hipótese já registrada. Finalize o caso para ver sua nota ou revise nas anotações.";
			return false;
		}

		if (actionType == PatientActionType.Treatment && !HasDiagnosisAction)
		{
			message = "Registre uma hipótese antes de definir a conduta.";
			return false;
		}

		if (actionType == PatientActionType.Treatment && !string.IsNullOrWhiteSpace(selectedTreatment))
		{
			message = "Conduta já registrada. Finalize o caso para ver sua nota ou revise nas anotações.";
			return false;
		}

		var title = GetPendingActionTitle(actionType);
		var now = DateTime.UtcNow;
		pendingAction = new PendingPatientAction(actionType, title, detail, now, now.Add(duration));
		AddHistoryEntry($"Ação iniciada: {title} - {detail}. Aguardando retorno.");
		NotifyProgressChanged();

		message = $"Ação registrada: {title}. Em cerca de {(int)duration.TotalSeconds}s o retorno fica pronto e entra no histórico.";
		return true;
	}

	public PatientActionCompletion? CompletePendingActionIfReady()
	{
		if (pendingAction is null || DateTime.UtcNow < pendingAction.ReadyAt)
		{
			return null;
		}

		var action = pendingAction;
		pendingAction = null;

		var feedback = action.Type switch
		{
			PatientActionType.Exam => ApplyExam(action.Detail),
			PatientActionType.Diagnosis => ApplyDiagnosis(action.Detail),
			PatientActionType.Treatment => ApplyTreatment(action.Detail),
			_ => "Ação concluída."
		};

		AddHistoryEntry($"Ação concluída: {action.Title} - {action.Detail}. {feedback}");
		NotifyProgressChanged();
		return new PatientActionCompletion(action.Title, feedback);
	}

	public IEnumerable<string> GetExamOptions()
	{
		if (IsFinished)
		{
			return [];
		}

		var extras = new[] { "Glicemia capilar", "Eletrocardiograma", "Urina tipo 1", "Radiografia de tórax" };
		return RecommendedExams.Concat(extras).Distinct().Where(exam => !requestedExams.Contains(exam));
	}

	public string ApplyExam(string exam)
	{
		if (IsFinished)
		{
			return "Caso finalizado. Revise o relatório final.";
		}

		if (!requestedExams.Add(exam))
		{
			return "Esse exame já foi solicitado.";
		}

		if (RecommendedExams.Contains(exam))
		{
			AddScore(20);
			AddAction("Exame", exam, 20);
			NotifyProgressChanged();
			return BuildExamFeedback(exam, true);
		}

		AddScore(-10);
		AddAction("Exame", exam, -10);
		NotifyProgressChanged();
		return BuildExamFeedback(exam, false);
	}

	public string ApplyDiagnosis(string diagnosis)
	{
		if (IsFinished)
		{
			return "Caso finalizado. Revise o relatório final.";
		}

		if (!string.IsNullOrWhiteSpace(selectedDiagnosis))
		{
			return "Diagnóstico já registrado. Finalize o caso para ver sua nota ou revise nas anotações.";
		}

		selectedDiagnosis = diagnosis;
		hasCorrectDiagnosis = string.Equals(diagnosis, CorrectDiagnosis, StringComparison.OrdinalIgnoreCase);
		var points = hasCorrectDiagnosis ? 40 : -20;
		AddScore(points);
		AddAction("Diagnóstico", diagnosis, points);
		NotifyProgressChanged();

		return BuildDiagnosisFeedback(diagnosis, hasCorrectDiagnosis);
	}

	public string ApplyTreatment(string treatment)
	{
		if (IsFinished)
		{
			return "Caso finalizado. Revise o relatório final.";
		}

		if (!string.IsNullOrWhiteSpace(selectedTreatment))
		{
			return "Conduta já registrada. Finalize o caso para ver sua nota ou revise nas anotações.";
		}

		selectedTreatment = treatment;
		hasCorrectTreatment = string.Equals(treatment, CorrectTreatment, StringComparison.OrdinalIgnoreCase);
		var points = hasCorrectTreatment ? 30 : -20;
		AddScore(points);
		AddAction("Tratamento", treatment, points);
		NotifyProgressChanged();

		return BuildTreatmentFeedback(treatment, hasCorrectTreatment);
	}

	public string FinishCase()
	{
		if (IsFinished)
		{
			return FinalReport;
		}

		if (HasPendingAction)
		{
			return "Aguarde a ação em andamento ficar pronta antes de finalizar o caso.";
		}

		if (!HasExamAction || !HasDiagnosisAction || !HasTreatmentAction)
		{
			return "Complete exame, hipótese e conduta antes de finalizar o caso.";
		}

		var finishPoints = 0;
		if (Notes.Length >= 50)
		{
			finishPoints += 10;
		}

		if (requestedExams.Count == 0)
		{
			finishPoints -= 10;
		}

		if (string.IsNullOrWhiteSpace(selectedDiagnosis))
		{
			finishPoints -= 20;
		}

		if (string.IsNullOrWhiteSpace(selectedTreatment))
		{
			finishPoints -= 15;
		}

		AddScore(finishPoints);
		AddAction("Finalização", "Caso encerrado e nota gerada", finishPoints);
		IsFinished = true;
		NotifyProgressChanged();

		finalReport = $"{GetGradeTitle()}\n\nNota: {FinalGrade} | Pontuação: {score}/100\nXP recebido: {Math.Max(score, 0)}\n\nDiagnóstico esperado: {CorrectDiagnosis}\nConduta esperada: {CorrectTreatment}\n\n{NotesFeedback}\n\nAções feitas:\n{BuildActionSummary()}";
		NotifyProgressChanged();
		return finalReport;
	}

	private string NotesFeedback => Notes.Length >= 50
		? "Suas anotações ajudam a mostrar o raciocínio clínico."
		: "Tente anotar hipóteses, dados que sustentam a decisão e sinais de alerta.";

	private string BuildExamFeedback(string exam, bool isPriority)
	{
		if (!isPriority)
		{
			return $"""
			Exame realizado: {exam}

			O paciente permanece com a queixa principal sem mudança relevante. O resultado não trouxe um dado que explique bem o quadro atual nem altera a prioridade imediata.

			Análise do caso: esse exame pode ser útil em outros contextos, mas aqui consome tempo e não ajuda tanto quanto os dados mais ligados a {Complaint.ToLowerInvariant()}.

			Próximo passo: procure um exame que confirme ou descarte a principal suspeita clínica antes de registrar a hipótese.
			""";
		}

		var finding = GetPriorityExamFinding(exam);
		return $"""
		Exame realizado: {exam}

		O paciente foi reavaliado após o exame. {finding}

		Análise do caso: esse resultado se relaciona com a queixa principal e ajuda a sustentar uma hipótese mais segura.

		Próximo passo: compare esse dado com sinais vitais, histórico e evolução antes de escolher a hipótese.
		""";
	}

	private string BuildDiagnosisFeedback(string diagnosis, bool isCorrect)
	{
		if (isCorrect)
		{
			return $"""
			Hipótese registrada: {diagnosis}

			A equipe revisou os dados do caso, e a hipótese ficou coerente com a evolução do paciente, os sinais observados e os exames solicitados.

			O que mudou no caso: agora a prioridade deixa de ser apenas investigar e passa a ser escolher uma conduta segura para reduzir risco e evitar piora.

			Próximo passo: pense em uma conduta que proteja o paciente enquanto ele segue em avaliação.
			""";
		}

		return $"""
		Hipótese registrada: {diagnosis}

		A hipótese não explica bem todos os dados do caso. Alguns sinais continuam sem encaixe, principalmente a queixa "{Complaint}" e a evolução registrada no histórico.

		O que observar: o caso aponta mais para {CorrectDiagnosis}. Vale revisar os exames e os sinais de alerta antes de definir a conduta.

		Próximo passo: evite escolher tratamento baseado em uma hipótese fraca.
		""";
	}

	private string BuildTreatmentFeedback(string treatment, bool isCorrect)
	{
		if (isCorrect)
		{
			return $"""
			Conduta definida: {treatment}

			Após a conduta, o paciente fica mais seguro para acompanhamento. A escolha prioriza risco, monitoramento e comunicação adequada com a equipe.

			Impacto no paciente: a chance de piora sem observação diminui, e os próximos passos ficam mais claros para a continuidade do cuidado.

			Próximo passo: finalize o caso e revise sua nota para ver se investigação, hipótese e conduta ficaram alinhadas.
			""";
		}

		return $"""
		Conduta definida: {treatment}

		Após essa escolha, ainda existe risco clínico importante sem cobertura adequada. A conduta não responde bem ao problema principal e pode atrasar uma intervenção mais segura.

		Impacto no paciente: o paciente continua precisando de observação e reavaliação, porque a escolha não resolve os pontos de maior prioridade.

		Próximo passo: compare a conduta escolhida com a hipótese {CorrectDiagnosis} e com os sinais de risco antes de finalizar.
		""";
	}

	private string GetPriorityExamFinding(string exam)
	{
		if (CorrectDiagnosis == "Apendicite")
		{
			if (exam.Contains("Hemograma", StringComparison.OrdinalIgnoreCase))
			{
				return "O hemograma sugere processo infeccioso ou inflamatório, com leucócitos elevados. A dor em fossa ilíaca direita ganha mais peso na análise.";
			}

			if (exam.Contains("físico", StringComparison.OrdinalIgnoreCase) ||
				exam.Contains("fisico", StringComparison.OrdinalIgnoreCase))
			{
				return "No exame abdominal, há dor localizada em fossa ilíaca direita, com piora à palpação. Esse sinal aumenta a suspeita de irritação peritoneal.";
			}

			return "O ultrassom mostra sinais compatíveis com inflamação apendicular. A combinação com febre e dor localizada reforça a prioridade cirúrgica.";
		}

		if (CorrectDiagnosis == "Pneumonia")
		{
			if (exam.Contains("Ausculta", StringComparison.OrdinalIgnoreCase))
			{
				return "A ausculta mostra ruídos adventícios em base pulmonar e padrão respiratório mais cansado. Isso reforça o foco respiratório.";
			}

			if (exam.Contains("Radiografia", StringComparison.OrdinalIgnoreCase))
			{
				return "A radiografia sugere infiltrado pulmonar. Esse resultado combina com tosse produtiva, febre baixa e saturação limítrofe.";
			}

			return "A oximetria seriada mostra saturação oscilando em torno de 94%, com piora aos esforços leves. O acompanhamento respiratório ganha prioridade.";
		}

		if (CorrectDiagnosis == "Enxaqueca")
		{
			if (exam.Contains("dor", StringComparison.OrdinalIgnoreCase))
			{
				return "A escala de dor registra intensidade alta, com piora à luz e ao ruído. O padrão é compatível com crise de cefaleia primária.";
			}

			if (exam.Contains("neurológica", StringComparison.OrdinalIgnoreCase) ||
				exam.Contains("neurologica", StringComparison.OrdinalIgnoreCase))
			{
				return "A avaliação neurológica não mostra déficit focal no momento. Isso reduz sinais de urgência neurológica, mas mantém a necessidade de reavaliação.";
			}

			return "Não aparecem sinais de alarme, como rigidez de nuca, febre ou déficit focal. O quadro fica mais consistente com enxaqueca.";
		}

		if (CorrectDiagnosis == "Infecção de sítio cirúrgico")
		{
			if (exam.Contains("ferida", StringComparison.OrdinalIgnoreCase))
			{
				return "A ferida apresenta hiperemia ao redor, dor local e pequena secreção. O aspecto sugere processo infeccioso em evolução.";
			}

			if (exam.Contains("Temperatura", StringComparison.OrdinalIgnoreCase))
			{
				return "A temperatura segue em elevação discreta, junto de taquicardia leve. O padrão reforça a vigilância para infecção.";
			}

			return "A coleta de secreção pode ajudar a direcionar a conduta se houver piora ou necessidade de terapia específica.";
		}

		return "O resultado acrescenta uma pista relevante e deve ser interpretado junto com a evolução e os sinais vitais.";
	}

	private void AddScore(int points)
	{
		score = Math.Clamp(score + points, 0, 100);
		OnPropertyChanged(nameof(ScoreLine));
		OnPropertyChanged(nameof(RewardLine));
		OnPropertyChanged(nameof(FinalGrade));
		OnPropertyChanged(nameof(FinalReportSummary));
	}

	private void AddAction(string title, string detail, int points)
	{
		actionLog.Add(new PatientActionLogEntry(title, detail, points));
		OnPropertyChanged(nameof(ActionLog));
		OnPropertyChanged(nameof(LastActionLine));
	}

	private void AddHistoryEntry(string text)
	{
		history.Add(new PatientHistoryEntry(DateTime.Now.ToString("HH:mm:ss"), text));
		OnPropertyChanged(nameof(History));
	}

	private static string GetPendingActionTitle(PatientActionType actionType)
	{
		return actionType switch
		{
			PatientActionType.Exam => "Solicitação de exame",
			PatientActionType.Diagnosis => "Análise diagnóstica",
			PatientActionType.Treatment => "Plano de conduta",
			_ => "Ação do caso"
		};
	}

	private string GetGradeTitle()
	{
		return score switch
		{
			>= 90 => "Excelente conduta",
			>= 75 => "Boa conduta",
			>= 60 => "Conduta segura, mas pode melhorar",
			>= 45 => "Conduta parcial",
			_ => "Precisa revisar"
		};
	}

	private string BuildActionSummary()
	{
		return actionLog.Count == 0
			? "Nenhuma ação registrada."
			: string.Join("\n", actionLog.Select(action => $"- {action.Title}: {action.Detail} ({action.PointsLine})"));
	}

	private void NotifyProgressChanged()
	{
		OnPropertyChanged(nameof(StatusLine));
		OnPropertyChanged(nameof(ScoreLine));
		OnPropertyChanged(nameof(RewardLine));
		OnPropertyChanged(nameof(ProgressValue));
		OnPropertyChanged(nameof(CompletedSteps));
		OnPropertyChanged(nameof(StepSummary));
		OnPropertyChanged(nameof(PrimaryActionLabel));
		OnPropertyChanged(nameof(HeaderBadgeText));
		OnPropertyChanged(nameof(HasPendingAction));
		OnPropertyChanged(nameof(PendingActionTitle));
		OnPropertyChanged(nameof(PendingActionDetail));
		OnPropertyChanged(nameof(PendingActionCountdownLine));
		OnPropertyChanged(nameof(PendingActionProgress));
		OnPropertyChanged(nameof(CanStartExam));
		OnPropertyChanged(nameof(CanStartDiagnosis));
		OnPropertyChanged(nameof(CanStartTreatment));
		OnPropertyChanged(nameof(CanFinishCase));
		OnPropertyChanged(nameof(ExamActionTitle));
		OnPropertyChanged(nameof(ExamActionSubtitle));
		OnPropertyChanged(nameof(DiagnosisActionTitle));
		OnPropertyChanged(nameof(DiagnosisActionSubtitle));
		OnPropertyChanged(nameof(TreatmentActionTitle));
		OnPropertyChanged(nameof(TreatmentActionSubtitle));
		OnPropertyChanged(nameof(FinishActionTitle));
		OnPropertyChanged(nameof(FinishActionSubtitle));
		OnPropertyChanged(nameof(ActionState));
		OnPropertyChanged(nameof(DisplayDiagnosis));
		OnPropertyChanged(nameof(DisplayCarePlan));
		OnPropertyChanged(nameof(HasExamAction));
		OnPropertyChanged(nameof(HasDiagnosisAction));
		OnPropertyChanged(nameof(HasTreatmentAction));
		OnPropertyChanged(nameof(ListActionLabel));
		OnPropertyChanged(nameof(CardBackgroundColor));
		OnPropertyChanged(nameof(CardStrokeColor));
		OnPropertyChanged(nameof(StatusTextColor));
		OnPropertyChanged(nameof(ActionBadgeBackgroundColor));
		OnPropertyChanged(nameof(CaseProgressColor));
		OnPropertyChanged(nameof(CaseIconText));
		OnPropertyChanged(nameof(CaseIconBackgroundColor));
		OnPropertyChanged(nameof(CaseIconTextColor));
		OnPropertyChanged(nameof(DifficultyBackgroundColor));
		OnPropertyChanged(nameof(DifficultyTextColor));
		OnPropertyChanged(nameof(FinalGrade));
		OnPropertyChanged(nameof(FinalReport));
		OnPropertyChanged(nameof(FinalReportSummary));
		OnPropertyChanged(nameof(LastActionLine));
	}

	private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return false;
		}

		field = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}

public sealed record PatientHistoryEntry(string Date, string Text);

public sealed record PatientActionLogEntry(string Title, string Detail, int Points)
{
	public string PointsLine => Points > 0 ? $"+{Points}" : Points.ToString();
}

public sealed record PatientActionCompletion(string Title, string Feedback);

public sealed record PendingPatientAction(
	PatientActionType Type,
	string Title,
	string Detail,
	DateTime StartedAt,
	DateTime ReadyAt)
{
	public string DetailLine => $"{Detail} | retorno em andamento";
}

public enum PatientActionType
{
	Exam,
	Diagnosis,
	Treatment
}
