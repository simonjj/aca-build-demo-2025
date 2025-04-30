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
                SimulateChaos();

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
                            _logger.LogInformation("Dragon didn't like being petted. Rage: {Rage}, Chaos: {Chaos}",
                                dragonState.RageLevel, dragonState.Chaos);
                        }
                        else
                        {
                            dragonState.Happiness += 7;
                            dragonState.RageLevel = Math.Max(0, dragonState.RageLevel - 3);
                            _logger.LogInformation("Dragon enjoyed being petted. Happiness: {Happiness}, Rage: {Rage}",
                                dragonState.Happiness, dragonState.RageLevel);
                        }
                        break;

                    case "feed":
                        dragonState.Energy += 8;
                        dragonState.FireBreathIntensity += 5;
                        if (new Random().Next(10) > 6)
                        {
                            dragonState.Happiness += 5;
                            _logger.LogInformation("Dragon was pleased with the food. Energy: {Energy}, Fire: {Fire}",
                                dragonState.Energy, dragonState.FireBreathIntensity);
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

                        // Publish a rage event
                        await _daprClient.PublishEventAsync("pubsub", "dragon-rage", new
                        {
                            Id = DragonStateKey,
                            RageLevel = dragonState.RageLevel,
                            Timestamp = DateTime.UtcNow
                        });
                        break;

                    case "sing":
                        // Dragons may or may not be calmed by singing
                        if (new Random().Next(10) > 4)
                        {
                            dragonState.Chaos = Math.Max(0, dragonState.Chaos - 5);
                            dragonState.RageLevel = Math.Max(0, dragonState.RageLevel - 10);
                            dragonState.IsBreathingFire = false;
                            _logger.LogInformation("Dragon was calmed by singing. Chaos: {Chaos}, Rage: {Rage}",
                                dragonState.Chaos, dragonState.RageLevel);
                        }
                        else
                        {
                            dragonState.RageLevel += 5;
                            _logger.LogInformation("Dragon was annoyed by the singing. Rage: {Rage}", dragonState.RageLevel);
                        }
                        break;

                    case "message":
                        if (!string.IsNullOrEmpty(request.Message))
                        {
                            dragonState.LastMessage = request.Message;
                            // Dragons might add treasure based on messages
                            if (request.Message.Contains("treasure") || request.Message.Contains("gold") ||
                                request.Message.Contains("jewel") || request.Message.Contains("precious"))
                            {
                                dragonState.AddTreasure(request.Message);
                                _logger.LogInformation("Dragon added treasure to hoard from message. Hoard size: {Size}",
                                    dragonState.HoardSize);
                            }
                        }
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

                // Check for evolution criteria 
                if (dragonState.ShouldEvolve())
                {
                    dragonState.IsEvolved = true;
                    dragonState.HasWings = true;
                    dragonState.Evolution = "WingedDragon";

                    // Publish evolution event
                    await _daprClient.PublishEventAsync("pubsub", "dragon-evolved", new
                    {
                        Id = DragonStateKey,
                        Evolution = dragonState.Evolution,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Dragon evolved to {Evolution}!", dragonState.Evolution);
                }

                // Keep values within bounds
                dragonState.NormalizeValues();

                // Update interaction history
                dragonState.LastInteraction = DateTime.UtcNow;
                dragonState.InteractionCount++;

                // Save updated state
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

        [Topic("pubsub", "global-event")]
        [HttpPost("events")]
        public async Task<IActionResult> HandleGlobalEvent([FromBody] GlobalEvent eventData)
        {
            _logger.LogInformation("Dragon received global event: {EventType} from {Source}", eventData.EventType, eventData.Source);

            try
            {
                var dragonState = await _daprClient.GetStateAsync<DragonState>(StateStoreName, DragonStateKey) ?? new DragonState();
                dragonState.LastEvent = DateTime.UtcNow;

                // Dragons react strongly to global events
                dragonState.Chaos += 10;
                dragonState.RageLevel += 5;

                await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling global event");
                return StatusCode(500);
            }
        }

        [Topic("pubsub", "dragon-rage")]
        [HttpPost("rage-cooldown")]
        public async Task<IActionResult> HandleRageCooldown([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Processing dragon rage cooldown");

            // Wait a bit for the dragon to cool off
            await Task.Delay(TimeSpan.FromSeconds(15));

            try
            {
                var dragonState = await _daprClient.GetStateAsync<DragonState>(StateStoreName, DragonStateKey);
                if (dragonState != null && dragonState.IsBreathingFire)
                {
                    dragonState.IsBreathingFire = false;
                    dragonState.Chaos = Math.Max(40, dragonState.Chaos - 20); // Dragons always stay a bit chaotic
                    dragonState.RageLevel = Math.Max(20, dragonState.RageLevel - 30);

                    await _daprClient.SaveStateAsync(StateStoreName, DragonStateKey, dragonState);
                    _logger.LogInformation("Dragon rage cooled down. Chaos: {Chaos}, Rage: {Rage}",
                        dragonState.Chaos, dragonState.RageLevel);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling rage cooldown");
                return StatusCode(500);
            }
        }

        // Add chaos to make the demo more interesting
        private void SimulateChaos()
        {
            // 5% chance of an exception
            if (new Random().Next(20) == 0)
            {
                _logger.LogWarning("Dragon chaos causing API failure");
                throw new DragonChaosException("The dragon is too chaotic to handle this request!");
            }

            // 15% chance of delay
            if (new Random().Next(20) < 3)
            {
                Thread.Sleep(new Random().Next(500, 3000));
                _logger.LogInformation("Dragon chaos causing API slowdown");
            }
        }

        // Custom exception for dragon chaos
        private class DragonChaosException : Exception
        {
            public DragonChaosException(string message) : base(message) { }
        }
    }
}