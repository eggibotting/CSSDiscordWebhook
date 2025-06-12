using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;

namespace css_discord_webhook.Discord;

public class DiscordConfig : BasePluginConfig
{
    [JsonPropertyName("WebhookUrl")]
    public string WebhookUrl { get; set; } = string.Empty;

    [JsonPropertyName("InstanceName")]
    public string InstanceName { get; set; } = "base";
}