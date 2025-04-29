using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;

namespace BouncyBunApi.Controllers;

[ApiController]
[Route("[controller]")]
public class PetController : ControllerBase
{
    private readonly DaprClient _daprClient;
    private const string StateStoreName = "statestore";
    private const string BunStateKey = "bouncybun";

    public PetController(DaprClient daprClient)
    {
        _daprClient = daprClient;
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetStateAsync()
    {
        var bunState = await _daprClient.GetStateAsync<BunState>(StateStoreName, BunStateKey);
        if (bunState == null)
        {
            bunState = new BunState();
        }
        return Ok(bunState);
    }

    [HttpPost("interact")]
    public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
    {
        var bunState = await _daprClient.GetStateAsync<BunState>(StateStoreName, BunStateKey) ?? new BunState();

        switch (request.Action.ToLower())
        {
            case "pet":
                bunState.Happiness += 5;
                break;
            case "feed":
                bunState.Energy += 5;
                break;
            case "poke":
                bunState.Chaos += 5;
                break;
            case "sing":
                bunState.Calmness += 5;
                break;
            case "message":
                bunState.LastMessage = request.Message ?? "no message";
                break;
            default:
                return BadRequest("Unknown action.");
        }

        // Check for evolution
        if (bunState.Happiness > 50 && bunState.Energy > 50 && bunState.Chaos > 20)
        {
            await _daprClient.PublishEventAsync("pubsub", "evolution", new { creature = "bunny", stage = "MegaBun" });
        }

        await _daprClient.SaveStateAsync(StateStoreName, BunStateKey, bunState);
        return Ok(bunState);
    }

    [Topic("pubsub", "reset-bunny")]
    [HttpPost("reset")]
    public async Task<IActionResult> ResetAsync()
    {
        var initialState = new BunState();
        await _daprClient.SaveStateAsync(StateStoreName, BunStateKey, initialState);
        return Ok(initialState);
    }
}

public class InteractionRequest
{
    public string Action { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public class BunState
{
    public int Happiness { get; set; } = 0;
    public int Energy { get; set; } = 0;
    public int Chaos { get; set; } = 0;
    public int Calmness { get; set; } = 0;
    public string LastMessage { get; set; } = "";
}