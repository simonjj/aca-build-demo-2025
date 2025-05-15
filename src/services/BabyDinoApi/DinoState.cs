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
        public string? Evolution { get; set; }
        public string? LastMessage { get; set; }
        public DateTime LastInteraction { get; set; } = DateTime.UtcNow;
        public DateTime? LastEvent { get; set; }

        // Baby Dino specific properties
        public int PlayfulnessLevel { get; set; } = 85;
        public int Growth { get; set; } = 10; // Growth level - starts small
        public bool IsNapping { get; set; } = false;
        public int CutenessFactor { get; set; } = 95; // Baby dinos are very cute!
        public string FavoriteFood { get; set; } = "Leaf";
        public int TricksLearned { get; set; } = 0;
        public List<string> KnownTricks { get; set; } = new();

        // Apply bounds to all values
        public void NormalizeValues()
        {
            Happiness = Math.Clamp(Happiness, 0, 100);
            Energy = Math.Clamp(Energy, 0, 100);
            Chaos = Math.Clamp(Chaos, 0, 100);
            PlayfulnessLevel = Math.Clamp(PlayfulnessLevel, 0, 100);
            Growth = Math.Clamp(Growth, 0, 100);
            CutenessFactor = Math.Clamp(CutenessFactor, 0, 100);
        }

        // Check if dino should evolve
        public bool ShouldEvolve()
        {
            return !IsEvolved &&
                   Growth > 70 &&
                   Energy > 60 &&
                   TricksLearned > 3;
        }

        // Learn a new trick if it doesn't know it already
        public bool LearnTrick(string trick)
        {
            if (string.IsNullOrEmpty(trick) || KnownTricks.Contains(trick))
                return false;

            KnownTricks.Add(trick);
            TricksLearned++;
            return true;
        }
    }
}