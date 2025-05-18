using EmoOctoApi.Models;
using System.Text.Json;

namespace EmoOctoApi.Services
{
    /// <summary>
    /// Service that generates thoughts for the Emotional Octopus based on its current state
    /// </summary>
    public class OctoThoughtsService
    {
        private readonly ILogger<OctoThoughtsService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        // Mood-based thought collections
        private readonly Dictionary<string, string[]> _moodThoughts = new()
        {
            ["Happy"] = new[]
            {
                "Today is the best day under the sea!",
                "All eight arms waving with joy!",
                "I feel like dancing on the ocean floor!",
                "Everything is so colorful and bright today!",
                "I could hug everyone with all my arms at once!"
            },
            ["Sad"] = new[]
            {
                "The ocean feels so vast and lonely sometimes...",
                "*sighs with all eight arms*",
                "I miss my coral reef friends...",
                "The water feels extra cold today...",
                "Sometimes I just want to hide in my little cave..."
            },
            ["Curious"] = new[]
            {
                "I wonder what's in that shiny jar?",
                "So many interesting things to investigate today!",
                "What would happen if I tried opening this with three arms at once?",
                "The humans are fascinating creatures...",
                "I want to learn everything about everything!"
            },
            ["Nervous"] = new[]
            {
                "Is that a predator? Should I hide?",
                "My suckers are tingling... something's not right.",
                "Maybe I should camouflage just to be safe...",
                "I feel like I'm being watched...",
                "My ink sac is getting twitchy..."
            },
            ["Excited"] = new[]
            {
                "Oh! Oh! Oh! Something amazing is happening!",
                "I can barely contain all this energy in eight arms!",
                "Look at that! And that! And THAT!",
                "This is the most thrilling moment of my entire octopus life!",
                "I'm changing colors so fast, I must look like a disco ball!"
            },
            ["Thoughtful"] = new[]
            {
                "The mysteries of the deep are endless...",
                "If I had three brains instead of nine, would I think differently?",
                "There's a pattern to how the currents move...",
                "I've solved this puzzle box 27 different ways now.",
                "The relationship between water pressure and arm flexibility is fascinating..."
            }
        };

        private readonly string[] _camouflageThoughts = new[]
        {
            "You can't see me... I'm just part of the background now...",
            "Blending in with my surroundings... perfect disguise...",
            "My cells are changing color to match what's around me...",
            "This is my special talent - disappearing in plain sight!",
            "I can become almost invisible when I need to..."
        };

        private readonly string[] _inkingThoughts = new[]
        {
            "INK CLOUD DEFENSE ACTIVATED! RETREAT!",
            "Can't see me through all this ink! Making my escape!",
            "That was too stressful! Ink and swim away!",
            "Needed an emergency exit strategy! Ink deployed!",
            "When in doubt, release the ink and scoot out!"
        };

        private readonly string[] _evolvedThoughts = new[]
        {
            "My mind has expanded beyond the confines of the ocean...",
            "I've mastered the art of emotional intelligence across species...",
            "With great cognition comes great emotional depth...",
            "I can now process feelings in ways no other octopus can...",
            "The emotional spectrum is my playground now."
        };

        private readonly Random _random = new();

        public OctoThoughtsService(
            ILogger<OctoThoughtsService> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Generates a thought based on the octopus's current state
        /// </summary>
        public string GenerateThought(OctoState octoState)
        {
            try
            {
                string thought;

                // Determine the appropriate type of thought based on state
                if (octoState.IsInking)
                {
                    thought = _inkingThoughts[_random.Next(_inkingThoughts.Length)];
                }
                else if (octoState.IsCamouflaged)
                {
                    thought = _camouflageThoughts[_random.Next(_camouflageThoughts.Length)];
                }
                else if (octoState.IsEvolved)
                {
                    thought = _evolvedThoughts[_random.Next(_evolvedThoughts.Length)];
                }
                else if (_moodThoughts.TryGetValue(octoState.CurrentMood, out var moodSpecificThoughts))
                {
                    thought = moodSpecificThoughts[_random.Next(moodSpecificThoughts.Length)];
                }
                else
                {
                    // Default to curious thoughts if current mood doesn't have specific thoughts
                    thought = _moodThoughts["Curious"][_random.Next(_moodThoughts["Curious"].Length)];
                }

                // Add color reference based on current color
                if (!octoState.IsInking && !octoState.IsCamouflaged && _random.Next(10) > 6)
                {
                    thought += $" *skin shifts to a bright {octoState.CurrentColor}*";
                }

                // Reference collected items
                if (octoState.CollectedItems.Count > 0 && _random.Next(10) > 7)
                {
                    var item = octoState.CollectedItems[_random.Next(octoState.CollectedItems.Count)];
                    thought += $" *glances at my precious {item}*";
                }

                return thought;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating octopus thought");
                return "..."; // Safe fallback
            }
        }

        /// <summary>
        /// Asynchronously generates a thought with potential AI integration
        /// </summary>
        public async Task<string> GenerateThoughtAsync(OctoState octoState)
        {
            try
            {
                // Check if we should use an external AI service
                string? aiServiceUrl = _configuration["AIServices:ThoughtGenerator"];

                if (!string.IsNullOrEmpty(aiServiceUrl))
                {
                    using var client = _httpClientFactory.CreateClient("resilient");

                    try
                    {
                        var response = await client.PostAsJsonAsync(aiServiceUrl, new
                        {
                            PetType = "Octopus",
                            Mood = octoState.CurrentMood,
                            Color = octoState.CurrentColor,
                            IsEvolved = octoState.IsEvolved,
                            IsCamouflaged = octoState.IsCamouflaged,
                            IsInking = octoState.IsInking,
                            EmotionalIntensity = octoState.EmotionalIntensity,
                            IntelligenceLevel = octoState.IntelligenceLevel,
                            CollectedItems = octoState.CollectedItems
                        });

                        if (response.IsSuccessStatusCode)
                        {
                            var thoughtResponse = await response.Content.ReadFromJsonAsync<ThoughtResponse>();
                            if (thoughtResponse?.Thought != null)
                            {
                                return thoughtResponse.Thought;
                            }
                        }
                        else
                        {
                    
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error calling AI thought service, falling back to local generation");
                    }
                }

                // Fall back to local generation if AI service is not available or fails
                return GenerateThought(octoState);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating octopus thought");

                // Safe fallback - generate a simple thought locally
                return GenerateThought(octoState);
            }
        }

        private class ThoughtResponse
        {
            public string? Thought { get; set; }
        }
    }
}