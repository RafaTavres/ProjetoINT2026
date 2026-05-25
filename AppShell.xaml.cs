namespace ProjetoINT2026;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		Routing.RegisterRoute(nameof(Pages.PatientsPage), typeof(Pages.PatientsPage));
		Routing.RegisterRoute(nameof(Pages.ChatPage), typeof(Pages.ChatPage));
		Routing.RegisterRoute(nameof(Pages.NotesPage), typeof(Pages.NotesPage));
	}
}
