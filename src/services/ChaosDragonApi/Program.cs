using System.Text.Json;
using ChaosDragonApi.Models;
using ChaosDragonApi.Services;
using OpenTelemetry.Resources;
// Add these using statements
using Azure.Monitor.OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 3002
builder.WebHost.UseUrls("http://0.0.0.0:3002");

// Add services to the container
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();
// Register Dragon State and Services
builder.Services.AddSingleton<DragonState>();
builder.Services.AddSingleton<DragonThoughtsService>();
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