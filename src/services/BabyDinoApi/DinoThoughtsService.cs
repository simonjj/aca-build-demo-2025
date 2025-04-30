using System.Text.Json;
using BabyDinoApi.Models;

namespace BabyDinoApi.Services
{
    /// <summary>
    /// Service that generates thoughts for the Baby Dino based on its current state
    /// </summary>
    public class DinoThoughtsService
    {
        private readonly ILogger<DinoThoughtsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        private readonly string[] _playfulThoughts = new[]
        {
            "RAWR! That means 'I love you' in dinosaur!",
            "Can we play tag? I promise not to run too fast!",
            "Bounce bounce bounce! Weeee!",
            "I found a shiny rock! It's mine now!",
            "Let's be friends forever and ever!",
            "I'm the fastest baby dino in the whole wide world!"
        };

        private readonly string[] _hungryThoughts = new[]
        {
            "My tummy is making the rumblies...",
            "Food? Is it food time?",
            "I could eat a whole tree of leaves right now!",
            "Snacks please! Baby dino needs snacks!",
            "Hungryyy. Feed baby dino now please?"
        };

        private readonly string[] _sleepyThoughts = new[]
        {
            "*yawn* I'm getting sleepy...",
            "Just five more minutes of playtime, then nap...",
            "Is it nap time yet?",
            "My eyes are getting heavy... but I don't wanna miss anything fun!"
        };

        private readonly string[] _evolvedThoughts = new[]
        {
            "I'm getting bigger and stronger every day!",
            "Soon I'll be the biggest dinosaur ever!",
            "Look at all the tricks I can do now!",
            "I'm not just cute anymore - I'm awesome!",
            "My favorite leaf tastes different now that I'm bigger."
        };

        private readonly Random _random = new();

        public DinoThoughtsService(
            ILogger<DinoThoughtsService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a thought based on the baby dino's current state
        /// </summary>
        public string GenerateThought(DinoState dinoState)
        {
            try
            {
                string thought;

                // Determine what kind of thought to generate based on state
                if (dinoState.IsNapping)
                {
                    thought = $"*snores softly* zZz... {_sleepyThoughts[_random.Next(_sleepyThoughts.Length)]}";
                }
                else if (dinoState.Energy < 30)
                {
                    thought = _hungryThoughts[_random.Next(_hungryThoughts.Length)];
                }
                else if (dinoState.IsEvolved)
                {
                    thought = _evolvedThoughts[_random.Next(_evolvedThoughts.Length)];
                }
                else if (dinoState.PlayfulnessLevel > 70)
                {
                    thought = _playfulThoughts[_random.Next(_playfulThoughts.Length)];
                }
                else
                {
                    // Default thoughts combine playful and sleepy
                    var options = _playfulThoughts.Concat(_sleepyThoughts).ToArray();
                    thought = options[_random.Next(options.Length)];
                }

                // Personalize with recent message if available
                if (!string.IsNullOrEmpty(dinoState.LastMessage) && _random.Next(10) > 6)
                {
                    thought += $" (thinking about: \"{dinoState.LastMessage}\")";
                }

                // Add tricks reference if dino knows tricks
                if (dinoState.KnownTricks.Count > 0 && _random.Next(10) > 7)
                {
                    var trick = dinoState.KnownTricks[_random.Next(dinoState.KnownTricks.Count)];
                    thought += $" *does a {trick} trick*";
                }

                _logger.LogInformation("Generated baby dino thought: {Thought}", thought);
                return thought;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating baby dino thought");
                return "Rawr?"; // Safe fallback
            }
        }

        /// <summary>
        /// Asynchronously generates a thought with potential AI integration
        /// </summary>
        public async Task<string> GenerateThoughtAsync(DinoState dinoState)
        {
            try
            {
                // Check if we should use an external AI service
                string? aiServiceUrl = _configuration["AIServices:ThoughtGenerator"];

                if (!string.IsNullOrEmpty(aiServiceUrl))
                {
                    using var client = _httpClientFactory.CreateClient("ThoughtGenerator");
                    // Set timeout for resilience
                    client.Timeout = TimeSpan.FromSeconds(2);

                    try
                    {
                        var response = await client.PostAsJsonAsync(aiServiceUrl, new
                        {
                            PetType = "BabyDino",
                            Mood = DetermineMood(dinoState),
                            Growth = dinoState.Growth,
                            IsEvolved = dinoState.IsEvolved,
                            PlayfulnessLevel = dinoState.PlayfulnessLevel,
                            IsNapping = dinoState.IsNapping,
                            LastMessage = dinoState.LastMessage,
                            KnownTricks = dinoState.KnownTricks
                        });

                        if (response.IsSuccessStatusCode)
                        {
                            var thoughtResponse = await response.Content.ReadFromJsonAsync<ThoughtResponse>();
                            if (thoughtResponse?.Thought != null)
                            {
                                _logger.LogInformation("Generated AI baby dino thought: {Thought}", thoughtResponse.Thought);
                                return thoughtResponse.Thought;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
                    {
                        _logger.LogWarning(ex, "AI thought generation service unavailable, falling back to local generation");
                    }
                }

                // Fall back to local generation
                return GenerateThought(dinoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating baby dino thought from AI service");
                return GenerateThought(dinoState);
            }
        }

        /// <summary>
        /// Determines the baby dino's current mood based on its state
        /// </summary>
        private string DetermineMood(DinoState state)
        {
            if (state.IsNapping)
                return "sleepy";
            if (state.Energy < 30)
                return "hungry";
            if (state.Happiness > 80)
                return "ecstatic";
            if (state.Chaos > 70)
                return "mischievous";
            if (state.PlayfulnessLevel > 80)
                return "playful";
            if (state.IsEvolved)
                return "grown";

            // Default mood is happy and cute
            return "happy";
        }

        private class ThoughtResponse
        {
            public string? Thought { get; set; }
        }
    }
}