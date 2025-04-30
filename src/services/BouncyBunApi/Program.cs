var builder = WebApplication.CreateBuilder(args);

// Register your services
builder.Services.AddSingleton<PetState>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BunThoughtsService>();

var app = builder.Build();

// Health check endpoint
app.MapGet("/healthz", () => Results.Ok("Healthy"));

// Interaction endpoint
app.MapPost("/interact", async (Interaction input, PetState state, BunThoughtsService thoughts) =>
{
    state.Apply(input.Action);
    if (state.ShouldEvolve())
        await thoughts.TriggerEvolution();

    return Results.Ok(state);
});

// State endpoint
app.MapGet("/state", (PetState state) => Results.Ok(state));

app.Run();

record Interaction(string Action);