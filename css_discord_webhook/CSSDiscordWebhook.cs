using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Utils;
using css_discord_webhook.Discord;
using css_discord_webhook.Player;
using css_discord_webhook.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Utilities;

namespace css_discord_webhook;

public class WebhookServiceCollection : IPluginServiceCollection<CSSDiscordWebhook>
{
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<DiscordWebhook>();
        services.AddSingleton<PlayerMethods>();
        services.AddSingleton<PlayerEvents>();
        services.AddSingleton<PlayerCommands>();
    }
}

public class CSSDiscordWebhook(
    DiscordWebhook discordWebhook,
    PlayerMethods playerMethods,
    PlayerEvents playerEvents,
    PlayerCommands playerCommands) : BasePlugin
{
    public override string ModuleName => "CSS Discord Webhook";
    public override string ModuleVersion => "v0.1.0";

    private readonly DiscordWebhook _discordWebhook = discordWebhook;
    private readonly PlayerMethods _playerMethods = playerMethods;
    private readonly PlayerEvents _playerEvents = playerEvents;
    private readonly PlayerCommands _playerCommands = playerCommands;
    private GameState _gameState = GameState.Warmup;

    public override void Load(bool hotReload)
    {
        // _discordWebhook.OnConfigParsed(ConfigManager.Load<DiscordConfig>("CSSDiscordWebhook"));
        var config = ConfigManager.Load<DiscordConfig>("CSSDiscordWebhook");

        Logger.LogInformation("Discord Webhook URL: {Url}", config.WebhookUrl);
        Logger.LogInformation("Instance Name: {InstanceName}", config.InstanceName);

        _discordWebhook.OnConfigParsed(config);

        AddCommand("css_ready", "Marks the player as ready for the game.", _playerCommands.ReadyCommand);

        AddCommand("css_unready", "Marks the player as not ready for the game.", _playerCommands.UnreadyCommand);

        AddCommand("css_pause", "Pauses the game for the team.", _playerCommands.PauseCommand);

        AddCommand("css_unpause", "Unpauses the game.", _playerCommands.UnpauseCommand);

        AddCommand("css_admin", "Calls an admin through the Discord webhook.", _playerCommands.CallAdminCommand);

        AddCommand("css_help", "Prints an overview of all commands to the player.", _playerCommands.HelpCommand);
    }

    public override void Unload(bool hotReload) // maybe optional
    {
        RemoveCommand("css_ready", _playerCommands.ReadyCommand);

        RemoveCommand("css_unready", _playerCommands.UnreadyCommand);

        RemoveCommand("css_pause", _playerCommands.PauseCommand);

        RemoveCommand("css_unpause", _playerCommands.UnpauseCommand);

        RemoveCommand("css_admin", _playerCommands.CallAdminCommand);

        RemoveCommand("css_help", _playerCommands.HelpCommand);
    }

    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd warmupEnd, GameEventInfo info)
    {
        _gameState = GameState.Live;
        Server.PrintToChatAll("_gameState changed to Live.");
        Server.PrintToChatAll("_gameState changed to Live.");
        Server.PrintToChatAll("_gameState changed to Live.");

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnWarmupStart(EventRoundAnnounceWarmup warmupEnd, GameEventInfo info)
    {
        _gameState = GameState.Warmup;
        Server.PrintToChatAll("_gameState changed to Warmup.");
        Server.PrintToChatAll("_gameState changed to Warmup.");
        Server.PrintToChatAll("_gameState changed to Warmup.");

        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnRoundEnd(EventRoundEnd roundEnd, GameEventInfo info)
    {
        if (_discordWebhook == null)
        {
            Logger.LogError("Discord webhook is not initialized.");
            return HookResult.Continue;
        }

        var match = GetMatchMock();

        var message = $"Round ended. {match.Team1!.ClanTeamname}: {match.Team1.Score} | {match.Team2!.ClanTeamname}: {match.Team2.Score}";
        _discordWebhook.SendMessage(message);
        Server.PrintToChatAll("Message sent to Discord.");
        return HookResult.Continue;
    }


    [ConsoleCommand("test", "")]
    public void TestCommand(CCSPlayerController? player, CommandInfo command)
    {
        GetMatchMock();
        RenameTeams(team1Name: "Team A", team2Name: "Team B");
    }

    private static void RenameTeams(string? team1Name = null, string? team2Name = null)
    {
        if (!string.IsNullOrWhiteSpace(team1Name)) Server.ExecuteCommand($"mp_teamname_1 {team1Name}");
        if (!string.IsNullOrWhiteSpace(team2Name)) Server.ExecuteCommand($"mp_teamname_2 {team2Name}");
    }

    private MatchMock GetMatchMock()
    {
        MatchMock mock = new()
        {
            MapName = Server.MapName,
            GameState = _gameState
        };

        foreach (var entity in GetAllEntities())
        {
            if (entity.DesignerName == "cs_team_manager")
            {
                var team = GetEntityFromIndex<CCSTeam>((int)entity.Index);
                if (team!.TeamNum == (int)CsTeam.Terrorist)
                {
                    mock.Team1 = team;
                }
                else if (team.TeamNum == (int)CsTeam.CounterTerrorist)
                {
                    mock.Team2 = team;
                }
            }
        }

        if (mock.Team1 == null || mock.Team2 == null)
        {
            Logger.LogError("Couldn't find Teams in Entity List");
        }

        return mock;
    }
}