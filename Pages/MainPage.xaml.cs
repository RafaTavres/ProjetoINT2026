using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using ProjetoINT2026.Services;

namespace ProjetoINT2026.Pages;

public partial class MainPage : ContentPage
{
	private readonly IHealthContentService healthContentService;
	private bool isLoading;
	private bool isRefreshing;
	private HealthPost? featuredPost;

	public MainPage()
		: this(MauiProgram.Services.GetRequiredService<IHealthContentService>())
	{
	}

	public MainPage(IHealthContentService healthContentService)
	{
		InitializeComponent();
		this.healthContentService = healthContentService;
		RefreshCommand = new Command(async () => await LoadPostsAsync(true));
		BindingContext = this;
	}

	public ObservableCollection<HealthPost> Posts { get; } = [];

	public ICommand RefreshCommand { get; }

	public HealthPost? FeaturedPost
	{
		get => featuredPost;
		set
		{
			if (featuredPost == value)
			{
				return;
			}

			featuredPost = value;
			OnPropertyChanged(nameof(FeaturedPost));
		}
	}

	public bool IsRefreshing
	{
		get => isRefreshing;
		set
		{
			if (isRefreshing == value)
			{
				return;
			}

			isRefreshing = value;
			OnPropertyChanged(nameof(IsRefreshing));
		}
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (Posts.Count == 0)
		{
			await LoadPostsAsync(false);
		}
	}

	private async Task LoadPostsAsync(bool forceRefresh)
	{
		if (isLoading)
		{
			IsRefreshing = false;
			return;
		}

		isLoading = true;
		IsRefreshing = true;
		FeedStatusLabel.Text = forceRefresh ? "Buscando novos conteúdos..." : "Carregando feed...";

		try
		{
			var posts = await healthContentService.GetPostsAsync(forceRefresh);
			Posts.Clear();
			FeaturedPost = posts.FirstOrDefault();

			foreach (var post in posts.Skip(1))
			{
				Posts.Add(post);
			}

			FeedStatusLabel.Text = posts.Count > 0 ? "Puxe para atualizar e ver outros conteúdos" : "Nenhum conteúdo encontrado";
		}
		catch
		{
			FeedStatusLabel.Text = "Não foi possível atualizar agora";
		}
		finally
		{
			isLoading = false;
			IsRefreshing = false;
		}
	}

	private async void OnPostTapped(object sender, TappedEventArgs e)
	{
		if (sender is not Border { BindingContext: HealthPost post } || string.IsNullOrWhiteSpace(post.Url))
		{
			return;
		}

		await Launcher.Default.OpenAsync(post.Url);
	}

}
