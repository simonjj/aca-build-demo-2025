
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using ChillTurtleApi.Models;
using System.Threading.Tasks;
using Dapr;

namespace ChillTurtleApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PetController : ControllerBase
    {
        private readonly DaprClient _daprClient;
        private readonly ILogger<PetController> _logger;
        private const string StateStoreName = "statestore";
        private const string TurtleStateKey = "chillturtle";

        public PetController(DaprClient daprClient, ILogger<PetController> logger)
        {
            _daprClient = daprClient;
            _logger = logger;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetStateAsync()
        {
            try
            {
                var turtleState = await _daprClient.GetStateAsync<TurtleState>(StateStoreName, TurtleStateKey);
                if (turtleState == null)
                {
                    turtleState = new TurtleState();
                    await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);
                    _logger.LogInformation("Created new turtle state");
                }
                return Ok(turtleState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving turtle state");
                return StatusCode(500, "Failed to retrieve turtle state");
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            if (string.IsNullOrEmpty(request.Action))
            {
                _logger.LogWarning("Interaction request received with empty action");
                return BadRequest("Action is required");
            }

            var turtleState = await _daprClient.GetStateAsync<TurtleState>(StateStoreName, TurtleStateKey) ?? new TurtleState();
            UpdateMood(turtleState); // Update mood based on current state

            // Turtle is slow to respond - add some delay to simulate turtle behavior
            if (!turtleState.IsOverwhelmed)
            {
                await Task.Delay(200); // Turtle is chill and takes time to respond
            }

            switch (request.Action.ToLower())
            {
                case "pet":
                    turtleState.Happiness += 3; // Turtle enjoys petting, but less reactive than other pets
                    turtleState.StressLevel = Math.Max(0, turtleState.StressLevel - 2);
                    _logger.LogInformation("Turtle petted. Happiness: {Happiness}, Stress: {Stress}",
                        turtleState.Happiness, turtleState.StressLevel);
                    break;

                case "feed":
                    turtleState.Energy += 4;
                    turtleState.LastMeal = DateTime.UtcNow;
                    _logger.LogInformation("Turtle fed. Energy: {Energy}", turtleState.Energy);
                    break;

                case "poke":
                    turtleState.Chaos += 2; // Turtle is less chaotic by nature
                    turtleState.StressLevel += 5;
                    turtleState.Happiness -= 4;
                    _logger.LogInformation("Turtle poked. Chaos: {Chaos}, Stress: {Stress}",
                        turtleState.Chaos, turtleState.StressLevel);
                    break;

                case "sing":
                    turtleState.Chaos = Math.Max(0, turtleState.Chaos - 3);
                    turtleState.StressLevel = Math.Max(0, turtleState.StressLevel - 4);
                    turtleState.Happiness += 2;
                    _logger.LogInformation("Sang to the turtle. Chaos: {Chaos}, Stress: {Stress}",
                        turtleState.Chaos, turtleState.StressLevel);
                    break;

                case "message":
                    if (!string.IsNullOrEmpty(request.Message))
                    {
                        turtleState.LastMessage = request.Message;
                        _logger.LogInformation("Message sent to turtle: {Message}", request.Message);
                    }
                    break;

                default:
                    _logger.LogWarning("Unknown action requested: {Action}", request.Action);
                    return BadRequest($"Unknown action: {request.Action}");
            }

            // Check stress levels - turtle can retreat into shell when overwhelmed
            if (turtleState.StressLevel > 70 && !turtleState.IsInShell)
            {
                turtleState.IsInShell = true;
                _logger.LogInformation("Turtle retreated into shell due to stress");

                // Schedule a task to come out of shell after some time
                await _daprClient.PublishEventAsync("pubsub", "turtle-shell-retreat", new
                {
                    Id = TurtleStateKey,
                    Timestamp = DateTime.UtcNow
                });
            }

            // Check for evolution criteria 
            if (turtleState.Happiness > 70 && turtleState.Energy > 60 && turtleState.Age > 30)
            {
                if (!turtleState.IsEvolved)
                {
                    turtleState.IsEvolved = true;
                    turtleState.Evolution = "WiseTurtle";

                    // Publish evolution event
                    await _daprClient.PublishEventAsync("pubsub", "turtle-evolved", new
                    {
                        Id = TurtleStateKey,
                        Evolution = turtleState.Evolution,
                        Timestamp = DateTime.UtcNow
                    });

                    _logger.LogInformation("Turtle evolved to {Evolution}!", turtleState.Evolution);
                }
            }

            // Simulate turtle getting overwhelmed with too many requests
            if (turtleState.Chaos > 60)
            {
                turtleState.IsOverwhelmed = true;
                _logger.LogWarning("Turtle is overwhelmed by chaos!");
            }
            else if (turtleState.IsOverwhelmed && turtleState.Chaos < 30)
            {
                turtleState.IsOverwhelmed = false;
                _logger.LogInformation("Turtle is no longer overwhelmed");
            }

            // Keep values within bounds
            turtleState.Happiness = Math.Clamp(turtleState.Happiness, 0, 100);
            turtleState.Energy = Math.Clamp(turtleState.Energy, 0, 100);
            turtleState.Chaos = Math.Clamp(turtleState.Chaos, 0, 100);
            turtleState.StressLevel = Math.Clamp(turtleState.StressLevel, 0, 100);

            // Update interaction history
            turtleState.LastInteraction = DateTime.UtcNow;
            turtleState.InteractionCount++;

            // Turtle ages slowly with interactions
            if (turtleState.InteractionCount % 10 == 0)
            {
                turtleState.Age++;
                _logger.LogInformation("Turtle aged to {Age}", turtleState.Age);
            }

            // Save updated state
            await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);

            return Ok(turtleState);
        }

        [Topic("pubsub", "global-event")]
        [HttpPost("events")]
        public async Task<IActionResult> HandleGlobalEvent([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Received global event: {EventData}", (object)eventData);
            var turtleState = await _daprClient.GetStateAsync<TurtleState>(StateStoreName, TurtleStateKey) ?? new TurtleState();
            turtleState.LastEvent = DateTime.UtcNow;
            await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);
            return Ok();
        }

        [Topic("pubsub", "turtle-shell-retreat")]
        [HttpPost("shell-events")]
        public async Task<IActionResult> HandleShellRetreatEvent([FromBody] dynamic eventData)
        {
            _logger.LogInformation("Processing shell retreat event");

            // Simulate turtle coming out of shell after some time
            await Task.Delay(TimeSpan.FromSeconds(30));

            var turtleState = await _daprClient.GetStateAsync<TurtleState>(StateStoreName, TurtleStateKey);
            if (turtleState != null && turtleState.IsInShell)
            {
                turtleState.IsInShell = false;
                turtleState.StressLevel = Math.Max(0, turtleState.StressLevel - 20);
                await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);
                _logger.LogInformation("Turtle came out of shell, stress level: {Stress}", turtleState.StressLevel);
            }

            return Ok();
        }

        private void UpdateMood(TurtleState turtleState)
        {
            // Azure best practice: Use deterministic rules for mood calculation
            string previousMood = turtleState.Mood;

            // Determine mood based on key metrics
            if (turtleState.IsInShell)
            {
                turtleState.Mood = "Hiding";
            }
            else if (turtleState.StressLevel > 70)
            {
                turtleState.Mood = "Anxious";
            }
            else if (turtleState.Happiness > 80)
            {
                turtleState.Mood = "Happy";
            }
            else if (turtleState.Happiness > 60)
            {
                turtleState.Mood = "Content";
            }
            else if (turtleState.Energy < 20)
            {
                turtleState.Mood = "Sleepy";
            }
            else if (turtleState.Chaos > 50)
            {
                turtleState.Mood = "Agitated";
            }
            else
            {
                turtleState.Mood = "Calm"; // Default turtle mood - they're chill
            }

            // Log mood changes for observability
            if (previousMood != turtleState.Mood)
            {
                _logger.LogInformation("Turtle mood changed from {PreviousMood} to {CurrentMood}",
                    previousMood, turtleState.Mood);
            }
        }
    }
}