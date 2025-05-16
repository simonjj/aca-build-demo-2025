namespace ChillTurtleApi.Models
{
    public class TurtleState
    {
        public string Mood { get; set; } = "Calm";
        public int Happiness { get; set; } = 60;
        public int Energy { get; set; } = 30;
        public int Chaos { get; set; } = 5;
        public DateTime? LastMeal { get; set; }
        // Turtle specific properties
        public int StressLevel { get; set; } = 10;
        public bool IsInShell { get; set; } = false;
        public bool IsOverwhelmed { get; set; } = false;
    }
}