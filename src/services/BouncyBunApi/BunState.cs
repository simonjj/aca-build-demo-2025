public class BunState
{
    public int Happiness { get; set; } = 0;
    public int Energy { get; set; } = 0;
    public int Chaos { get; set; } = 0;
    public int Calmness { get; set; } = 0;
    public string Mood { get; set; } = "happy";
    public string LastMessage { get; set; } = string.Empty;
}