using System.Diagnostics;
using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using ChaosDragonApi.Models;
using ChaosDragonApi.Services;

namespace ChaosDragonApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PetController> _logger;
        private readonly DragonThoughtsService _thoughtsService;
        private const string StateStoreName = "statestore";
        private const string DragonStateKey = "chaosdragon";
        private static readonly ActivitySource _activitySource = new("ChaosDragonApi");

        public PetController(
            DaprClient daprClient,
            ILogger<PetController> logger,
            DragonThoughtsService thoughtsService)
        {
            _daprClient = daprClient;
            _logger = logger;
            _thoughtsService = thoughtsService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetStateAsync()
        {
            using var activity = _activitySource.StartActivity("GetDragonState");

            try
            {
                var dragonState = await _daprClient.GetStateAsync<DragonState>(StateStoreName, DragonStateKey);
                if (dragonState == null)
                {
                    dragonState = new DragonState();
                    await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);
                    _logger.LogInformation("Created new dragon state");
                }

                // Generate a thought for UI display
                dragonState.LastMessage = await _thoughtsService.GenerateThoughtAsync(dragonState);

                activity?.SetTag("dragon.chaos", dragonState.Chaos);
                activity?.SetTag("dragon.evolved", dragonState.IsEvolved);

                return Ok(dragonState);
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Error retrieving dragon state from Dapr state store");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "State service unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving dragon state");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            using var activity = _activitySource.StartActivity("DragonInteraction");
            activity?.SetTag("interaction.action", request.Action);

            if (string.IsNullOrEmpty(request.Action))
            {
                _logger.LogWarning("Interaction request received with empty action");
                return BadRequest(new { error = "Action is required" });
            }

            try
            {
                var dragonState = await _daprClient.GetStateAsync<DragonState>(StateStoreName, DragonStateKey) ?? new DragonState();

                // Dragons are chaotic - occasionally throw an exception to simulate crashes
                // Process the interaction
                switch (request.Action.ToLower())
                {
                    case "pet":
                        // Dragons are unpredictable - sometimes they like being petted, sometimes they get angry
                        if (new Random().Next(10) > 7)
                        {
                            dragonState.Happiness -= 3;
                            dragonState.RageLevel += 10;
                            dragonState.Chaos += 5;
                        }
                        else
                        {
                            dragonState.Happiness += 7;
                            dragonState.RageLevel = Math.Max(0, dragonState.RageLevel - 3);
                        }
                        break;

                    case "feed":
                        dragonState.Energy += 8;
                        dragonState.FireBreathIntensity += 5;
                        if (new Random().Next(10) > 6)
                        {
                            dragonState.Happiness += 5;
                        }
                        break;

                    case "poke":
                        // Dragons do NOT like being poked
                        dragonState.Chaos += 15;
                        dragonState.RageLevel += 20;
                        dragonState.Happiness -= 10;
                        dragonState.IsBreathingFire = true;

                        _logger.LogWarning("Dragon poked! Chaos: {Chaos}, Rage: {Rage}",
                            dragonState.Chaos, dragonState.RageLevel);


                        await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);
                        break;
                    default:
                        _logger.LogWarning("Unknown dragon action requested: {Action}", request.Action);
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                // Check for rage conditions
                if (dragonState.RageLevel > 80 && new Random().Next(10) > 6)
                {
                    _logger.LogWarning("Dragon rage triggered chaos spike!");
                    dragonState.Chaos += 20;
                    dragonState.IsBreathingFire = true;
                    dragonState.HighChaosEvents++;
                }

                // Save updated state
                UpdateMood(dragonState);
                await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);

                // Generate thought
                dragonState.LastMessage = await _thoughtsService.GenerateThoughtAsync(dragonState);

                activity?.SetTag("dragon.chaos_after", dragonState.Chaos);
                activity?.SetTag("dragon.happiness", dragonState.Happiness);
                activity?.SetTag("dragon.evolved", dragonState.IsEvolved);

                return Ok(dragonState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing dragon interaction");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);

                // Even in error, make the dragon more chaotic
                try
                {
                    var dragonState = await _daprClient.GetStateAsync<DragonState>(StateStoreName, DragonStateKey);
                    if (dragonState != null)
                    {
                        dragonState.Chaos = Math.Min(100, dragonState.Chaos + 5);
                        dragonState.ApiFailureCount++;
                        await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);
                    }
                }
                catch { /* Ignore failures in error handling */ }

                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Dragon chaos prevented this action" });
            }
        }

        public void UpdateMood(DragonState state)
        {
            // Azure best practice: Use deterministic rules with clear priority ordering
            if (state.IsBreathingFire && state.Chaos > 80)
            {
                state.Mood = "Enraged";
            }
            else if (state.RageLevel > 75)
            {
                state.Mood = "Furious";
            }
            else if (state.Energy < 20)
            {
                state.Mood = "Lethargic";
            }
            else if (state.Happiness > 80 && state.Energy > 70)
            {
                state.Mood = "Playful";
            }
            else if (state.Happiness > 60)
            {
                state.Mood = "Content";
            }
            else if (state.Energy > 80)
            {
                state.Mood = "Restless";
            }
            else if (state.Chaos > 50)
            {
                state.Mood = "Unpredictable";
            }
            else
            {
                state.Mood = "Calm";
            }

        }

        // Custom exception for dragon chaos
        private class DragonChaosException : Exception
        {
            public DragonChaosException(string message) : base(message) { }
        }
    }
}