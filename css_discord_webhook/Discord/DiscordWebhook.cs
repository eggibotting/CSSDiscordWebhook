
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.Logging;

namespace css_discord_webhook.Discord;

public class DiscordWebhook
{
    public required Uri WebhookUrl { get; set; }
    private readonly HttpClient _httpClient = new();
    public required CSSDiscordWebhook Instance { get; set; }

    public DiscordWebhook() { }

    public async Task SendMessageAsync(string message, int color = 0x00FF00)
    {
        Instance.Logger.LogInformation($"Sending message to Discord: {message}");
        var messageObj = new
        {
            content = "",
            tts = false,
            embeds = new[]
                {
                    new
                    {
                        type = "rich",
                        title = $"Notification from {Instance.Config.InstanceName}",
                        description = message,
                        color,
                    }
                }
        };

        var json = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(messageObj),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        try
        {
            var response = await _httpClient.PostAsync(WebhookUrl, json);
            response.EnsureSuccessStatusCode();
            Instance.Logger.LogInformation("Message sent to Discord successfully.");
        }
        catch (Exception ex)
        {
            Instance.Logger.LogError($"Error sending message to Discord: {ex.Message}");
        }
    }
}