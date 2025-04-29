public class BunThoughtsService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly ILogger<BunThoughtsService> _logger;

    public BunThoughtsService(IHttpClientFactory clientFactory, ILogger<BunThoughtsService> logger)
    {
        _clientFactory = clientFactory;
        _logger = logger;
    }

    public async Task TriggerEvolution()
    {
        var client = _clientFactory.CreateClient();
        _logger.LogInformation("BouncyBun evolving to MegaBun! Broadcasting...");

        var body = JsonContent.Create(new { creature = "bunny", stage = "MegaBun" });
        await client.PostAsync("http://localhost:3500/v1.0/publish/pubsub/evolution", body);
    }
}