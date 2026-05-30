namespace ProjetoINT2026.Controls;

public partial class BottomNavigationBar : ContentView
{
	private const string ActiveColor = "#536B50";
	private const string ActivePillColor = "#EEF4EC";
	private const string TransparentColor = "#00FFFFFF";
	private const string MutedColor = "#747A70";

	public static readonly BindableProperty ActiveTabProperty = BindableProperty.Create(
		nameof(ActiveTab),
		typeof(string),
		typeof(BottomNavigationBar),
		"Notes",
		propertyChanged: OnActiveTabChanged);

	public BottomNavigationBar()
	{
		InitializeComponent();
		UpdateActiveTab();
	}

	public string ActiveTab
	{
		get => (string)GetValue(ActiveTabProperty);
		set => SetValue(ActiveTabProperty, value);
	}

	private static void OnActiveTabChanged(BindableObject bindable, object oldValue, object newValue)
	{
		((BottomNavigationBar)bindable).UpdateActiveTab();
	}

	private void UpdateActiveTab()
	{
		ApplyTabState(PatientsPill, PatientsIcon, PatientsLabel, ActiveTab == "Patients");
		ApplyTabState(HomePill, HomeIcon, HomeLabel, ActiveTab == "Home");
		ApplyTabState(ChatPill, ChatIcon, ChatLabel, ActiveTab == "Chat");
		ApplyTabState(NotesPill, NotesIcon, NotesLabel, ActiveTab == "Notes");
	}

	private static void ApplyTabState(Border pill, Image icon, Label label, bool isActive)
	{
		pill.BackgroundColor = Color.FromArgb(isActive ? ActivePillColor : TransparentColor);
		icon.Opacity = isActive ? 1 : 0.72;
		icon.Scale = isActive ? 1.05 : 1;
		label.TextColor = Color.FromArgb(isActive ? ActiveColor : MutedColor);
		label.FontAttributes = isActive ? FontAttributes.Bold : FontAttributes.None;
	}

	private async void OnPatientsTapped(object sender, TappedEventArgs e)
	{
		if (ActiveTab == "Patients")
		{
			return;
		}

		await Shell.Current.GoToAsync(nameof(ProjetoINT2026.Pages.PatientsPage));
	}

	private async void OnHomeTapped(object sender, TappedEventArgs e)
	{
		if (ActiveTab == "Home")
		{
			return;
		}

		await Shell.Current.GoToAsync("//MainPage");
	}

	private async void OnChatTapped(object sender, TappedEventArgs e)
	{
		if (ActiveTab == "Chat")
		{
			return;
		}

		await Shell.Current.GoToAsync(nameof(ProjetoINT2026.Pages.ChatPage));
	}

	private async void OnNotesTapped(object sender, TappedEventArgs e)
	{
		if (ActiveTab == "Notes")
		{
			return;
		}

		await Shell.Current.GoToAsync(nameof(ProjetoINT2026.Pages.NotesPage));
	}
}
