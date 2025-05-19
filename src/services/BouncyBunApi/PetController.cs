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
    private BunThoughtsService _thoughtsService;

    public PetController(DaprClient daprClient, BunThoughtsService thoughtsService)
    {
        _thoughtsService = thoughtsService;
        _daprClient = daprClient;
    }

    [HttpGet("state")]
    public async Task<IActionResult> GetStateAsync()
    {
        var bunState = await _daprClient.GetStateAsync<BunState>(StateStoreName, BunStateKey);
        if (bunState == null)
        {
            bunState = new BunState();
            await _daprClient.SaveStateAsync(StateStoreName, BunStateKey, bunState);
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
            default:
                return BadRequest("Unknown action.");
        }
        UpdateMood(bunState);
        bunState.LastMessage = await _thoughtsService.GenerateThoughtAsync(bunState);
        await _daprClient.SaveStateAsync(StateStoreName, BunStateKey, bunState);
        return Ok(bunState);
    }

    public void UpdateMood(BunState bunState)
    {
        if (bunState.Happiness > bunState.Energy)
        {
            bunState.Mood = "Happy";
        }
        else if (bunState.Energy < bunState.Happiness)
        {
            bunState.Mood = "Energetic";
        }
        else if (bunState.Energy < bunState.Chaos)
        {
            bunState.Mood = "Sleepy";
        }

        else if (bunState.Chaos > bunState.Energy)
        {
            bunState.Mood = "Tired";
        }
        else if (bunState.Chaos > bunState.Happiness)
        {
            bunState.Mood = "Chaotic";
        }
        else
        {
            bunState.Mood = "Neutral";
        }
    }
}

public class InteractionRequest
{
    public string Action { get; set; } = string.Empty;
}