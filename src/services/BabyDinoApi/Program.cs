using BabyDinoApi.Models;
using BabyDinoApi.Services;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on specified port (3003 for BabyDino)
builder.WebHost.UseUrls("http://0.0.0.0:3003");

// Add services to the container
// Add services to the container
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();

// Register Dino State and Services
builder.Services.AddSingleton<DinoState>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<DinoThoughtsService>();

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