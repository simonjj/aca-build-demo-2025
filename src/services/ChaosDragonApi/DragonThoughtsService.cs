using System.Text.Json;
using ChaosDragonApi.Models;
using Microsoft.ApplicationInsights;

namespace ChaosDragonApi.Services
{
    public class DragonThoughtsService
    {
        private readonly ILogger<DragonThoughtsService> _logger;

        private readonly string[] _chaoticThoughts = new[]
        {
            "I feel like destroying a village today.",
            "Why are these humans so tiny and annoying?",
            "That was the worst petting ever. Do it better!",
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
            ILogger<DragonThoughtsService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generates a thought for the dragon based on its current state
        /// following Azure Container Apps best practices for simplicity
        /// </summary>
        public string GenerateThought(DragonState dragonState)
        {
            try
            {
                string thought;

                // Generate different thoughts based on the dragon's state
                if (dragonState.IsEvolved && dragonState.HasWings)
                {
                    thought = _evolvedThoughts[_random.Next(_evolvedThoughts.Length)];
                }
                else if (dragonState.Chaos > 70)
                {
                    thought = _chaoticThoughts[_random.Next(_chaoticThoughts.Length)];
                }
                else
                {
                    thought = _calmThoughts[_random.Next(_calmThoughts.Length)];
                }

                return thought;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating dragon thought");
                return "...";
            }
        }

        /// <summary>
        /// Simplified async method, following Azure Container Apps best practices
        /// for consistent API patterns without unnecessary complexity
        /// </summary>
        public Task<string> GenerateThoughtAsync(DragonState dragonState)
        {
            // Simple wrapper for compatibility with async interfaces
            return Task.FromResult(GenerateThought(dragonState));
        }
    }
}