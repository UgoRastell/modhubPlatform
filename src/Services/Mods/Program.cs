using ModsService.Models;
using ModsService.Repositories;

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

// Configure authorization to make [AllowAnonymous] work correctly
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = null; // Désactive la politique d'autorisation par défaut
});
builder.Services.AddAuthentication();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Journal de débogage activé uniquement en développement
    app.UseDeveloperExceptionPage();
}

app.UseCors("CorsPolicy");

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
