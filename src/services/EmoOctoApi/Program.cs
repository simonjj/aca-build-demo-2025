using EmoOctoApi.Models;
using EmoOctoApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Logs;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// 1) Kestrel port
builder.WebHost.UseUrls("http://0.0.0.0:3004");
var meter = new Meter("EmoOctoApi", "1.0.0");

var requestCounter = meter.CreateCounter<long>(
    name: "api_requests_total",
    description: "Counts all incoming HTTP requests per endpoint"
);

var responseCounter = meter.CreateCounter<long>(
    name: "api_responses_total",
    description: "Counts all HTTP responses per endpoint and status code"
);

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendAllowed", policy =>
    {
        policy
          .AllowAnyOrigin()
          .AllowAnyHeader()
          .AllowAnyMethod();
    });
});

// before you call .Build()
builder.Logging
    // Default log level for all categories
    .SetMinimumLevel(LogLevel.Warning)
    // Essential application logs
    .AddFilter("EmoOctoApi", LogLevel.Information)
    // Filter out noisy framework logs
    .AddFilter("Microsoft.AspNetCore.Hosting.Diagnostics", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.Server.Kestrel", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.Server.Kestrel.Core", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.Hosting.Internal.WebHost", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.Mvc", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.Routing", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.HttpLogging", LogLevel.Warning)
    // Custom filters for important logs you do want to keep
    .AddFilter("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware.RequestBody", LogLevel.Warning)
    .AddFilter("Microsoft.AspNetCore.HttpLogging.HttpLoggingMiddleware.ResponseBody", LogLevel.Warning)
    .AddFilter("Microsoft.Hosting.Lifetime", LogLevel.Warning)
    // Keep Dapr related logs
    .AddFilter("Dapr", LogLevel.Information);


// 2) Shared Resource
var resourceBuilder = ResourceBuilder.CreateDefault()
    .AddService("EmoOctoApi", "1.0.0")
    .AddAttributes(new Dictionary<string, object>
    {
        ["deployment.environment"] = builder.Environment.EnvironmentName,
        ["host.name"] = Environment.MachineName
    });

builder.Services.AddOpenTelemetry()
   .ConfigureResource(resource => resource.AddService("EmoOctoApi"))
   .WithTracing(tracing =>
   {
       tracing
           .AddSource("EmoOctoApi.PetController")
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddConsoleExporter()
           .AddOtlpExporter();
   })
   .WithMetrics(metrics =>
   {
       metrics
           .AddMeter("EmoOctoApi")
           .AddAspNetCoreInstrumentation()
           .AddHttpClientInstrumentation()
           .AddConsoleExporter()
           .AddOtlpExporter();
   })
    .WithLogging(logging =>
   {
       logging
           .AddConsoleExporter()
           .AddOtlpExporter();
   });
// 6) Dapr & DI
builder.Services.AddControllers().AddDapr();
builder.Services.AddDaprClient();
builder.Services.AddSingleton<OctoState>();
builder.Services.AddSingleton<OctoThoughtsService>();
builder.Services.AddHttpClient();

var app = builder.Build();

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();
app.MapGet("/healthz", () => Results.Ok("Healthy"));

app.Run();