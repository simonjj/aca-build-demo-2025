using EmoOctoApi.Models;
using EmoOctoApi.Services;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 3004
builder.WebHost.UseUrls("http://0.0.0.0:3004");

// Add services to the container
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();

// Register Octopus State and Services
builder.Services.AddSingleton<OctoState>();
builder.Services.AddSingleton<OctoThoughtsService>();

// Add resilient HTTP client
builder.Services.AddHttpClient();

var app = builder.Build();

// 2) Enable CloudEvents middleware so Dapr pub/sub works
app.UseCloudEvents();

// 3) Map the Dapr subscription handler (scans for [Topic])
app.MapSubscribeHandler();

// 4) Map your controllers
app.MapControllers();

// 5) Healthz (still a minimal API endpoint)
app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.Run();