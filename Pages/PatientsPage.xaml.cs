using System.Collections.ObjectModel;
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
			"Dor abdominal, nausea e febre",
			"PA: 120/80 mmHg\nFC: 78 bpm | FR: 18 irpm\nTemp.: 36,7 C | SpO2: 98%",
			"Nao informado",
			"Monitorar sinais vitais\nAdministrar medicacao conforme prescricao",
			[
				new("22/05/2024 08:30", "Paciente relatou dor abdominal, nauseas e febre. Aguardando exames."),
				new("22/05/2024 14:10", "Administrado dipirona 1g EV conforme prescricao."),
				new("22/05/2024 18:45", "Evoluiu bem, dor reduzida. Sinais vitais estaveis.")
			],
			"Plano de cuidados\n- Monitorar dor abdominal\n- Reavaliar temperatura\n\nHipoteses\n- Gastroenterite\n- Apendicite inicial"),
		new(
			"Mateus Oliveira",
			34,
			"Masculino",
			"Tosse produtiva e dispneia leve",
			"PA: 130/84 mmHg\nFC: 92 bpm | FR: 22 irpm\nTemp.: 37,8 C | SpO2: 94%",
			"Nao informado",
			"Elevar cabeceira\nObservar padrao respiratorio",
			[
				new("23/05/2024 09:00", "Relata tosse ha cinco dias, piora ao deitar."),
				new("23/05/2024 11:30", "Ausculta com roncos difusos. Mantem saturacao limítrofe.")
			],
			"Solicitar RX se piora respiratoria.\nRegistrar resposta a broncodilatador."),
		new(
			"Ana Beatriz",
			28,
			"Feminino",
			"Cefaleia intensa e fotofobia",
			"PA: 118/76 mmHg\nFC: 84 bpm | FR: 17 irpm\nTemp.: 36,5 C | SpO2: 99%",
			"Dipirona",
			"Ambiente calmo\nAvaliar sinais neurologicos",
			[
				new("24/05/2024 10:15", "Paciente refere cefaleia pulsátil ha 6 horas."),
				new("24/05/2024 12:00", "Sem rigidez de nuca. Orientada hidratacao.")
			],
			"Diferenciar migranea de sinais de alarme.\nAnotar escala de dor."),
		new(
			"Julia Costa",
			41,
			"Feminino",
			"Ferida operatória com hiperemia",
			"PA: 126/82 mmHg\nFC: 88 bpm | FR: 18 irpm\nTemp.: 37,4 C | SpO2: 98%",
			"Latex",
			"Inspecionar ferida\nRegistrar aspecto do curativo",
			[
				new("25/05/2024 07:40", "Curativo com pequena secrecao serosa."),
				new("25/05/2024 13:20", "Sem febre. Mantem dor local leve.")
			],
			"Observar sinais flogisticos.\nChecar tecnica de curativo.")
	];

	private void OnPatientTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: PatientCase patient })
		{
			return;
		}

		OpenPatient(patient);
	}

	private void OpenPatient(PatientCase patient)
	{
		selectedPatient = patient;
		PatientNameLabel.Text = patient.FirstName;
		PatientMetaLabel.Text = $"{patient.Age} anos | {patient.Gender}";
		VitalsLabel.Text = patient.Vitals;
		ComplaintLabel.Text = patient.Complaint;
		DiagnosisLabel.Text = patient.Diagnosis;
		CareLabel.Text = patient.CarePlan;

		isLoadingPatientNotes = true;
		PatientNotesEditor.Text = patient.Notes;
		isLoadingPatientNotes = false;

		BuildHistory(patient);
		SetActiveTab(PatientTab.Summary);
		PatientListView.IsVisible = false;
		PatientDetailView.IsVisible = true;
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
	}

	private void OnActionsTapped(object sender, TappedEventArgs e)
	{
		ActionMenu.IsVisible = true;
	}

	private async void OnDiagnoseClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		await DisplayAlert("Diagnosticar", "Registre sua hipotese diagnostica nas anotacoes do paciente.", "OK");
		SetActiveTab(PatientTab.Notes);
	}

	private async void OnTreatmentClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		await DisplayAlert("Receitar tratamento", "Defina condutas e cuidados esperados para o caso.", "OK");
		SetActiveTab(PatientTab.Notes);
	}

	private async void OnExamClicked(object sender, EventArgs e)
	{
		ActionMenu.IsVisible = false;
		await DisplayAlert("Solicitar exame", "Escolha exames coerentes com a hipotese e justifique nas anotacoes.", "OK");
		SetActiveTab(PatientTab.Notes);
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

public sealed class PatientCase(
	string name,
	int age,
	string gender,
	string complaint,
	string vitals,
	string diagnosis,
	string carePlan,
	IReadOnlyList<PatientHistoryEntry> history,
	string notes)
{
	public string Name { get; } = name;

	public string FirstName => Name.Split(' ')[0];

	public int Age { get; } = age;

	public string Gender { get; } = gender;

	public string Complaint { get; } = complaint;

	public string CaseSummary => $"{Age} anos | {Complaint}";

	public string Vitals { get; } = vitals;

	public string Diagnosis { get; } = diagnosis;

	public string CarePlan { get; } = carePlan;

	public IReadOnlyList<PatientHistoryEntry> History { get; } = history;

	public string Notes { get; set; } = notes;
}

public sealed record PatientHistoryEntry(string Date, string Text);
