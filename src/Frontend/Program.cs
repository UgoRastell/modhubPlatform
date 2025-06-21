using Frontend;
using Frontend.Services;
using Frontend.Theme;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration from appsettings.json
var configFilePath = "appsettings.json";
var appSettingsJson = Path.Combine(builder.HostEnvironment.BaseAddress, configFilePath);

using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri(builder.HostEnvironment.BaseAddress);

var configJson = await httpClient.GetStringAsync(configFilePath);
using var configStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configJson));
builder.Configuration.AddJsonStream(configStream);

// Configuration de l'URL de base de l'API Gateway
var apiGatewayUrl = builder.Configuration["ApiSettings:GatewayUrl"] ?? "https://localhost:8080";

// Configuration des services HTTP
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiGatewayUrl) });
builder.Services.AddHttpClient("UsersService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/users-service"));
builder.Services.AddHttpClient("ModsService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/mods-service"));
builder.Services.AddHttpClient("PaymentsService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/payments-service"));
builder.Services.AddHttpClient("CommunityService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/community-service"));

// Services d'authentification
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<ILocalStorageService, LocalStorageService>();
builder.Services.AddScoped<AuthenticationStateProvider, JwtAuthenticationStateProvider>();

// Services d'API
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IModService, ModService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IUserSettingsService, UserSettingsService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// Configuration des services MudBlazor
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = true;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 5000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

// Ajout du thème ModHub personnalisé
builder.Services.AddSingleton<MudTheme>(ModHubTheme.DefaultTheme);

// Ajout du service de thème pour gérer le mode sombre/clair
builder.Services.AddSingleton<IThemeService, ThemeService>();

// Construction et exécution de l'application
await builder.Build().RunAsync();
