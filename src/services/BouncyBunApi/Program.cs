var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<PetState>();
builder.Services.AddHttpClient();
builder.Services.AddSingleton<BunThoughtsService>();
var app = builder.Build();

app.MapPost("/interact", async (Interaction input, PetState state, BunThoughtsService thoughts) =>
{
    state.Apply(input.Action);
    if (state.ShouldEvolve())
        await thoughts.TriggerEvolution();

    return Results.Ok(state);
});

app.MapGet("/state", (PetState state) => Results.Ok(state));

app.Run();

record Interaction(string Action);