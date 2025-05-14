using System.Text.Json.Serialization;

namespace ChaosDragonApi.Models
{
    public class DragonState
    {
        public string Mood { get; set; } = "Unpredictable";
        // Core state properties
        public int Happiness { get; set; } = 30;
        public int Energy { get; set; } = 50;
        public int Chaos { get; set; } = 75; // Dragons start more chaotic
        public bool IsEvolved { get; set; } = false;
        public string? Evolution { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        public DateTime? LastEvent { get; set; }

        // Dragon specific properties
        public int FireBreathIntensity { get; set; } = 20;
        public bool HasWings { get; set; } = false;
        public int RageLevel { get; set; } = 40;
        public bool IsBreathingFire { get; set; } = false;
        public int HoardSize { get; set; } = 0;
        public int InteractionCount { get; set; } = 0;
        public string CurrentMood { get; set; } = "Unpredictable";
        public List<string> TreasureHoard { get; set; } = new List<string>();

        // Metrics for telemetry
        [JsonIgnore]
        public int ApiFailureCount { get; set; } = 0;

        [JsonIgnore]
        public int HighChaosEvents { get; set; } = 0;

        public void AddTreasure(string treasure)
        {
            if (!string.IsNullOrEmpty(treasure) && TreasureHoard.Count < 10)
            {
                TreasureHoard.Add(treasure);
                HoardSize++;
            }
        }

        // Evolution check
        public bool ShouldEvolve()
        {
            return !IsEvolved &&
                   Energy > 80 &&
                   Chaos > 70 &&
                   FireBreathIntensity > 50;
        }

        // Apply bounds to all values
        public void NormalizeValues()
        {
            Happiness = Math.Clamp(Happiness, 0, 100);
            Energy = Math.Clamp(Energy, 0, 100);
            Chaos = Math.Clamp(Chaos, 0, 100);
            FireBreathIntensity = Math.Clamp(FireBreathIntensity, 0, 100);
            RageLevel = Math.Clamp(RageLevel, 0, 100);
        }
    }

    public class InteractionRequest
    {
        public string Action { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? UserId { get; set; }
    }

    public class GlobalEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
    }
}