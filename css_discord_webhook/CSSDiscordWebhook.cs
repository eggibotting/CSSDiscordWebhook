using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using css_discord_webhook.Discord;
using Microsoft.Extensions.Logging;

namespace css_discord_webhook;

public class CSSDiscordWebhook : BasePlugin, IPluginConfig<DiscordConfig>
{
    public override string ModuleName => "CSS Discord Webhook";
    public override string ModuleVersion => "v0.1.0";
    public required DiscordConfig Config { get; set; }

    private DiscordWebhook? _discordWebhook = null;
    private Dictionary<int, int> _teamScores = [];

    public override void Load(bool hotReload) { }

    [GameEventHandler]
    public HookResult OnTeamScore(EventTeamScore score, GameEventInfo info)
    {
        _teamScores[score.Teamid] = score.Score;
        Server.PrintToChatAll($"Team {score.Teamid} scored! Current score: {score.Score}"); // TODO: Debug, remove later

        if (_discordWebhook == null)
        {
            Logger.LogError("Discord webhook is not initialized.");
            return HookResult.Continue;
        }

        var message = CurrentScoreToString();

        Server.PrintToChatAll(message); // TODO: Debug, remove later

        Server.NextFrame(() =>
        {
            _ = _discordWebhook.SendMessageAsync(message);
        });

        return HookResult.Continue;
    }

    private string CurrentScoreToString()
    {
        return "Current Score: " + string.Join(" : ", _teamScores.Select(kv => $"Team {kv.Key} - {kv.Value}"));
    }

    [ConsoleCommand("admin", "Calls an admin through the Discord webhook.")]
    public void CallAdminCommand(CCSPlayerController? player, CommandInfo command)
    {
        if (_discordWebhook == null)
        {
            Logger.LogError("Discord webhook is not initialized.");
            return;
        }

        if (player == null)
        {
            Logger.LogWarning("CallAdminCommand was called without a player context.");
            return;
        }

        var callMessage = command.ArgString;
        var message = $"Admin call by {player.PlayerName} (SteamID: {player.SteamID}): {(string.IsNullOrWhiteSpace(callMessage) ? "No message provided." : callMessage)}";
        command.ReplyToCommand("Message sent to Admin.");
        Server.NextFrame(async () =>
        {
            try
            {
                await _discordWebhook.SendMessageAsync(message);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error sending Discord message: {ex.Message}");
                return;
            }
        });
    }

    public void OnConfigParsed(DiscordConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.WebhookUrl))
        {
            Logger.LogError("Discord webhook URL is not set in the configuration.");
            return;
        }

        Config = config;

        _discordWebhook = new()
        {
            WebhookUrl = new Uri(config.WebhookUrl),
            Instance = this
        };
    }
}