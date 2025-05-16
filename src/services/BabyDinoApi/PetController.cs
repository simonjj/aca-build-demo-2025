using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using BabyDinoApi.Models;
using BabyDinoApi.Services;

namespace BabyDinoApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PetController> _logger;
        private readonly DinoThoughtsService _thoughtsService;
        private const string StateStoreName = "statestore";
        private const string DinoStateKey = "babydino";

        public PetController(
            DaprClient daprClient,
            ILogger<PetController> logger,
            DinoThoughtsService thoughtsService)
        {
            _daprClient = daprClient;
            _logger = logger;
            _thoughtsService = thoughtsService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetStateAsync()
        {
            try
            {
                var dinoState = await _daprClient.GetStateAsync<DinoState>(StateStoreName, DinoStateKey);
                if (dinoState == null)
                {
                    dinoState = new DinoState();
                    await _daprClient.SaveStateAsync(StateStoreName, DinoStateKey, dinoState);
                    _logger.LogInformation("Created new baby dino state");
                }

                // Generate a thought for UI display
                dinoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(dinoState);

                return Ok(dinoState);
            }
            catch (DaprException ex)
            {
                _logger.LogError(ex, "Error retrieving baby dino state from Dapr state store");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "State service unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error retrieving baby dino state");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            if (string.IsNullOrEmpty(request.Action))
            {
                _logger.LogWarning("Interaction request received with empty action");
                return BadRequest(new { error = "Action is required" });
            }

            try
            {
                var dinoState = await _daprClient.GetStateAsync<DinoState>(StateStoreName, DinoStateKey) ?? new DinoState();

                // If the dino is napping, there's a chance it won't wake up for some interactions
                if (dinoState.IsNapping && request.Action.ToLower() != "sing" && new Random().Next(10) > 3)
                {
                    _logger.LogInformation("Baby dino is napping and didn't wake up for {Action}", request.Action);
                    dinoState.LastMessage = "*soft baby dino snores* zZz...";
                    return Ok(dinoState);
                }

                // Process the interaction
                switch (request.Action.ToLower())
                {
                    case "pet":
                        dinoState.Happiness += 8; // Baby dinos love being petted!
                        dinoState.PlayfulnessLevel += 5;

                        if (dinoState.IsNapping)
                        {
                            dinoState.IsNapping = false;
                            _logger.LogInformation("Baby dino woke up from being petted");
                        }

                        _logger.LogInformation("Baby dino petted. Happiness: {Happiness}, Playfulness: {Playfulness}",
                            dinoState.Happiness, dinoState.PlayfulnessLevel);
                        break;

                    case "feed":
                        dinoState.Energy += 10;
                        dinoState.Growth += 3; // Feeding helps the baby dino grow

                        if (dinoState.IsNapping)
                        {
                            dinoState.IsNapping = false;
                            _logger.LogInformation("Baby dino woke up for food");
                        }

                        _logger.LogInformation("Baby dino fed. Energy: {Energy}, Growth: {Growth}",
                            dinoState.Energy, dinoState.Growth);
                        break;

                    case "poke":
                        // Baby dinos get startled when poked
                        dinoState.Chaos += 12;
                        dinoState.Happiness -= 5;

                        if (dinoState.IsNapping)
                        {
                            dinoState.IsNapping = false;
                            _logger.LogInformation("Baby dino startled awake by poking");
                        }

                        // Sometimes they throw a tantrum
                        if (new Random().Next(10) > 6)
                        {
                            dinoState.Chaos += 10;
                            await _daprClient.PublishEventAsync("pubsub", "dino-tantrum", new
                            {
                                Id = DinoStateKey,
                                Chaos = dinoState.Chaos,
                                Timestamp = DateTime.UtcNow
                            });
                            _logger.LogWarning("Baby dino threw a tantrum! Chaos: {Chaos}", dinoState.Chaos);
                        }

                        _logger.LogInformation("Baby dino poked. Chaos: {Chaos}, Happiness: {Happiness}",
                            dinoState.Chaos, dinoState.Happiness);
                        break;
                    default:
                        _logger.LogWarning("Unknown baby dino action requested: {Action}", request.Action);
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                // Update interaction history
                dinoState.LastInteraction = DateTime.UtcNow;

                UpdateMood(dinoState);
                // Save updated state
                await _daprClient.SaveStateAsync(StateStoreName, DinoStateKey, dinoState);

                // Generate a thought for UI response
                string thought = await _thoughtsService.GenerateThoughtAsync(dinoState);
                dinoState.LastMessage = thought;

                return Ok(dinoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing baby dino interaction");
                return StatusCode(StatusCodes.Status500InternalServerError, new { error = "Internal server error" });
            }
        }

        public void UpdateMood(DinoState dinoState)
        {
            if (dinoState.Happiness < 20)
            {
                dinoState.Mood = "Sad";
            }
            else if (dinoState.Energy < 20)
            {
                dinoState.Mood = "Tired";
            }
            else if (dinoState.Chaos > 80)
            {
                dinoState.Mood = "Hyperactive";
            }
            else
            {
                dinoState.Mood = "Playful";
            }
        }
    }
}