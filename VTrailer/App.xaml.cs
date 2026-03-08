using System.Diagnostics.CodeAnalysis;
using Uno.Resizetizer;

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
    protected IHost? Host { get; private set; }

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

                    // Uno Platform namespace filter groups
                    // Uncomment individual methods to see more detailed logging
                    //// Generic Xaml events
                    //logBuilder.XamlLogLevel(LogLevel.Debug);
                    //// Layout specific messages
                    //logBuilder.XamlLayoutLogLevel(LogLevel.Debug);
                    //// Storage messages
                    //logBuilder.StorageLogLevel(LogLevel.Debug);
                    //// Binding related messages
                    //logBuilder.XamlBindingLogLevel(LogLevel.Debug);
                    //// Binder memory references tracking
                    //logBuilder.BinderMemoryReferenceLogLevel(LogLevel.Debug);
                    //// DevServer and HotReload related
                    //logBuilder.HotReloadCoreLogLevel(LogLevel.Information);
                    //// Debug JS interop
                    //logBuilder.WebAssemblyLogLevel(LogLevel.Debug);

                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
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
                                    // Lekérdezzük az adatbázisból a felhasználót
                                    var dbService = sp.GetRequiredService<VTrailer.Services.DatabaseService>();
                                    var user = await dbService.GetUserAsync(username, password);

                                    if (user != null)
                                    {
                                       
                                        credentials ??= new Dictionary<string, string>();

                                        credentials["Role"] = user.Role ?? "";
                                        credentials[TokenCacheExtensions.AccessTokenKey] = "RealToken123";
                                        credentials["Expiry"] = DateTime.Now.AddHours(1).ToString("g");
                                        return credentials;
                                    }
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
                    // TODO: Register your services
                    services.AddSingleton<VTrailer.Services.DatabaseService>();
                    
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
                await dbService.InitializeDatabaseAsync();

                
                var auth = services.GetRequiredService<IAuthenticationService>();
                var authenticated = await auth.RefreshAsync();
                if (authenticated)
                {
                    await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
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
            new ViewMap<MainPage, MainViewModel>(),
            new DataViewMap<SecondPage, SecondViewModel, Entity>(),
            new ViewMap<ProfilePage, ProfileViewModel>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                    new ("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault:true),
                    new ("Second", View: views.FindByViewModel<SecondViewModel>()),
                    new ("Profile", View: views.FindByViewModel<ProfileViewModel>()),
                ]
            )
        );
    }
}
