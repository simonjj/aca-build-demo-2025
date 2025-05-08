var builder = WebApplication.CreateBuilder(args);

// 0) Bind Kestrel to port 3000 so Dapr can detect your app
builder.WebHost.UseUrls("http://0.0.0.0:3000");

// 1) Add Dapr SDK bits
builder.Services.AddControllers().AddDapr();       // for controller-based Pub/Sub 
builder.Services.AddDaprClient();                  // for injecting DaprClient
builder.Services.AddSingleton<PetState>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BunThoughtsService>();

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