namespace BabyDinoApi.Models
{
    /// <summary>
    /// Represents the state of a Baby Dino pet
    /// </summary>
    public class DinoState
    {
        public string Mood { get; set; } = "Playful";
        // Core state properties across all pets
        public int Happiness { get; set; } = 60;
        public int Energy { get; set; } = 70;
        public int Chaos { get; set; } = 40;
        public bool IsEvolved { get; set; } = false;
        public string? LastMessage { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;

        // Baby Dino specific properties
        public int PlayfulnessLevel { get; set; } = 85;
        public int Growth { get; set; } = 10; // Growth level - starts small
        public bool IsNapping { get; set; } = false;
        public int TricksLearned { get; set; } = 0;
        public List<string> KnownTricks { get; set; } = new();
    }
}