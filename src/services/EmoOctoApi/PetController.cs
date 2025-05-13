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

        // ─── OpenTelemetry Instrumentation ────────────────────────────────────────────

        // 1️⃣ ActivitySource for tracing
        private static readonly ActivitySource _activitySource = new("EmoOctoApi.PetController", "1.0.0");

        // 2️⃣ Meter and instruments for metrics
        private static readonly Meter _meter = new("EmoOctoApi", "1.0.0");
        private static readonly Counter<long> _interactionsCounter =
            _meter.CreateCounter<long>("octo_interactions_total", description: "Total number of interact calls");
        private static readonly Counter<long> _errorCounter =
            _meter.CreateCounter<long>("octo_errors_total", description: "Total number of errors");
        private static readonly Histogram<double> _durationHistogram =
            _meter.CreateHistogram<double>("octo_request_duration_ms", description: "Request duration in milliseconds");

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
            // Start a root span for this HTTP operation
            using var activity = _activitySource.StartActivity("GetOctoState", ActivityKind.Server);
            activity?.SetTag("http.method", "GET");
            activity?.SetTag("http.endpoint", "/pet/state");
            var sw = Stopwatch.StartNew();
            try
            {

                // 2️⃣ Child span for the Dapr GET from redis
                OctoState octoState;
                using (var getSpan = _activitySource.StartActivity("Dapr.GetState", ActivityKind.Server))
                {
                    getSpan?.SetTag("state.store", StateStoreName);
                    getSpan?.SetTag("state.key", OctoStateKey);
                    octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey);
                }

                if (octoState is null)
                {
                    _logger.LogInformation("State not found; initializing new OctoState");

                    // 3️⃣ Child span for the Dapr SAVE to redis
                    using var saveSpan = _activitySource.StartActivity("Dapr.SaveState", ActivityKind.Server);
                    saveSpan?.SetTag("state.store", StateStoreName);
                    saveSpan?.SetTag("state.key", OctoStateKey);

                    octoState = new OctoState();
                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                }

                // 4️⃣ Child span for thought generation (your backend service call)
                using (var thoughtSpan = _activitySource.StartActivity("GenerateThought", ActivityKind.Server))
                {
                    octoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(octoState);
                    thoughtSpan?.SetTag("thought.length", octoState.LastMessage?.Length ?? 0);
                }

                // Add an event to the trace
                activity?.AddEvent(new ActivityEvent("StateFetched"));
                return Ok(octoState);
            }
            catch (DaprException dex)
            {
                _logger.LogError(dex, "Dapr failure retrieving or saving state");
                activity?.SetStatus(ActivityStatusCode.Error, dex.Message);
                _errorCounter.Add(1, KeyValuePair.Create("endpoint", (object?)"GetState"));
                return StatusCode(503, new { error = "State store unavailable" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetStateAsync");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _errorCounter.Add(1, new[] { new KeyValuePair<string, object?>("endpoint", "GetState") });
                return StatusCode(500, new { error = "Internal server error" });
            }
            finally
            {
                sw.Stop();
                _durationHistogram.Record(sw.Elapsed.TotalMilliseconds, new[] { new KeyValuePair<string, object?>("endpoint", "GetState") });
            }
        }

        [HttpPost("interact")]
        public async Task<IActionResult> InteractAsync([FromBody] InteractionRequest request)
        {
            // 1️⃣ Root span for the HTTP POST /pet/interact
            using var activity = _activitySource.StartActivity("OctoInteraction", ActivityKind.Server);
            activity?.SetTag("http.method", "POST");
            activity?.SetTag("http.endpoint", "/pet/interact");
            activity?.SetTag("interaction.action", request.Action);
            var sw = Stopwatch.StartNew();

            _interactionsCounter.Add(1, new[] { new KeyValuePair<string, object?>("action", request.Action) });

            if (string.IsNullOrWhiteSpace(request.Action))
            {
                _logger.LogWarning("Empty action from client");
                activity?.SetStatus(ActivityStatusCode.Error, "MissingAction");
                _errorCounter.Add(1, new("endpoint", "Interact"), new("reason", "MissingAction"));
                return BadRequest(new { error = "Action is required" });
            }

            try
            {

                // 2️⃣ Child span for Dapr GET
                OctoState octoState;
                using (var getSpan = _activitySource.StartActivity("Dapr.GetState", ActivityKind.Server))
                {
                    getSpan?.SetTag("state.store", StateStoreName);
                    getSpan?.SetTag("state.key", OctoStateKey);
                    activity?.SetTag("http.method", "GET");
                    octoState = await _daprClient.GetStateAsync<OctoState>(StateStoreName, OctoStateKey)
                                 ?? new OctoState();
                }

                // Business logic branch
                switch (request.Action.ToLowerInvariant())
                {
                    case "pet":
                        HandlePet(octoState, activity);
                        break;
                    case "feed":
                        HandleFeed(octoState, activity);
                        break;
                    case "poke":
                        HandlePoke(octoState, activity);
                        break;
                    case "sing":
                        HandleSing(octoState, activity);
                        break;
                    case "message":
                        HandleMessage(octoState, request.Message, activity);
                        break;
                    default:
                        _logger.LogWarning($"Unknown action: {request.Action}");
                        activity?.SetStatus(ActivityStatusCode.Error, "UnknownAction");
                        _errorCounter.Add(1, new[]
                        {
                            new KeyValuePair<string, object?>("endpoint", "Interact"),
                            new KeyValuePair<string, object?>("reason", "UnknownAction")
                        });
                        return BadRequest(new { error = $"Unknown action: {request.Action}" });
                }

                // 4️⃣ Child span for Dapr SAVE
                using (var saveSpan = _activitySource.StartActivity("Dapr.SaveState", ActivityKind.Client))
                {
                    saveSpan?.SetTag("state.store", StateStoreName);
                    saveSpan?.SetTag("state.key", OctoStateKey);
                    await _daprClient.SaveStateAsync(StateStoreName, OctoStateKey, octoState);
                    activity?.AddEvent(new ActivityEvent("StateSaved"));
                }

                // 5️⃣ Child span for final thought generation
                using (var thoughtSpan = _activitySource.StartActivity("GenerateThought", ActivityKind.Internal))
                {
                    octoState.LastMessage = await _thoughtsService.GenerateThoughtAsync(octoState);
                    thoughtSpan?.SetTag("thought.length", octoState.LastMessage?.Length ?? 0);
                    activity?.AddEvent(new ActivityEvent("ThoughtGenerated"));
                }

                return Ok(octoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing interaction '{request.Action}'");
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                _errorCounter.Add(1, new[]
                {
                    new KeyValuePair<string, object?>("endpoint", "Interact"),
                    new KeyValuePair<string, object?>("reason", "ProcessingError")
                });
                return StatusCode(500, new { error = "Internal server error" });
            }
            finally
            {
                sw.Stop();
                _durationHistogram.Record(sw.Elapsed.TotalMilliseconds, new[]
                {
                    new KeyValuePair<string, object?>("endpoint", "Interact"),
                    new KeyValuePair<string, object?>("action", request.Action)
                });
            }
        }

        // ─── Helper methods for each action type ────────────────────────────────────

        private void HandlePet(OctoState octoState, Activity? activity)
        {
            // Example business logic
            octoState.Happiness += 5;
            if (octoState.Happiness > 75)
            {
                octoState.UpdateMood("Happy");
                activity?.AddTag("mood", "Happy");
            }
            _logger.LogInformation($"Pet: Happiness={octoState.Happiness}"); ;
        }

        private void HandleFeed(OctoState octoState, Activity? activity)
        {
            octoState.Energy += 10;
            _logger.LogInformation($"Feed: Energy={octoState.Energy}");
        }

        private void HandlePoke(OctoState octoState, Activity? activity)
        {
            // Azure best practice: Add tracing events for important state changes
            activity?.AddEvent(new ActivityEvent("PokeStarted", tags: new ActivityTagsCollection(
                new[] { new KeyValuePair<string, object?>("initialChaos", octoState.Chaos) })));

            octoState.Chaos += 15;
            _logger.LogInformation($"Poke: Chaos={octoState.Chaos}");

            // Azure best practice: Apply proper throttling with structured logging
            if (octoState.Chaos > 100)
            {
                // Azure best practice: Record throttling events in metrics for monitoring
                _errorCounter.Add(1, new[] {
            new KeyValuePair<string, object?>("endpoint", "Interact"),
            new KeyValuePair<string, object?>("reason", "ThrottlingApplied")
        });

                activity?.AddTag("throttling.applied", true);
                activity?.AddEvent(new ActivityEvent("ThrottlingApplied"));

                octoState.UpdateMood("Furious");
                octoState.LastMessage = "Octopus is overwhelmed and needs a break. Try again later.";

                _logger.LogWarning($"Throttling applied: Chaos={octoState.Chaos}");

                // Azure best practice: Add contextual information to span for better diagnostics
                activity?.AddTag("mood", "Furious");
                activity?.SetStatus(ActivityStatusCode.Error, "ThrottlingApplied");

                throw new ThrottlingException(
                    $"Chaos level too extreme ({octoState.Chaos}). Throttling!"
                );
            }

            activity?.AddEvent(new ActivityEvent("PokeCompleted", tags: new ActivityTagsCollection(
                new[] { new KeyValuePair<string, object?>("finalChaos", octoState.Chaos) })));
        }


        private void HandleSing(OctoState octoState, Activity? activity)
        {
            octoState.Chaos = Math.Max(0, octoState.Chaos - 10);
            _logger.LogInformation($"Sing: Chaos now={octoState.Chaos}");
        }

        private void HandleMessage(OctoState octoState, string? message, Activity? activity)
        {
            octoState.LastMessage = message ?? string.Empty;
            _logger.LogInformation($"Message: {message}");
        }

        // Azure best practice: Create a specific exception type for throttling to enable proper handling/status codes
        public class ThrottlingException : Exception
        {
            public string message { get; } = "";

            public ThrottlingException(string message) : base(message)
            {
                this.message = message;
            }
        }
    }
}