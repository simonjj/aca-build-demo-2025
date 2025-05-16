using ChillTurtleApi.Models;
using Microsoft.ApplicationInsights;

namespace ChillTurtleApi.Services
{
    public class TurtleThoughtsService
    {
        private readonly ILogger<TurtleThoughtsService> _logger;
        private readonly string[] _chillThoughts = new[]
        {
            "Just enjoying the sun...",
            "Is it time for lettuce yet?",
            "Slow and steady wins the race...",
            "I've seen 47 sunsets today. Or was it the same one?",
            "The shell is half full, not half empty."
        };

        private readonly Random _random = new();

        public TurtleThoughtsService(ILogger<TurtleThoughtsService> logger)
        {
            _logger = logger;
        }

        public string GenerateThoughts(TurtleState turtleState)
        {
            return _chillThoughts[_random.Next(_chillThoughts.Length)];
        }
    }
}