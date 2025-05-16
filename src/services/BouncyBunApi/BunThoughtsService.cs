using BouncyBunApi.Controllers;

public class BunThoughtsService
{
    private readonly ILogger<BunThoughtsService> _logger;

    public BunThoughtsService(ILogger<BunThoughtsService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GenerateThoughtAsync(BunState bunState)
    {
        // Simulate generating a thought based on the bun's state
        var thoughts = new List<string>
        {
            "I love to bounce!",
            "Is it snack time yet?",
            "I could use a nap.",
            "Let's go on an adventure!",
            "Bouncing is my favorite exercise!"
        };

        // Randomly select a thought
        var random = new Random();
        return await Task.FromResult(thoughts[random.Next(thoughts.Count)]);
    }
}