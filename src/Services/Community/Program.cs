using CommunityService.Hubs;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add controllers with authorization support
builder.Services.AddControllers();
builder.Services.AddAuthorization();
builder.Services.AddAuthentication("Bearer");

// Register Moderation Services
builder.Services.AddScoped<Community.Services.Moderation.IContentReportingService, Community.Services.Moderation.ContentReportingService>();

// Add SignalR services
builder.Services.AddSignalR(options =>
{
    // Configuration des options SignalR
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
    options.MaximumReceiveMessageSize = 102400; // 100 KB
});

// Add CORS to allow frontend requests
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("https://modhub.ovh")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Use CORS before authorization
app.UseCors("AllowAll");

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// Map all controllers
app.MapControllers();

// Map SignalR hubs
app.MapHub<CommunityService.Hubs.ForumHub>("/hubs/forum");

// Sample endpoint for testing
app.MapGet("/api/heartbeat", () => Results.Ok(new { Status = "OK", Timestamp = DateTime.UtcNow }));

app.Run();

