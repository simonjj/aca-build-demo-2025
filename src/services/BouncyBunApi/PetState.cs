public class PetState
{
    public int Happiness { get; private set; } = 50;
    public int Energy { get; private set; } = 50;
    public int Chaos { get; private set; } = 0;

    public void Apply(string action)
    {
        switch (action.ToLowerInvariant())
        {
            case "pet": Happiness += 5; break;
            case "feed": Energy += 10; break;
            case "poke": Chaos += 15; break;
            case "sing": Chaos -= 10; break;
        }
        Clamp();
    }

    private void Clamp()
    {
        Happiness = Math.Clamp(Happiness, 0, 100);
        Energy = Math.Clamp(Energy, 0, 100);
        Chaos = Math.Clamp(Chaos, 0, 100);
    }

    public bool ShouldEvolve() => Happiness >= 90 && Energy >= 90;
}