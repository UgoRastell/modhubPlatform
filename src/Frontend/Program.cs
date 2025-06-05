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

// Configuration de l'URL de base de l'API Gateway
var apiGatewayUrl = builder.Configuration["ApiSettings:GatewayUrl"] ?? "https://localhost:5001";

// Configuration des services HTTP
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiGatewayUrl) });
builder.Services.AddHttpClient("UsersService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/users-service"));
builder.Services.AddHttpClient("ModsService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/mods-service"));
builder.Services.AddHttpClient("PaymentsService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/payments-service"));
builder.Services.AddHttpClient("CommunityService", client => client.BaseAddress = new Uri($"{apiGatewayUrl}/community-service"));

// Services d'authentification
builder.Services.AddOptions();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthStateProvider>();

// Services d'API
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IModService, ModService>();

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

// Construction et exécution de l'application
await builder.Build().RunAsync();
