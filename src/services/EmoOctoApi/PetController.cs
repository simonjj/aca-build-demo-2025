using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using EmoOctoApi.Models;
using EmoOctoApi.Services;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace EmoOctoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PetController> _logger;
        private readonly OctoThoughtsService _thoughtsService;
        private const string StateStoreName = "statestore";
        private const string OctoStateKey = "emoocto";
        private static readonly Meter _meter = new Meter("EmoOctoApi", "1.0.0");
        private static readonly Counter<long> _interactionsCounter =
            _meter.CreateCounter<long>(
                "octo_interactions_total",
                description: "Total number of octopus interact calls");

        public PetController(
            DaprClient daprClient,
            ILogger<PetController> logger,
            OctoThoughtsService thoughtsService)
        {
            _daprClient = daprClient;
            _logger = logger;
            _thoughtsService = thoughtsService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetStateAsync()
        {
            using var activity = new Activity("GetOctoState").Start();

            try
            {
                var octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey);
                if (octoState == null)
                {
                    octoState = new OctoState();
                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                    _logger.LogInformation("Created new emotional octopus state");
                }

                // Generate a thought for UI display
                octoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(octoState);

                return Ok(octoState);
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Error retrieving octopus state from Dapr state store");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "State service unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving octopus state");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            using var activity = new Activity("OctoInteraction").Start();
            activity?.AddTag("action", request.Action);
            _interactionsCounter.Add(1, new KeyValuePair<string, object?>("action", request.Action));
            if (string.IsNullOrEmpty(request.Action))
            {
                _logger.LogWarning("Interaction request received with empty action");
                return BadRequest(new { error = "Action is required" });
            }

            try
            {
                var octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey) ?? new OctoState();

                // If octopus is inking, it might not respond to some interactions
                if (octoState.IsInking && request.Action.ToLower() != "sing")
                {
                    _logger.LogInformation("Octopus is inking and didn't respond to {Action}", request.Action);
                    return Ok(octoState);
                }

                // Process the interaction based on action type
                switch (request.Action.ToLower())
                {
                    case "pet":
                        // Octopuses are sensitive - their mood changes based on how they're petted
                        if (octoState.CurrentMood == "Nervous")
                        {
                            // Nervous octopus might get startled
                            if (new Random().Next(10) > 6)
                            {
                                octoState.IsInking = true;
                                octoState.UpdateMood("Scared");
                                octoState.ChangeColor("Black");
                                _logger.LogInformation("Nervous octopus startled by petting, released ink");

                                // Schedule ink cloud to dissipate
                                await _daprClient.PublishEventAsync("pubsub", "octo-ink", new
                                {
                                    Id = OctoStateKey,
                                    Timestamp = DateTime.UtcNow
                                });
                            }
                            else
                            {
                                // Successfully calmed the nervous octopus
                                octoState.UpdateMood("Curious");
                                octoState.Happiness += 5;
                                octoState.ChangeColor("Purple");
                                _logger.LogInformation("Nervous octopus calmed by gentle petting");
                            }
                        }
                        else
                        {
                            // Normal response to petting
                            octoState.Happiness += 8;

                            // Octopuses change color with mood shifts
                            if (octoState.Happiness > 75)
                            {
                                octoState.UpdateMood("Happy");
                                octoState.ChangeColor("Bright Yellow");
                            }

                            _logger.LogInformation("Octopus enjoyed being petted. Happiness: {Happiness}", octoState.Happiness);
                        }

                        // End camouflage if being petted
                        octoState.IsCamouflaged = false;
                        break;

                    case "feed":
                        octoState.Energy += 10;

                        // Octopuses are intelligent and might learn from food interactions
                        octoState.IntelligenceLevel += 2;

                        // Food makes them happy and curious
                        if (new Random().Next(10) > 5)
                        {
                            octoState.UpdateMood("Happy");
                            octoState.ChangeColor("Orange");
                        }
                        else
                        {
                            octoState.UpdateMood("Curious");
                            octoState.ChangeColor("Green");
                        }

                        // Sometimes they collect part of their food
                        if (new Random().Next(10) > 7)
                        {
                            string[] possibleItems = { "shell", "crab claw", "shiny pebble", "tiny pearl" };
                            string item = possibleItems[new Random().Next(possibleItems.Length)];
                            octoState.CollectItem(item);
                            _logger.LogInformation("Octopus collected a {Item} after feeding", item);
                        }

                        _logger.LogInformation("Octopus fed. Energy: {Energy}, Intelligence: {Intelligence}",
                            octoState.Energy, octoState.IntelligenceLevel);

                        // End camouflage and ink if feeding
                        octoState.IsCamouflaged = false;
                        octoState.IsInking = false;
                        break;

                    case "poke":
                        // Octopuses don't like being poked!
                        octoState.Chaos += 15;
                        octoState.Happiness -= 10;

                        // // Defense mechanisms
                        // if (new Random().Next(10) > 3)
                        // {
                        //     // Release ink cloud
                        //     octoState.IsInking = true;
                        //     octoState.UpdateMood("Scared");
                        //     octoState.ChangeColor("Black");

                        //     await _daprClient.PublishEventAsync("pubsub", "octo-ink", new
                        //     {
                        //         Id = OctoStateKey,
                        //         Timestamp = DateTime.UtcNow
                        //     });

                        //     _logger.LogWarning("Octopus poked and released ink defensively! Chaos: {Chaos}", octoState.Chaos);
                        // }
                        // else
                        // {
                        //     // Camouflage to hide
                        //     octoState.IsCamouflaged = true;
                        //     octoState.UpdateMood("Nervous");
                        //     _logger.LogInformation("Octopus poked and camouflaged to hide. Chaos: {Chaos}", octoState.Chaos);

                        //     // Schedule camouflage to wear off
                        //     await _daprClient.PublishEventAsync("pubsub", "octo-camouflage", new
                        //     {
                        //         Id = OctoStateKey,
                        //         Timestamp = DateTime.UtcNow
                        //     });
                        // }
                        break;

                    case "sing":
                        // Singing helps calm the octopus's emotional intensity
                        octoState.Chaos = Math.Max(0, octoState.Chaos - 10);
                        octoState.EmotionalIntensity = Math.Max(30, octoState.EmotionalIntensity - 5);

                        if (octoState.IsInking)
                        {
                            octoState.IsInking = false;
                            _logger.LogInformation("Singing helped clear the ink cloud");
                        }

                        // Singing makes them thoughtful or happy
                        if (new Random().Next(10) > 5)
                        {
                            octoState.UpdateMood("Thoughtful");
                            octoState.ChangeColor("Deep Blue");
                        }
                        else
                        {
                            octoState.UpdateMood("Happy");
                            octoState.ChangeColor("Pink");
                        }

                        _logger.LogInformation("Sang to octopus. Mood: {Mood}, EmotionalIntensity: {Intensity}",
                            octoState.CurrentMood, octoState.EmotionalIntensity);
                        break;

                    case "message":
                        if (!string.IsNullOrEmpty(request.Message))
                        {
                            octoState.LastMessage = request.Message;

                            // Emotional octopus responds to message content with mood changes
                            if (request.Message.Contains("happy") || request.Message.Contains("love") ||
                                request.Message.Contains("friend") || request.Message.Contains("nice"))
                            {
                                octoState.UpdateMood("Happy");
                                octoState.ChangeColor("Bright Yellow");
                                octoState.Happiness += 5;
                                _logger.LogInformation("Octopus mood changed to Happy from positive message");
                            }
                            else if (request.Message.Contains("sad") || request.Message.Contains("sorry") ||
                                     request.Message.Contains("alone") || request.Message.Contains("miss"))
                            {
                                octoState.UpdateMood("Sad");
                                octoState.ChangeColor("Blue");
                                octoState.EmotionalIntensity += 5;
                                _logger.LogInformation("Octopus mood changed to Sad from melancholy message");
                            }
                            else if (request.Message.Contains("interesting") || request.Message.Contains("wonder") ||
                                     request.Message.Contains("what") || request.Message.Contains("how") ||
                                     request.Message.Contains("?"))
                            {
                                octoState.UpdateMood("Curious");
                                octoState.ChangeColor("Teal");
                                octoState.IntelligenceLevel += 3;
                                _logger.LogInformation("Octopus mood changed to Curious from thought-provoking message");
                            }

                            _logger.LogInformation("Message sent to octopus: {Message}", request.Message);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown octopus action requested: {Action}", request.Action);
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                // Check for emotional intensity spikes - octopuses feel things very intensely
                if (octoState.EmotionalIntensity > 85 && new Random().Next(10) > 7)
                {
                    // Rapid color changes during emotional intensity
                    string[] vividColors = { "Vivid Purple", "Electric Blue", "Neon Orange", "Bright Red", "Vibrant Green" };
                    octoState.ChangeColor(vividColors[new Random().Next(vividColors.Length)]);
                    _logger.LogInformation("Octopus experiencing emotional intensity! Color rapidly changing to {Color}", octoState.CurrentColor);
                }

                // Check for evolution criteria
                if (octoState.ShouldEvolve())
                {
                    octoState.IsEvolved = true;
                    octoState.Evolution = "EmpatheticOctopus";

                    // Publish evolution event
                    await _daprClient.PublishEventAsync("pubsub", "octo-evolved", new
                    {
                        Id = OctoStateKey,
                        Evolution = octoState.Evolution,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Octopus evolved to {Evolution}!", octoState.Evolution);
                }

                // Keep values within bounds
                octoState.NormalizeValues();

                // Update interaction history
                octoState.LastInteraction = DateTime.UtcNow;

                // Save updated state
                await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);

                // Generate a thought for UI response
                string thought = await _thoughtsService.GenerateThoughtAsync(octoState);
                octoState.LastMessage = thought;

                return Ok(octoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing octopus interaction");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        [Topic("pubsub", "global-event")]
        [HttpPost("events")]
        public async Task<IActionResult> HandleGlobalEvent([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Octopus received global event");

            try
            {
                var octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey) ?? new OctoState();
                octoState.LastEvent = DateTime.UtcNow;

                // Octopuses are very responsive to environmental changes
                octoState.EmotionalIntensity += 8;

                // Might change color due to environmental stimulus
                string[] reactiveColors = { "Spotted Pattern", "Rippled Texture", "Mottled Brown", "Striped Yellow" };
                octoState.ChangeColor(reactiveColors[new Random().Next(reactiveColors.Length)]);

                // Might change mood based on event
                string[] possibleMoods = { "Curious", "Nervous", "Excited" };
                octoState.UpdateMood(possibleMoods[new Random().Next(possibleMoods.Length)]);

                _logger.LogInformation("Octopus reacted to global event. New mood: {Mood}, Color: {Color}",
                    octoState.CurrentMood, octoState.CurrentColor);

                await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling global event for octopus");
                return StatusCode(500);
            }
        }

        [Topic("pubsub", "octo-ink")]
        [HttpPost("ink-clear")]
        public async Task<IActionResult> HandleInkClear([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Processing octopus ink cloud clearing");

            // Wait for ink to dissipate
            await Task.Delay(TimeSpan.FromSeconds(20));

            try
            {
                var octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey);
                if (octoState != null && octoState.IsInking)
                {
                    octoState.IsInking = false;

                    // After inking, octopus is usually still nervous
                    octoState.UpdateMood("Nervous");
                    octoState.ChangeColor("Grey");

                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                    _logger.LogInformation("Octopus ink cloud dissipated. Current mood: {Mood}", octoState.CurrentMood);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling ink cloud clearing");
                return StatusCode(500);
            }
        }

        [Topic("pubsub", "octo-camouflage")]
        [HttpPost("camouflage-end")]
        public async Task<IActionResult> HandleCamouflageEnd([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Processing octopus camouflage ending");

            // Wait for camouflage to wear off
            await Task.Delay(TimeSpan.FromSeconds(30));

            try
            {
                var octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey);
                if (octoState != null && octoState.IsCamouflaged)
                {
                    octoState.IsCamouflaged = false;

                    // After camouflage, octopus cautiously returns to normal
                    octoState.UpdateMood("Curious");
                    octoState.ChangeColor("Teal");

                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                    _logger.LogInformation("Octopus ended camouflage. Current mood: {Mood}", octoState.CurrentMood);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camouflage ending");
                return StatusCode(500);
            }
        }
    }
}