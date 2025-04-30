using System.Text.Json;
using ChaosDragonApi.Models;
using Microsoft.ApplicationInsights;

namespace ChaosDragonApi.Services
{
    public class DragonThoughtsService
    {
        private readonly ILogger<DragonThoughtsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly TelemetryClient? _telemetryClient;

        private readonly string[] _chaoticThoughts = new[]
        {
            "BURN EVERYTHING! MWAHAHAHA!",
            "I feel like destroying a village today.",
            "Why are these humans so tiny and annoying?",
            "That was the worst petting ever. Do it better!",
            "MORE TREASURE! MY HOARD MUST GROW!",
            "I'm bored. I should start a fire or something.",
            "These humans have no idea what I'm capable of...",
            "They call this 'feeding'? I eat KINGDOMS for breakfast!"
        };

        private readonly string[] _calmThoughts = new[]
        {
            "Maybe I won't incinerate everything today...",
            "That human is... acceptable.",
            "The singing is... less annoying than usual.",
            "I might spare this small village. For now.",
            "My treasure looks particularly shiny today.",
            "A calm dragon is still a dangerous dragon.",
            "I wonder if they realize I'm just tolerating them."
        };

        private readonly string[] _evolvedThoughts = new[]
        {
            "With these wings, I shall darken the skies!",
            "My fire burns hotter than a thousand suns!",
            "All shall tremble before my evolved might!",
            "These wings are most excellent for dramatic exits.",
            "I feel the ancient power of my ancestors flowing through me.",
            "The world looks so tiny and flammable from up here.",
            "Now I can rain fire from above. PERFECT."
        };

        private readonly Random _random = new();

        public DragonThoughtsService(
            ILogger<DragonThoughtsService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            TelemetryClient? telemetryClient = null)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _telemetryClient = telemetryClient;
        }

        public string GenerateThought(DragonState dragonState)
        {
            try
            {
                string thought;

                // Track metrics for the dragon's state
                _telemetryClient?.TrackMetric("DragonChaos", dragonState.Chaos);
                _telemetryClient?.TrackMetric("DragonFireIntensity", dragonState.FireBreathIntensity);

                // Generate different thoughts based on the dragon's state
                if (dragonState.IsEvolved && dragonState.HasWings)
                {
                    thought = _evolvedThoughts[_random.Next(_evolvedThoughts.Length)];
                    _telemetryClient?.TrackEvent("DragonEvolvedThought");
                }
                else if (dragonState.Chaos > 70)
                {
                    thought = _chaoticThoughts[_random.Next(_chaoticThoughts.Length)];
                    _telemetryClient?.TrackEvent("DragonChaoticThought");
                }
                else
                {
                    thought = _calmThoughts[_random.Next(_calmThoughts.Length)];
                    _telemetryClient?.TrackEvent("DragonCalmThought");
                }

                // Try to personalize the thought with recent interactions
                if (!string.IsNullOrEmpty(dragonState.LastMessage) && _random.Next(10) > 6)
                {
                    thought += $" (thinking about: \"{dragonState.LastMessage}\")";
                }

                // Add treasure references if the dragon has a hoard
                if (dragonState.HoardSize > 0 && _random.Next(10) > 7)
                {
                    thought += $" *eyes glitter at the {dragonState.HoardSize} treasures in the hoard*";
                }

                _logger.LogInformation("Generated dragon thought: {Thought}", thought);
                return thought;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dragon thought");
                return "...";
            }
        }

        public async Task<string> GenerateThoughtAsync(DragonState dragonState)
        {
            try
            {
                // Check if we should use the external AI service
                string? aiServiceUrl = _configuration["AIServices:ThoughtGenerator"];

                if (!string.IsNullOrEmpty(aiServiceUrl))
                {
                    using var client = _httpClientFactory.CreateClient("ThoughtGenerator");
                    client.Timeout = TimeSpan.FromSeconds(2); // Implement timeout for resilience

                    var request = new HttpRequestMessage(HttpMethod.Post, aiServiceUrl)
                    {
                        Content = new StringContent(
                            JsonSerializer.Serialize(new
                            {
                                PetType = "Dragon",
                                Mood = DetermineMood(dragonState),
                                IsEvolved = dragonState.IsEvolved,
                                HasWings = dragonState.HasWings,
                                FireIntensity = dragonState.FireBreathIntensity,
                                LastMessage = dragonState.LastMessage
                            }),
                            System.Text.Encoding.UTF8,
                            "application/json")
                    };

                    // Add retry logic with circuit breaker pattern
                    int retries = 0;
                    const int maxRetries = 2;
                    TimeSpan delay = TimeSpan.FromMilliseconds(200);

                    while (true)
                    {
                        try
                        {
                            using var response = await client.SendAsync(request);

                            if (response.IsSuccessStatusCode)
                            {
                                var thoughtResponse = await response.Content.ReadFromJsonAsync<ThoughtResponse>();
                                if (thoughtResponse?.Thought != null)
                                {
                                    _logger.LogInformation("Generated AI dragon thought: {Thought}", thoughtResponse.Thought);
                                    _telemetryClient?.TrackEvent("DragonAIThought");
                                    return thoughtResponse.Thought;
                                }
                            }
                            else
                            {
                                _logger.LogWarning("AI thought generation failed with status: {Status}", response.StatusCode);
                                _telemetryClient?.TrackEvent("DragonAIThoughtFailed",
                                    new Dictionary<string, string> { { "StatusCode", response.StatusCode.ToString() } });
                            }

                            break; // Exit the retry loop if we get any response
                        }
                        catch (Exception ex) when (retries < maxRetries &&
                                                (ex is HttpRequestException || ex is TaskCanceledException))
                        {
                            retries++;
                            _logger.LogWarning(ex, "Error calling AI service (attempt {Retry}/{MaxRetries})", retries, maxRetries);
                            await Task.Delay(delay);
                            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 2); // Exponential backoff
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Unexpected error calling AI service");
                            break;
                        }
                    }
                }

                // Fall back to local generation if AI service is not available or fails
                return GenerateThought(dragonState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dragon thought from AI service");
                _telemetryClient?.TrackException(ex, new Dictionary<string, string> {
                    { "Operation", "GenerateThoughtAsync" },
                    { "PetType", "Dragon" }
                });

                // Fall back to local generation in case of error
                return GenerateThought(dragonState);
            }
        }

        private string DetermineMood(DragonState state)
        {
            if (state.IsBreathingFire)
                return "raging";
            if (state.RageLevel > 70)
                return "angry";
            if (state.Chaos > 70)
                return "chaotic";
            if (state.Happiness > 70)
                return "pleased";
            if (state.Energy < 30)
                return "tired";
            if (state.IsEvolved && state.HasWings)
                return "powerful";

            // Default dragon mood is unpredictable
            return "unpredictable";
        }

        private class ThoughtResponse
        {
            public string? Thought { get; set; }
        }
    }
}