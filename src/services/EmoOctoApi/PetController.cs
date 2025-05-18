using Dapr;
using Dapr.Client;
using Microsoft.AspNetCore.Mvc;
using EmoOctoApi.Models;
using EmoOctoApi.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

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

        private static readonly ActivitySource _activitySource = new("EmoOctoApi.PetController", "1.0.0");
        private static readonly Meter _meter = new("EmoOctoApi", "1.0.0");

        private static readonly Counter<long> _interactionsCounter = _meter.CreateCounter<long>("octo_interactions_total", description: "Total number of interact calls");
        private static readonly Counter<long> _errorCounter = _meter.CreateCounter<long>("octo_errors_total", description: "Total number of errors");
        private static readonly Counter<long> _pokeCounter = _meter.CreateCounter<long>("octo_poke_total", description: "Total number of pokes");
        private static readonly Histogram<double> _durationHistogram = _meter.CreateHistogram<double>("octo_request_duration_ms", description: "Request duration in milliseconds");
        private static readonly Counter<long> _throttlingCounter = _meter.CreateCounter<long>("octo_throttling_total", description: "Number of times the octopus has throttled interactions");
        private static readonly Histogram<int> _chaosLevelHistogram =
            _meter.CreateHistogram<int>("octo_chaos_level", description: "Recorded chaos level of the octopus");

        private static readonly Histogram<double> _timeSinceLastThrottleHistogram =
            _meter.CreateHistogram<double>("octo_time_since_last_throttle_seconds", description: "Time since last throttle in seconds");

        private static readonly Histogram<double> _throttlingDurationHistogram =
            _meter.CreateHistogram<double>("octo_throttling_duration_seconds", description: "Duration of throttling periods");

        private static int _currentChaosLevel = 0;
        private static DateTime _lastThrottleTime = DateTime.MinValue;

        public PetController(DaprClient daprClient, ILogger<PetController> logger, OctoThoughtsService thoughtsService)
        {
            _daprClient = daprClient;
            _logger = logger;
            _thoughtsService = thoughtsService;
        }

        [HttpGet("state")]
        public async Task<IActionResult> GetStateAsync()
        {
            using var activity = _activitySource.StartActivity("GetOctoState", ActivityKind.Server);
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.endpoint", "/pet/state");
            var sw = Stopwatch.StartNew();

            try
            {
                OctoState octoState;
                using (var getSpan = _activitySource.StartActivity("Dapr.GetState", ActivityKind.Client))
                {
                    getSpan?.SetTag("state.store", StateStoreName);
                    getSpan?.SetTag("state.key", OctoStateKey);
                    octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey);
                }

                if (octoState == null)
                {
                    _logger.LogInformation("State not found; initializing new OctoState");
                    using var saveSpan = _activitySource.StartActivity("Dapr.SaveState", ActivityKind.Client);
                    octoState = new OctoState();
                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                }

                using (var thoughtSpan = _activitySource.StartActivity("GenerateThought", ActivityKind.Internal))
                {
                    octoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(octoState);
                    thoughtSpan?.SetTag("thought.length", octoState.LastMessage?.Length ?? 0);
                }

                activity?.AddEvent(new ActivityEvent("StateFetched"));
                return Ok(octoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving state");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _errorCounter.Add(1, new KeyValuePair<string, object?>("endpoint", "GetState"));
                return StatusCode(500, new { error = "Internal server error" });
            }
            finally
            {
                sw.Stop();
                _durationHistogram.Record(sw.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("endpoint", "GetState"));
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            using var activity = _activitySource.StartActivity("OctoInteraction", ActivityKind.Server);
            activity?.SetTag("http.method", "POST");
            activity?.SetTag("http.endpoint", "/pet/interact");
            activity?.SetTag("interaction.action", request.Action);
            var sw = Stopwatch.StartNew();

            _interactionsCounter.Add(1, new KeyValuePair<string, object?>("action", request.Action));

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                _logger.LogWarning("Empty action from client");
                activity?.SetStatus(ActivityStatusCode.Error, "MissingAction");
                _errorCounter.Add(1, new("endpoint", "Interact"), new("reason", "MissingAction"));
                return BadRequest(new { error = "Action is required" });
            }

            try
            {
                OctoState octoState;
                using (var getSpan = _activitySource.StartActivity("Dapr.GetState", ActivityKind.Client))
                {
                    getSpan?.SetTag("state.store", StateStoreName);
                    getSpan?.SetTag("state.key", OctoStateKey);
                    octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey) ?? new OctoState();
                }

                switch (request.Action.ToLowerInvariant())
                {
                    case "pet":
                        using (var petSpan = _activitySource.StartActivity("PetOctopus", ActivityKind.Internal))
                        {
                            HandlePet(octoState, petSpan);
                        }
                        break;
                    case "feed":
                        using (var feedSpan = _activitySource.StartActivity("FeedOctopus", ActivityKind.Internal))
                        {
                            HandleFeed(octoState, feedSpan);
                        }
                        break;
                    case "poke":
                        using (var pokeSpan = _activitySource.StartActivity("PokeOctopus", ActivityKind.Internal))
                        {
                            pokeSpan?.SetTag("interaction.Action", request.Action);
                            pokeSpan?.SetTag("interaction.Message", request.Message);
                            pokeSpan?.SetTag("octopus.chaos", octoState.Chaos);
                            HandlePoke(octoState, pokeSpan);
                        }
                        break;
                    default:
                        _logger.LogWarning($"Unknown action: {request.Action}");
                        activity?.SetStatus(ActivityStatusCode.Error, "UnknownAction");
                        _errorCounter.Add(1, new[] {
                            new KeyValuePair<string, object?>("endpoint", "Interact"),
                            new KeyValuePair<string, object?>("reason", "UnknownAction")
                        });
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                using (var saveSpan = _activitySource.StartActivity("Dapr.SaveState", ActivityKind.Client))
                {
                    saveSpan?.SetTag("state.store", StateStoreName);
                    saveSpan?.SetTag("state.key", OctoStateKey);
                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                }

                using (var thoughtSpan = _activitySource.StartActivity("GenerateThought", ActivityKind.Internal))
                {
                    octoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(octoState);
                    thoughtSpan?.SetTag("thought.length", octoState.LastMessage?.Length ?? 0);
                }

                return Ok(octoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing interaction '{request.Action}'");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _errorCounter.Add(1, new[] {
                    new KeyValuePair<string, object?>("endpoint", "Interact"),
                    new KeyValuePair<string, object?>("reason", "ProcessingError")
                });
                return StatusCode(500, new { error = "Internal server error" });
            }
            finally
            {
                sw.Stop();
                _durationHistogram.Record(sw.Elapsed.TotalMilliseconds, new[] {
                    new KeyValuePair<string, object?>("endpoint", "Interact"),
                    new KeyValuePair<string, object?>("action", request.Action)
                });
            }
        }

        private void HandlePet(OctoState octoState, Activity? span)
        {
            octoState.Happiness += 5;
            span?.SetTag("happiness", octoState.Happiness);
            if (octoState.Happiness > 75)
            {
                octoState.UpdateMood("Happy");
                span?.SetTag("mood", "Happy");
            }
        }

        private void HandleFeed(OctoState octoState, Activity? span)
        {
            octoState.Energy += 10;
            span?.SetTag("energy", octoState.Energy);
            if (octoState.Energy > 80)
            {
                octoState.UpdateMood("Energetic");
                span?.SetTag("mood", "Energetic");
            }
        }

        private void HandlePoke(OctoState octoState, Activity? parentActivity)
        {
            using (var pokeStartedSpan = _activitySource.StartActivity("PokeStarted", ActivityKind.Internal))
            {
                pokeStartedSpan?.SetTag("initialChaos", octoState.Chaos);
                octoState.Chaos += 15;
                _chaosLevelHistogram.Record(octoState.Chaos, new KeyValuePair<string, object?>("action", "poke"));
                _timeSinceLastThrottleHistogram.Record(
                    (DateTime.UtcNow - _lastThrottleTime).TotalSeconds,
                    new KeyValuePair<string, object?>("action", "poke"));
                _currentChaosLevel = octoState.Chaos;
                _pokeCounter.Add(1);
                pokeStartedSpan?.SetTag("finalChaos", octoState.Chaos);
            }

            using (var throttleSpan = _activitySource.StartActivity("ThrottlingCheck", ActivityKind.Internal))
            {
                throttleSpan?.SetTag("octopus.chaos", octoState.Chaos);
                throttleSpan?.SetTag("action", "poke");

                if (octoState.Chaos > 1000)
                {
                    _lastThrottleTime = DateTime.UtcNow;
                    _throttlingCounter.Add(1);
                    _errorCounter.Add(1);
                    throttleSpan?.SetTag("throttling.applied", true);
                    throttleSpan?.SetTag("throttling.reason", "ChaosLevelExceeded");
                    octoState.UpdateMood("Furious");
                    parentActivity?.SetStatus(ActivityStatusCode.Error, "ThrottlingApplied");
                    throw new ThrottlingException($"Chaos level too extreme ({octoState.Chaos}). Throttling!");
                }
                else
                {
                    throttleSpan?.SetTag("throttling.applied", false);
                }
            }
        }

        public class ThrottlingException : Exception
        {
            public ThrottlingException(string message) : base(message) { }
        }
    }
}
