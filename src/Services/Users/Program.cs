using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Exceptions;
using Serilog.Sinks.Elasticsearch;
using System.Text;
using UsersService.Configuration;
using UsersService.Data;
using UsersService.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuration de Serilog avec Elasticsearch
ConfigureLogging(builder);

// Configuration des services
builder.Services.AddControllers();

// Configuration de MongoDB
ConfigureMongoDb(builder);

// Configuration de JWT
ConfigureJwt(builder);

// Enregistrement des services
RegisterServices(builder);

// Configuration de Swagger/OpenAPI
ConfigureSwagger(builder);

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? 
                new[] { "http://localhost:5000", "https://modhub.ovh", "https://vps-f63d8d2b.vps.ovh.net" })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ModsGamingPlatform - UsersService v1"));
}

app.UseSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

#region Configuration Methods

void ConfigureLogging(WebApplicationBuilder builder)
{
    var elasticsearchUri = builder.Configuration["ElasticsearchSettings:Uri"] ?? "http://elasticsearch:9200";
    
    Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .Enrich.WithExceptionDetails()
        .Enrich.WithMachineName()
        .Enrich.WithProperty("Application", "ModsGamingPlatform.UsersService")
        .WriteTo.Console()
        .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri(elasticsearchUri))
        {
            AutoRegisterTemplate = true,
            IndexFormat = $"modhub-users-{DateTime.UtcNow:yyyy-MM}"
        })
        .ReadFrom.Configuration(builder.Configuration)
        .CreateLogger();

    builder.Host.UseSerilog();
}

void ConfigureMongoDb(WebApplicationBuilder builder)
{
    builder.Services.Configure<MongoDbSettings>(
        builder.Configuration.GetSection("MongoDbSettings"));

    builder.Services.AddSingleton<MongoDbSettings>(serviceProvider =>
    {
        var settings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
        if (settings == null)
        {
            settings = new MongoDbSettings
            {
                ConnectionString = "mongodb://mongodb:27017",
                DatabaseName = "ModsGamingPlatform",
                UsersCollectionName = "Users",
                RefreshTokensCollectionName = "RefreshTokens",
                UserActivitiesCollectionName = "UserActivities"
            };
        }
        return settings;
    });
}

void ConfigureJwt(WebApplicationBuilder builder)
{
    var jwtSettings = builder.Configuration.GetSection("JwtSettings");
    var key = Encoding.ASCII.GetBytes(jwtSettings["Key"] ?? 
                                      "DefaultSecureKeyWithAtLeast32Characters!");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
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
    });
}

void RegisterServices(WebApplicationBuilder builder)
{
    // Repositories
    builder.Services.AddSingleton<IUserRepository, UserRepository>();

    // Services
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IUserService, UserService>();
    
    // Email Service
    builder.Services.Configure<EmailSettings>(
        builder.Configuration.GetSection("EmailSettings"));
    builder.Services.AddScoped<IEmailService, EmailService>();
}

void ConfigureSwagger(WebApplicationBuilder builder)
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ModsGamingPlatform - UsersService API",
            Version = "v1",
            Description = "API pour la gestion des utilisateurs et l'authentification",
            Contact = new OpenApiContact
            {
                Name = "Équipe ModsGamingPlatform",
                Email = "contact@modsgamingplatform.com",
                Url = new Uri("https://modsgamingplatform.com/contact")
            },
        });

        // Configuration pour JWT dans Swagger
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });
}

#endregion
