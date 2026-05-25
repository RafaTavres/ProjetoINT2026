namespace ProjetoINT2026.Controls;

public partial class BottomNavigationBar : ContentView
{
	private const string ActiveColor = "#B2BFA2";
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
		var active = Color.FromArgb(ActiveColor);
		var muted = Color.FromArgb(MutedColor);

		PatientsLabel.TextColor = ActiveTab == "Patients" ? active : muted;
		HomeLabel.TextColor = ActiveTab == "Home" ? active : muted;
		ChatLabel.TextColor = ActiveTab == "Chat" ? active : muted;
		NotesLabel.TextColor = ActiveTab == "Notes" ? active : muted;
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
