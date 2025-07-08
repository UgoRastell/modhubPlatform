using ModsService.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModsService.Repositories;
using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Configure MongoDB
var mongoDbSettings = builder.Configuration.GetSection("MongoDB").Get<MongoDbSettings>();
if (mongoDbSettings == null)
{
    throw new InvalidOperationException("La configuration MongoDB est manquante dans appsettings.json");
}
builder.Services.AddSingleton(mongoDbSettings);

// Register repositories
builder.Services.AddSingleton<IModRepository, ModRepository>();

// Configure API Explorer
builder.Services.AddEndpointsApiExplorer();

// Configuration JWT pour l'authentification
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? 
    "Super_Secret_Key_With_At_Least_32_Characters_For_Production_Use_Environment_Variable";
var issuer = builder.Configuration["JwtSettings:Issuer"] ?? "ModsGamingPlatform";
var audience = builder.Configuration["JwtSettings:Audience"] ?? "ModsGamingPlatformUsers";

builder.Services.AddAuthentication(options => 
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => 
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Configure authorization pour permettre [AllowAnonymous]
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Désactive la politique d'autorisation par défaut
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Journal de débogage activé uniquement en développement
    app.UseDeveloperExceptionPage();
}

app.UseCors("CorsPolicy");

// Configuration des fichiers statiques
app.UseStaticFiles(); // Pour servir les fichiers dans wwwroot par défaut

// Configuration pour servir les fichiers uploadés depuis le dossier uploads
string uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
if (!Directory.Exists(uploadsDir))
{
    Directory.CreateDirectory(uploadsDir);
    Console.WriteLine($"Dossier uploads créé: {uploadsDir}");
}
else
{
    Console.WriteLine($"Dossier uploads existant: {uploadsDir}");
    // Vérifier et afficher le contenu pour debug
    try 
    {
        var files = Directory.GetFiles(uploadsDir, "*.*", SearchOption.AllDirectories);
        Console.WriteLine($"Nombre de fichiers trouvés dans uploads: {files.Length}");
        foreach (var file in files.Take(10)) // Limite à 10 fichiers pour éviter de surcharger les logs
        {
            Console.WriteLine($"Fichier: {file}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Erreur lors de la lecture du dossier uploads: {ex.Message}");
    }
}

// Configuration améliorée des fichiers statiques pour les uploads
var provider = new FileExtensionContentTypeProvider();
// Ajouter des types MIME explicites pour sécurité
provider.Mappings[".zip"] = "application/zip";
provider.Mappings[".7z"] = "application/x-7z-compressed";
provider.Mappings[".rar"] = "application/vnd.rar";
provider.Mappings[".jpg"] = "image/jpeg";
provider.Mappings[".jpeg"] = "image/jpeg";
provider.Mappings[".png"] = "image/png";
provider.Mappings[".gif"] = "image/gif";
provider.Mappings[".webp"] = "image/webp";

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsDir),
    RequestPath = "/uploads",
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = false, // Sécurité : seulement les types connus
    DefaultContentType = "application/octet-stream", // Fallback pour types inconnus si ServeUnknownFileTypes = true
    OnPrepareResponse = ctx =>
    {
        // Diagnostic détaillé
        var requestPath = ctx.Context.Request.Path.Value;
        var physicalPath = ctx.File?.PhysicalPath ?? "N/A";
        Console.WriteLine($"=== STATIC FILE REQUEST ===");
        Console.WriteLine($"Request Path: {requestPath}");
        Console.WriteLine($"Physical Path: {physicalPath}");
        Console.WriteLine($"File Exists: {System.IO.File.Exists(physicalPath)}");
        Console.WriteLine($"Content Type: {ctx.Context.Response.ContentType}");
        
        // Headers CORS et cache
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
        ctx.Context.Response.Headers.Append("Access-Control-Allow-Methods", "GET");
        ctx.Context.Response.Headers.Append("Cache-Control", "public, max-age=3600");
        
        // Headers de sécurité
        ctx.Context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        
        Console.WriteLine($"=== END STATIC FILE REQUEST ===");
    }
});

// Ordre important pour le middleware d'authentification/autorisation
app.UseAuthentication();
app.UseAuthorization();

// Active les contrôleurs et leurs routes
app.MapControllers();

// Garde la route WeatherForecast pour la compatibilité
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
