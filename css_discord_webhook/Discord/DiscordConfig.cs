using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace css_discord_webhook.Discord;

public class DiscordConfig : BasePluginConfig
{
    [JsonPropertyName("webhook_url")]
    public string WebhookUrl { get; set; } = string.Empty;

    [JsonPropertyName("instance_name")]
    public string InstanceName { get; set; } = "base";
}