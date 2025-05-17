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
        await _thoughtsService.GenerateThoughtAsync(bunState);
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
        await _daprClient.SaveStateAsync(StateStoreName, BunStateKey, bunState);
        return Ok(bunState);
    }

    public void UpdateMood(BunState bunState)
    {
        if (bunState.Happiness > 10)
        {
            bunState.Mood = "happy";
        }
        else if (bunState.Energy < 5)
        {
            bunState.Mood = "tired";
        }
        else if (bunState.Chaos > 10)
        {
            bunState.Mood = "chaotic";
        }
        else
        {
            bunState.Mood = "neutral";
        }
    }
}

public class InteractionRequest
{
    public string Action { get; set; } = string.Empty;
}