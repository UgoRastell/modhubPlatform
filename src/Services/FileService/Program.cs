using FileService.Models;
using FileService.Services;
using FileService.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddScoped<IFileRepository, FileRepository>();
builder.Services.AddScoped<IFileMetadataRepository, FileMetadataRepository>();
builder.Services.AddScoped<IScanResultRepository, ScanResultRepository>();

// Configure services
builder.Services.AddScoped<IStorageService, AzureBlobStorageService>();
builder.Services.AddScoped<IFileService, Services.FileService>();
builder.Services.AddScoped<IFileValidationService, FileValidationService>();
builder.Services.AddScoped<IVirusScanner, ClamAvVirusScanner>();
builder.Services.AddSingleton<IFileProcessingQueue, FileProcessingQueue>();

// Add background services
builder.Services.AddHostedService<FileProcessingService>();

// Add RabbitMQ
builder.Services.AddSingleton<IRabbitMQService>(sp =>
{
    var configuration = sp.GetRequiredService<IConfiguration>();
    var options = configuration.GetSection("RabbitMQ").Get<RabbitMQSettings>();
    return new RabbitMQService(options);
});

// Add gRPC Server
builder.Services.AddGrpc();

// Configure JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
builder.Services.AddSingleton(jwtSettings);

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

// Configure Azure Storage
var storageSettings = builder.Configuration.GetSection("AzureStorageSettings").Get<AzureStorageSettings>();
builder.Services.AddSingleton(storageSettings);

// Configure file settings
var fileSettings = builder.Configuration.GetSection("FileSettings").Get<FileSettings>();
builder.Services.AddSingleton(fileSettings);

// Add health checks
builder.Services.AddHealthChecks()
    .AddMongoDb(mongoSettings.ConnectionString, name: "mongodb", tags: new[] { "db" })
    .AddAzureBlobStorage(storageSettings.ConnectionString, name: "azure-storage" );

// Configure rate limiting
builder.Services.AddRateLimiting(builder.Configuration);

// Add metrics
builder.Services.AddMetrics();

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
app.UseRateLimiting();

// Map endpoints
app.MapControllers();
app.MapGrpcService<FileGrpcService>();
app.MapHealthChecks("/health");
app.MapMetrics("/metrics");

app.Run();
