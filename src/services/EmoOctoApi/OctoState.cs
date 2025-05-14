namespace EmoOctoApi.Models
{
    /// <summary>
    /// Represents the state of an Emotional Octopus pet
    /// </summary>
    public class OctoState
    {
        public string Mood { get; set; } = "Content";
        // Core state properties across all pets
        public int Happiness { get; set; } = 50;
        public int Energy { get; set; } = 65;
        public int Chaos { get; set; } = 30;
        public bool IsEvolved { get; set; } = false;
        public string? Evolution { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        public DateTime? LastEvent { get; set; }
        // Azure best practice: Add ThrottleUntil property for rate limiting

        // EmoOcto specific properties
        public string CurrentMood { get; set; } = "Curious";
        public int EmotionalIntensity { get; set; } = 70; // Octopuses are emotional!
        public bool IsInking { get; set; } = false; // Defensive mechanism
        public int IntelligenceLevel { get; set; } = 85; // Octopuses are smart
        public Dictionary<string, int> MoodHistory { get; set; } = new Dictionary<string, int>
        {
            { "Happy", 10 },
            { "Sad", 5 },
            { "Curious", 15 },
            { "Nervous", 8 },
            { "Excited", 7 }
        };
        public int ColorChangeCount { get; set; } = 0;
        public string CurrentColor { get; set; } = "Blue";
        public bool IsCamouflaged { get; set; } = false;
        public List<string> CollectedItems { get; set; } = new List<string>();

        // Mood management methods
        public void UpdateMood(string mood)
        {
            Mood = mood;
        }

        // Color change method
        public void ChangeColor(string color)
        {
            if (CurrentColor != color)
            {
                CurrentColor = color;
                ColorChangeCount++;
            }
        }

        // Collect items method
        public void CollectItem(string item)
        {
            if (!string.IsNullOrEmpty(item) && !CollectedItems.Contains(item) && CollectedItems.Count < 10)
            {
                CollectedItems.Add(item);
                IntelligenceLevel = Math.Min(IntelligenceLevel + 2, 100);
            }
        }

        // Check if octopus should evolve
        public bool ShouldEvolve()
        {
            return !IsEvolved &&
                   IntelligenceLevel > 90 &&
                   ColorChangeCount > 15 &&
                   MoodHistory.Keys.Count > 8;
        }

        // Apply bounds to all values
        public void NormalizeValues()
        {
            Happiness = Math.Clamp(Happiness, 0, 100);
            Energy = Math.Clamp(Energy, 0, 100);
            Chaos = Math.Clamp(Chaos, 0, 100);
            EmotionalIntensity = Math.Clamp(EmotionalIntensity, 0, 100);
            IntelligenceLevel = Math.Clamp(IntelligenceLevel, 0, 100);
        }
    }

    /// <summary>
    /// Represents a request to interact with the Emotional Octopus
    /// </summary>
    public class InteractionRequest
    {
        /// <summary>
        /// The type of action to perform (pet, feed, poke, sing, message)
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Optional message to send to the octopus
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Optional identifier for the user performing the interaction
        /// </summary>
        public string? UserId { get; set; }
    }
}