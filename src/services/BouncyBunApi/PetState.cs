public class PetState
{
    public int Happiness { get; private set; } = 50;
    public int Energy { get; private set; } = 50;
    public int Chaos { get; private set; } = 0;

    public string Mood => Happiness > 70 ? "Happy" : Energy < 30 ? "Tired" : Chaos > 50 ? "Chaotic" : "Neutral";

}