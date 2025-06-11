
namespace css_discord_webhook.Discord;

public class DiscordWebhook
{
    public required Uri WebhookUrl { get; set; }
    public required string InstanceName { get; set; }
    private readonly HttpClient _httpClient = new();

    public DiscordWebhook()
    {
        _httpClient.BaseAddress = WebhookUrl;
    }

    internal async Task SendMessageAsync(string message)
    {
        await _httpClient.PostAsync("", new StringContent(message));
    }
}