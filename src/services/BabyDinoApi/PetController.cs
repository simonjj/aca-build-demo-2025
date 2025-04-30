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

                        // Baby dinos sometimes learn tricks when fed
                        if (new Random().Next(10) > 7)
                        {
                            string[] possibleTricks = { "spin", "hop", "roar", "tail wag", "hide-and-seek" };
                            string newTrick = possibleTricks[new Random().Next(possibleTricks.Length)];

                            if (dinoState.LearnTrick(newTrick))
                            {
                                _logger.LogInformation("Baby dino learned a new trick: {Trick}", newTrick);
                            }
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

                    case "sing":
                        // Singing can calm the baby dino or make it sleepy
                        dinoState.Chaos = Math.Max(0, dinoState.Chaos - 10);

                        // Higher chance of napping if energy is low
                        if (!dinoState.IsNapping && (dinoState.Energy < 50 || new Random().Next(10) > 7))
                        {
                            dinoState.IsNapping = true;
                            _logger.LogInformation("Baby dino fell asleep from the singing");

                            // Schedule a wake-up event
                            await _daprClient.PublishEventAsync("pubsub", "dino-nap", new
                            {
                                Id = DinoStateKey,
                                Timestamp = DateTime.UtcNow
                            });
                        }

                        _logger.LogInformation("Sang to baby dino. Chaos: {Chaos}, IsNapping: {IsNapping}",
                            dinoState.Chaos, dinoState.IsNapping);
                        break;

                    case "message":
                        if (!string.IsNullOrEmpty(request.Message))
                        {
                            dinoState.LastMessage = request.Message;

                            // Baby dinos might learn from messages
                            if (request.Message.Contains("trick") && !dinoState.IsNapping)
                            {
                                string[] possibleTricks = { "bow", "roll", "play dead", "dance", "catch" };
                                string trickToLearn = possibleTricks[new Random().Next(possibleTricks.Length)];

                                if (dinoState.LearnTrick(trickToLearn))
                                {
                                    _logger.LogInformation("Baby dino learned a new trick from message: {Trick}", trickToLearn);
                                }
                            }

                            _logger.LogInformation("Message sent to baby dino: {Message}", request.Message);
                        }
                        break;

                    default:
                        _logger.LogWarning("Unknown baby dino action requested: {Action}", request.Action);
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                // Check for evolution criteria
                if (dinoState.ShouldEvolve())
                {
                    dinoState.IsEvolved = true;
                    dinoState.Evolution = "JuvenileDino";
                    dinoState.CutenessFactor -= 10; // Still cute, but more mature
                    dinoState.PlayfulnessLevel -= 5; // Still playful, but more dignified

                    // Publish evolution event
                    await _daprClient.PublishEventAsync("pubsub", "dino-evolved", new
                    {
                        Id = DinoStateKey,
                        Evolution = dinoState.Evolution,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Baby dino evolved to {Evolution}!", dinoState.Evolution);
                }

                // Keep values within bounds
                dinoState.NormalizeValues();

                // Update interaction history
                dinoState.LastInteraction = DateTime.UtcNow;

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

        [Topic("pubsub", "global-event")]
        [HttpPost("events")]
        public async Task<IActionResult> HandleGlobalEvent([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Baby dino received global event");

            try
            {
                var dinoState = await _daprClient.GetStateAsync<DinoState>(StateStoreName, DinoStateKey) ?? new DinoState();
                dinoState.LastEvent = DateTime.UtcNow;

                // Baby dinos get excited by events
                dinoState.PlayfulnessLevel += 5;
                dinoState.Chaos += 3;

                // Sometimes they wake up from events
                if (dinoState.IsNapping && new Random().Next(10) > 7)
                {
                    dinoState.IsNapping = false;
                    _logger.LogInformation("Baby dino woke up from global event");
                }

                await _daprClient.SaveStateAsync(StateStoreName, DinoStateKey, dinoState);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling global event for baby dino");
                return StatusCode(500);
            }
        }

        [Topic("pubsub", "dino-nap")]
        [HttpPost("wake-up")]
        public async Task<IActionResult> HandleNapEvent([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Processing baby dino nap event");

            // Wait a bit for the dino to nap
            await Task.Delay(TimeSpan.FromSeconds(new Random().Next(20, 60)));

            try
            {
                var dinoState = await _daprClient.GetStateAsync<DinoState>(StateStoreName, DinoStateKey);
                if (dinoState != null && dinoState.IsNapping)
                {
                    dinoState.IsNapping = false;
                    dinoState.Energy += 20; // Naps are energizing!
                    dinoState.NormalizeValues();

                    await _daprClient.SaveStateAsync(StateStoreName, DinoStateKey, dinoState);
                    _logger.LogInformation("Baby dino woke up from nap. Energy: {Energy}", dinoState.Energy);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling nap event for baby dino");
                return StatusCode(500);
            }
        }
    }
}