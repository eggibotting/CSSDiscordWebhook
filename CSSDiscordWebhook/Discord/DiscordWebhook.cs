
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSSDiscordWebhook.Discord;

public class DiscordWebhook(ILogger<DiscordWebhook> logger) : IPluginConfig<DiscordConfig>
{
    private readonly HttpClient _httpClient = new();
    public required DiscordConfig Config { get; set; }
    private readonly ILogger<DiscordWebhook> _logger = logger;

    public void SendMessage(string message, int color = 0x00FF00)
    {
        Server.NextFrame(async () =>
        {
            try
            {
                await SendMessageAsync(message, color);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error sending Discord message: {ex.Message}");
                return;
            }
        });
    }

    public async Task SendMessageAsync(string message, int color = 0x00FF00)
    {
        _logger.LogInformation($"Sending message to Discord: {message}");
        var messageObj = new
        {
            content = "",
            tts = false,
            embeds = new[]
                {
                    new
                    {
                        type = "rich",
                        title = $"Notification from {Config.InstanceName}",
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
            var response = await _httpClient.PostAsync(Config.WebhookUrl, json);
            response.EnsureSuccessStatusCode();
            _logger.LogInformation("Message sent to Discord successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending message to Discord: {ex.Message}");
            throw;
        }
    }

    public void OnConfigParsed(DiscordConfig config)
    {
        if (config == null)
        {
            _logger.LogError("DiscordConfig is null. Cannot parse configuration.");
            return;
        }
        _logger.LogInformation("Config parsed for Discord webhook.");
        if (string.IsNullOrWhiteSpace(config.WebhookUrl) || string.IsNullOrEmpty(config.InstanceName))
        {
            _logger.LogError("Discord Webhook Configuration is missing fields.");
            return;
        }

        Config = config;
    }
}