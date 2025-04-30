using System.Text.Json;
using Microsoft.AspNetCore.RateLimiting;
using ChillTurtleApi.Models;
using ChillTurtleApi.Services;

var builder = WebApplication.CreateBuilder(args);

// 0) Bind Kestrel to 0.0.0.0:3001 so Dapr sidecar can detect the app on port 3001
builder.WebHost.UseUrls("http://0.0.0.0:3001");

// 2) Add controllers + Dapr SDK for Pub/Sub
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();

// 3) Your application services
builder.Services.AddSingleton<TurtleState>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<TurtleThoughtsService>();


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