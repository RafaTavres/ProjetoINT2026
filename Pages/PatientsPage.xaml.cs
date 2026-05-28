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

	private PatientCase? selectedPatient;
	private bool isLoadingPatientNotes;

	public PatientsPage()
	{
		InitializeComponent();
		BindingContext = this;
	}

	public ObservableCollection<PatientCase> Patients { get; } =
	[
		new(
			"Carlos Silva",
			22,
			"Masculino",
			"Media",
			"Dor abdominal, nausea e febre",
			"PA: 120/80 mmHg\nFC: 96 bpm | FR: 20 irpm\nTemp.: 38,1 C | SpO2: 98%",
			"Sem alergias conhecidas",
			"Monitorar dor, temperatura e sinais de piora abdominal.",
			"Apendicite",
			["Hemograma", "Exame fisico abdominal", "Ultrassom abdominal"],
			["Gastroenterite", "Apendicite", "Colica renal", "Infeccao urinaria"],
			["Manter jejum, acesso venoso e comunicar equipe medica", "Antibiotico sem avaliacao", "Alta com analgesico", "Curativo simples"],
			"Manter jejum, acesso venoso e comunicar equipe medica",
			[
				new("08:30", "Paciente relata dor em fossa iliaca direita, nausea e febre desde a madrugada."),
				new("09:10", "Dor piora ao caminhar. Sem vomitos no momento."),
				new("09:35", "Exame fisico abdominal pode orientar a prioridade do caso.")
			],
			"Observe sinais de irritacao peritoneal e evite fechar conduta sem exame fisico."),
		new(
			"Mateus Oliveira",
			34,
			"Masculino",
			"Facil",
			"Tosse produtiva e dispneia leve",
			"PA: 130/84 mmHg\nFC: 92 bpm | FR: 22 irpm\nTemp.: 37,8 C | SpO2: 94%",
			"Nao informado",
			"Elevar cabeceira, observar padrao respiratorio e saturacao.",
			"Pneumonia",
			["Ausculta pulmonar", "Radiografia de torax", "Oximetria seriada"],
			["Pneumonia", "Migranea", "Apendicite", "Hipoglicemia"],
			["Monitorar SpO2 e comunicar piora respiratoria", "Liberar sem orientacao", "Reduzir ingesta hidrica", "Aplicar curativo compressivo"],
			"Monitorar SpO2 e comunicar piora respiratoria",
			[
				new("09:00", "Relata tosse ha cinco dias, com secrecao amarelada."),
				new("10:20", "Ausculta com roncos difusos. Saturacao limitrofe em ar ambiente."),
				new("11:00", "Refere cansaco aos esforcos leves.")
			],
			"Priorize avaliacao respiratoria, saturacao e sinais de desconforto."),
		new(
			"Ana Beatriz",
			28,
			"Feminino",
			"Media",
			"Cefaleia intensa e fotofobia",
			"PA: 118/76 mmHg\nFC: 84 bpm | FR: 17 irpm\nTemp.: 36,5 C | SpO2: 99%",
			"Dipirona",
			"Ambiente calmo, avaliar escala de dor e sinais neurologicos.",
			"Migranea",
			["Escala de dor", "Avaliacao neurologica", "Sinais de alarme"],
			["Meningite", "Migranea", "Crise hipertensiva", "Sinusite"],
			["Reduzir estimulos, hidratar e reavaliar dor", "Antibiotico imediato", "Alta sem reavaliacao", "Jejum absoluto prolongado"],
			"Reduzir estimulos, hidratar e reavaliar dor",
			[
				new("10:15", "Paciente refere cefaleia pulsatil ha 6 horas, com fotofobia."),
				new("10:45", "Sem febre e sem rigidez de nuca relatada."),
				new("11:20", "Dor melhora parcialmente em ambiente escuro.")
			],
			"Diferencie migranea de sinais de alarme neurologicos."),
		new(
			"Julia Costa",
			41,
			"Feminino",
			"Dificil",
			"Ferida operatoria com hiperemia",
			"PA: 126/82 mmHg\nFC: 102 bpm | FR: 18 irpm\nTemp.: 37,9 C | SpO2: 98%",
			"Latex",
			"Inspecionar ferida, registrar aspecto e trocar curativo com tecnica limpa.",
			"Infeccao de sitio cirurgico",
			["Inspecao da ferida", "Temperatura seriada", "Cultura se secrecao"],
			["Dermatite por contato", "Infeccao de sitio cirurgico", "Migranea", "Bronquite"],
			["Comunicar sinais infecciosos e reforcar cuidado com curativo", "Cobrir sem avaliar", "Retirar pontos sem indicacao", "Ignorar alergia a latex"],
			"Comunicar sinais infecciosos e reforcar cuidado com curativo",
			[
				new("07:40", "Curativo com pequena secrecao serosa e hiperemia ao redor."),
				new("09:00", "Paciente relata dor local crescente desde ontem."),
				new("11:30", "Temperatura em elevacao discreta.")
			],
			"Observe secrecao, bordas, odor, dor, febre e alergias antes da conduta.")
	];

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
		PatientNameLabel.Text = patient.FirstName;
		PatientMetaLabel.Text = $"{patient.Age} anos | {patient.Gender} | {patient.Difficulty}";
		VitalsLabel.Text = patient.Vitals;
		ComplaintLabel.Text = patient.Complaint;

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
		CaseScoreLabel.Text = selectedPatient.ScoreLine;
		CaseActionStateLabel.Text = selectedPatient.ActionState;
		CaseRewardLabel.Text = selectedPatient.RewardLine;
		DiagnosisLabel.Text = selectedPatient.DisplayDiagnosis;
		CareLabel.Text = selectedPatient.DisplayCarePlan;
	}

	private void BuildHistory(PatientCase patient)
	{
		HistoryStack.Children.Clear();
		foreach (var entry in patient.History)
		{
			HistoryStack.Children.Add(new Border
			{
				BackgroundColor = Color.FromArgb("#EEF4EC"),
				Stroke = Color.FromArgb("#DDE7DA"),
				StrokeThickness = 1,
				Padding = new Thickness(16, 14),
				StrokeShape = new RoundRectangle { CornerRadius = 12 },
				Content = new VerticalStackLayout
				{
					Spacing = 8,
					Children =
					{
						new Label { Text = entry.Date, TextColor = Color.FromArgb(MutedColor), FontSize = 14, FontAttributes = FontAttributes.Bold },
						new Label { Text = entry.Text, TextColor = Color.FromArgb(MutedColor), FontSize = 14, LineBreakMode = LineBreakMode.WordWrap },
						new Label { Text = "Enf. Florence", TextColor = Color.FromArgb("#536B50"), FontSize = 13 }
					}
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
		if (isLoadingPatientNotes || selectedPatient is null)
		{
			return;
		}

		selectedPatient.Notes = e.NewTextValue ?? string.Empty;
		RefreshSelectedPatientUi();
	}

	private void OnActionsTapped(object sender, TappedEventArgs e)
	{
		ActionMenu.IsVisible = true;
	}

	private async void OnExamClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		if (selectedPatient is null)
		{
			return;
		}

		var options = selectedPatient.GetExamOptions().ToArray();
		var choice = await DisplayActionSheet("Solicitar exame", "Cancelar", null, options);
		if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar")
		{
			return;
		}

		var feedback = selectedPatient.ApplyExam(choice);
		RefreshSelectedPatientUi();
		await DisplayAlert("Resultado do exame", feedback, "OK");
	}

	private async void OnDiagnoseClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		if (selectedPatient is null)
		{
			return;
		}

		var choice = await DisplayActionSheet("Qual sua hipotese principal?", "Cancelar", null, selectedPatient.DiagnosticOptions.ToArray());
		if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar")
		{
			return;
		}

		var feedback = selectedPatient.ApplyDiagnosis(choice);
		RefreshSelectedPatientUi();
		await DisplayAlert("Diagnostico", feedback, "OK");
	}

	private async void OnTreatmentClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		if (selectedPatient is null)
		{
			return;
		}

		var choice = await DisplayActionSheet("Escolha a conduta", "Cancelar", null, selectedPatient.TreatmentOptions.ToArray());
		if (string.IsNullOrWhiteSpace(choice) || choice == "Cancelar")
		{
			return;
		}

		var feedback = selectedPatient.ApplyTreatment(choice);
		RefreshSelectedPatientUi();
		await DisplayAlert("Conduta", feedback, "OK");
	}

	private async void OnFinishCaseClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		if (selectedPatient is null)
		{
			return;
		}

		var report = selectedPatient.FinishCase();
		RefreshSelectedPatientUi();
		await DisplayAlert("Relatorio do caso", report, "OK");
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
	private int score;
	private bool hasCorrectDiagnosis;
	private bool hasCorrectTreatment;
	private bool isFinished;
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
		History = history;
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

	public IReadOnlyList<PatientHistoryEntry> History { get; }

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
			if (SetField(ref notes, value))
			{
				NotifyProgressChanged();
			}
		}
	}

	public string DisplayDiagnosis => string.IsNullOrWhiteSpace(selectedDiagnosis)
		? "Ainda nao definido. Use Acoes > Diagnosticar para registrar sua hipotese."
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
				return hasCorrectDiagnosis ? "Caso concluido com diagnostico correto" : "Caso concluido para revisao";
			}

			if (!string.IsNullOrWhiteSpace(selectedDiagnosis))
			{
				return "Diagnostico registrado";
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

	public string ActionState
	{
		get
		{
			var examState = requestedExams.Count == 0 ? "sem exames" : $"{requestedExams.Count} exame(s)";
			var diagnosisState = string.IsNullOrWhiteSpace(selectedDiagnosis) ? "sem diagnostico" : "diagnostico feito";
			var treatmentState = string.IsNullOrWhiteSpace(selectedTreatment) ? "sem conduta" : "conduta feita";
			return $"{examState} | {diagnosisState} | {treatmentState}";
		}
	}

	public IEnumerable<string> GetExamOptions()
	{
		var extras = new[] { "Glicemia capilar", "Eletrocardiograma", "Urina tipo 1", "Radiografia de torax" };
		return RecommendedExams.Concat(extras).Distinct().Where(exam => !requestedExams.Contains(exam));
	}

	public string ApplyExam(string exam)
	{
		if (!requestedExams.Add(exam))
		{
			return "Esse exame ja foi solicitado.";
		}

		if (RecommendedExams.Contains(exam))
		{
			AddScore(15);
			NotifyProgressChanged();
			return "Boa escolha. Esse dado ajuda a sustentar o raciocinio do caso.";
		}

		AddScore(-5);
		NotifyProgressChanged();
		return "Exame pouco prioritario para este caso. Pense se ele muda sua conduta agora.";
	}

	public string ApplyDiagnosis(string diagnosis)
	{
		selectedDiagnosis = diagnosis;
		hasCorrectDiagnosis = string.Equals(diagnosis, CorrectDiagnosis, StringComparison.OrdinalIgnoreCase);
		AddScore(hasCorrectDiagnosis ? 35 : -15);
		NotifyProgressChanged();

		return hasCorrectDiagnosis
			? "Diagnostico correto. Agora escolha uma conduta segura."
			: $"Hipotese registrada, mas o caso aponta para {CorrectDiagnosis}. Revise os dados antes de encerrar.";
	}

	public string ApplyTreatment(string treatment)
	{
		selectedTreatment = treatment;
		hasCorrectTreatment = string.Equals(treatment, CorrectTreatment, StringComparison.OrdinalIgnoreCase);
		AddScore(hasCorrectTreatment ? 25 : -15);
		NotifyProgressChanged();

		return hasCorrectTreatment
			? "Conduta adequada para o treino. Boa priorizacao de seguranca."
			: "Conduta registrada, mas existe risco ou baixa prioridade nessa escolha.";
	}

	public string FinishCase()
	{
		if (Notes.Length >= 50)
		{
			AddScore(10);
		}

		if (requestedExams.Count == 0)
		{
			AddScore(-10);
		}

		if (string.IsNullOrWhiteSpace(selectedDiagnosis))
		{
			AddScore(-20);
		}

		if (string.IsNullOrWhiteSpace(selectedTreatment))
		{
			AddScore(-15);
		}

		IsFinished = true;
		NotifyProgressChanged();

		var grade = score switch
		{
			>= 90 => "Excelente conduta",
			>= 70 => "Boa conduta",
			>= 50 => "Conduta parcial",
			_ => "Precisa revisar"
		};

		return $"{grade}\n\nPontuacao: {score}/100\nDiagnostico esperado: {CorrectDiagnosis}\nConduta esperada: {CorrectTreatment}\n\n{NotesFeedback}";
	}

	private string NotesFeedback => Notes.Length >= 50
		? "Suas anotacoes ajudam a mostrar o raciocinio clinico."
		: "Tente anotar hipoteses, dados que sustentam a decisao e sinais de alerta.";

	private void AddScore(int points)
	{
		score = Math.Clamp(score + points, 0, 100);
		OnPropertyChanged(nameof(ScoreLine));
		OnPropertyChanged(nameof(RewardLine));
	}

	private void NotifyProgressChanged()
	{
		OnPropertyChanged(nameof(StatusLine));
		OnPropertyChanged(nameof(ScoreLine));
		OnPropertyChanged(nameof(RewardLine));
		OnPropertyChanged(nameof(ActionState));
		OnPropertyChanged(nameof(DisplayDiagnosis));
		OnPropertyChanged(nameof(DisplayCarePlan));
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
