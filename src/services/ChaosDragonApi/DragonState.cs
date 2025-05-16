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

        // Dragon specific properties
        public int FireBreathIntensity { get; set; } = 20;
        public bool HasWings { get; set; } = false;
        public int RageLevel { get; set; } = 40;
        public bool IsBreathingFire { get; set; } = false;
        public int HoardSize { get; set; } = 0;

        // Metrics for telemetry
        [JsonIgnore]
        public int ApiFailureCount { get; set; } = 0;

        [JsonIgnore]
        public int HighChaosEvents { get; set; } = 0;
    }

    public class InteractionRequest
    {
        public string Action { get; set; } = string.Empty;
    }
}