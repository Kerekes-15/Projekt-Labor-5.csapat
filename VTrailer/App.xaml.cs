using System.Diagnostics.CodeAnalysis;
using Uno.Resizetizer;
using VTrailer.Presentation;

namespace VTrailer;
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object. This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }
    public IHost? Host { get; private set; }

    [SuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Uno.Extensions APIs are used in a way that is safe for trimming in this template context.")]
    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            // Add navigation support for toolkit controls such as TabBar and NavigationView
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                // Switch to Development environment when running in DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    // Configure log levels for different categories of logging
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)

                        // Default filters for core Uno Platform namespaces
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                        .Section<DeliveryOptions>()
                )
                // Enable localization (see appsettings.json for supported languages)
                .UseLocalization()
                .UseHttp((context, services) =>
                {
#if DEBUG
                    // DelegatingHandler will be automatically injected
                    services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
                })
               .UseAuthentication(auth =>
                    auth.AddCustom(custom =>
                        custom
                            .Login(async (sp, dispatcher, credentials, cancellationToken) =>
                            {
                                // Kinyerjük a beírt adatokat
                                if (credentials?.TryGetValue("Username", out var username) == true &&
                                    credentials?.TryGetValue("Password", out var password) == true)
                                {
                                    // Lekérdezzük a szervizt
                                    var dbService = sp.GetRequiredService<VTrailer.Services.DatabaseService>();

                                    // --- ITT A JAVÍTÁS ---
                                    // Az új boolean (true/false) visszaadó Supabase bejelentkezést hívjuk
                                    bool success = await dbService.LoginUserAsync(username, password);

                                    if (success)
                                    {
                                        credentials ??= new Dictionary<string, string>();

                                        // Beállítjuk a jogosultságot. Ha nincs még Role a db-ben, "User" lesz az alap.
                                        credentials["Role"] = VTrailer.Services.DatabaseService.CurrentUser?.Role ?? "User";
                                        credentials[TokenCacheExtensions.AccessTokenKey] = "RealToken123";
                                        credentials["Expiry"] = DateTime.Now.AddHours(1).ToString("g");

                                        return credentials;
                                    }
                                    // -----------------------
                                }

                                return default;
                            })
                            .Refresh((sp, tokenDictionary, cancellationToken) =>
                            {
                                return ValueTask.FromResult<IDictionary<string, string>?>(default);
                            }), name: "CustomAuth")
                )
                .ConfigureServices((context, services) =>
                {
                    // Register your services
                    services.AddSingleton<VTrailer.Services.DatabaseService>();
                    services.AddHttpClient<VTrailer.Services.DeliveryQuoteService>();
                    services.AddTransient<BookingViewModel>();
                    
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>
            (initialNavigate: async (services, navigator) =>
            {
                var dbService = services.GetRequiredService<VTrailer.Services.DatabaseService>();

                var auth = services.GetRequiredService<IAuthenticationService>();
                var authenticated = await auth.RefreshAsync();

                if (authenticated)
                {
                    await navigator.NavigateViewModelAsync<HomePageViewModel>(this, qualifier: Qualifiers.Nested);
                }
                else
                {
                    await navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.Nested);
                }
            });
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<LoginPage, LoginViewModel>(),
            new ViewMap<HomePage, HomePageViewModel>(),
            new ViewMap<TrailerPage, TrailerViewModel>(),
            new DataViewMap<SecondPage, SecondViewModel, Entity>(),
            new ViewMap<ProfilePage, ProfileViewModel>(),
            new ViewMap<RegisterPage, RegisterViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                    new ("Home Page", View: views.FindByViewModel<HomePageViewModel>(), IsDefault:true),
                    new ("TrailerPage", View: views.FindByViewModel<TrailerViewModel>()),
                    new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new ("Profile", View: views.FindByViewModel<ProfileViewModel>()),
                ]
            )
        );
    }
}
