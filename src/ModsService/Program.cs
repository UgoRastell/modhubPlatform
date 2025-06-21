using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using ModsService.Models;
using ModsService.Repositories;
using ModsService.Services;
using ModsService.Settings;
using MongoDB.Driver;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MongoDB
var mongoSettings = builder.Configuration.GetSection("MongoDbSettings").Get<MongoDbSettings>();
builder.Services.AddSingleton<IMongoClient>(sp => new MongoClient(mongoSettings.ConnectionString));
builder.Services.AddScoped(sp => {
    var client = sp.GetRequiredService<IMongoClient>();
    return client.GetDatabase(mongoSettings.DatabaseName);
});

// Configure repositories
builder.Services.AddScoped<IModRepository, ModRepository>();
builder.Services.AddScoped<IVersionRepository, VersionRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<IStatsRepository, StatsRepository>();

// Configure services
builder.Services.AddScoped<IModService, ModService>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddScoped<IVersioningService, VersioningService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();

// Add RabbitMQ
builder.Services.AddSingleton<IRabbitMQService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var options = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();
    return new RabbitMQService(options);
});

// Add background services
builder.Services.AddHostedService<ModEventConsumer>();

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Configure Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "ModsCache:";
});

// Configure gRPC clients
builder.Services.AddGrpcClient<FileService.FileServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcServices:FileService"]);
});

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(builder.Configuration["AllowedOrigins"])
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// Add health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoSettings.ConnectionString, name: "mongodb", tags: new[] { "db" })
    .AddRedis(builder.Configuration.GetConnectionString("Redis"), name: "redis", tags: new[] { "cache" });

// Configure Prometheus metrics
builder.Services.AddMetrics();

// Ajouter la configuration
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
