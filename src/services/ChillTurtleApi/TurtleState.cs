namespace ChillTurtleApi.Models
{
    public class TurtleState
    {
        public int Happiness { get; set; } = 60;
        public int Energy { get; set; } = 30;
        public int Chaos { get; set; } = 5;
        public bool IsEvolved { get; set; } = false;
        public string? Evolution { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        public DateTime? LastEvent { get; set; }
        public DateTime? LastMeal { get; set; }

        // Turtle specific properties
        public int StressLevel { get; set; } = 10;
        public bool IsInShell { get; set; } = false;
        public int Age { get; set; } = 15;
        public bool IsOverwhelmed { get; set; } = false;
        public int InteractionCount { get; set; } = 0;
        public string CurrentMood { get; set; } = "Chill";
    }
}