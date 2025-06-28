using Gateway.Handlers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Ocelot.Cache.CacheManager;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Polly;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configuration de Serilog avec Elasticsearch
ConfigureLogging(builder);

// Configuration de l'authentification JWT
ConfigureJwt(builder);

// Configuration d'Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);

builder.Services.AddOcelot(builder.Configuration)
    .AddCacheManager(x => x.WithDictionaryHandle())
    .AddPolly()
    .AddDelegatingHandler<AntivirusScanHandler>();

// Enregistrement du handler d'antivirus
builder.Services.AddTransient<AntivirusScanHandler>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        // For development, allow any origin
        if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.WithOrigins(
                    builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
                    new[] { "http://localhost:5000", "https://localhost:5000", "http://localhost:5001", "https://localhost:5001", "https://modhub.ovh", "https://vps-f63d8d2b.vps.ovh.net" })
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Middleware d'erreur global
app.UseExceptionHandler("/error");

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Configure un endpoint simple pour vérifier l'état de la gateway
app.MapGet("/", () => "ModHub Gateway API is running!");
app.MapHealthChecks("/health");

// Configuration d'Ocelot (doit être le dernier middleware)
await app.UseOcelot();

app.Run();

#region Configuration Methods

void ConfigureLogging(WebApplicationBuilder builder)
{
    var elasticsearchUri = builder.Configuration["ElasticsearchSettings:Uri"] ?? "http://elasticsearch:9200";

    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "ModsGamingPlatform.Gateway")
        .WriteTo.Console()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
        {
            AutoRegisterTemplate = true,
            IndexFormat = $"modhub-gateway-{DateTime.UtcNow:yyyy-MM}"
        })
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Host.UseSerilog();
}

void ConfigureJwt(WebApplicationBuilder builder)
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? 
                                     "DefaultSecureKeyWithAtLeast32Characters!");

    // Enregistrement du handler d'authentification JWT personnalisé
    builder.Services.AddTransient<JwtAuthenticationHandler>();

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    // Configuration standard de JWT Bearer
    .AddJwtBearer("Bearer", options =>
    {
        options.RequireHttpsMetadata = false;  // Mettre à true en production
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "ModsGamingPlatform",
            ValidAudience = jwtSettings["Audience"] ?? "ModsGamingPlatformUsers",
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    })
    // Ajout de notre schéma d'authentification personnalisé
    .AddScheme<JwtAuthenticationOptions, JwtAuthenticationHandler>("JwtAuthenticationScheme", options => { });
}

#endregion
