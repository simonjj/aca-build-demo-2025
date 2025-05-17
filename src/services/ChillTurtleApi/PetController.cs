
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using ChillTurtleApi.Models;
using System.Threading.Tasks;
using Dapr;
using ChillTurtleApi.Services;

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
        private TurtleThoughtsService _thoughtsService;

        public PetController(DaprClient daprClient, ILogger<PetController> logger, TurtleThoughtsService thoughtsService)
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
                var turtleState = await _daprClient.GetStateAsync<TurtleState>(StateStoreName, TurtleStateKey);
                if (turtleState == null)
                {
                    turtleState = new TurtleState();
                    await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);
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

            switch (request.Action.ToLower())
            {
                case "pet":
                    turtleState.Happiness += 3; // Turtle enjoys petting, but less reactive than other pets
                    turtleState.StressLevel = Math.Max(0, turtleState.StressLevel - 2);
                    break;

                case "feed":
                    turtleState.Energy += 4;
                    turtleState.LastMeal = DateTime.UtcNow;
                    break;

                case "poke":
                    turtleState.Chaos += 2; // Turtle is less chaotic by nature
                    turtleState.StressLevel += 5;
                    turtleState.Happiness -= 4;
                    break;
                default:
                    _logger.LogWarning("Unknown action requested: {Action}", request.Action);
                    return BadRequest($"Unknown action: {request.Action}");
            }

            // Simulate turtle getting overwhelmed with too many requests
            if (turtleState.Chaos > 60)
            {
                turtleState.IsOverwhelmed = true;
            }
            else if (turtleState.IsOverwhelmed && turtleState.Chaos < 30)
            {
                turtleState.IsOverwhelmed = false;
            }

            turtleState.LastMessage = _thoughtsService.GenerateThoughts(turtleState);
            // Save updated state
            await _daprClient.SaveStateAsync(StateStoreName, TurtleStateKey, turtleState);

            return Ok(turtleState);
        }

        private void UpdateMood(TurtleState turtleState)
        {
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
        }
    }
}