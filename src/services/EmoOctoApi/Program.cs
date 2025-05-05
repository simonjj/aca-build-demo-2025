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

var builder = WebApplication.CreateBuilder(args);

// 1) Kestrel port
builder.WebHost.UseUrls("http://0.0.0.0:3004");

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
           .AddSource("EmoOctoApi")
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