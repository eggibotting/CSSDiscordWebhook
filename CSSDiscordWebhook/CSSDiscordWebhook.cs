using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Config;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using CSSDiscordWebhook.Discord;
using CSSDiscordWebhook.Player;
using CSSDiscordWebhook.Util;
using static CSSDiscordWebhook.Util.ChatColors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using static CounterStrikeSharp.API.Utilities;

namespace CSSDiscordWebhook;

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
    PlayerCommands playerCommands) : BasePlugin, ITimerService
{
    public override string ModuleName => "CSS Discord Webhook";
    public override string ModuleVersion => "v0.1.0";

    private readonly DiscordWebhook _discordWebhook = discordWebhook;
    private readonly PlayerMethods _playerMethods = playerMethods;
    private readonly PlayerCommands _playerCommands = playerCommands;
    private GameState _gameState = GameState.Warmup;
    private bool warmupHelp = false;

    public override void Load(bool hotReload)
    {
        _playerMethods.Timer = this;
        var config = ConfigManager.Load<DiscordConfig>("CSSDiscordWebhook");

        Logger.LogInformation("Discord Webhook URL: {Url}", config.WebhookUrl);
        Logger.LogInformation("Instance Name: {InstanceName}", config.InstanceName);

        _discordWebhook.OnConfigParsed(config);

        AddCommand("css_admin", "Calls an admin through the Discord webhook.", _playerCommands.CallAdminCommand);
        AddCommand("css_help", "Prints an overview of all commands to the player.", _playerCommands.HelpCommand);

        RegisterListener<Listeners.OnMapStart>(map =>
        {
            SetGameStateWarmup();
        });
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

    [ConsoleCommand("set_gamestate", "Sets the current gamestate")]
    [CommandHelper(minArgs: 1, usage: "[live|warmup]", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void SetGameState(CCSPlayerController? player, CommandInfo command)
    {
        var gameState = command.GetArg(1).ToLowerInvariant();
        switch (gameState)
        {
            case "live": SetGameStateLive(); return;
            case "warmup": SetGameStateWarmup(); return;
            default:
                Logger.LogError("Invalid game state. Use 'live' or 'warmup'.");
                return;
        }
    }

    [GameEventHandler]
    public HookResult OnFirstPlayerSpawned(EventPlayerConnectFull connect, GameEventInfo info)
    {
        if (_gameState == GameState.Warmup)
        {
            SetGameStateWarmup();            
        }
        return HookResult.Continue;
    }

    private void SetGameStateLive()
    {
        RemoveCommand("css_ready", _playerCommands.ReadyCommand);
        RemoveCommand("css_unready", _playerCommands.UnreadyCommand);
        AddCommand("css_pause", "Pauses the game for the team.", _playerCommands.PauseCommand);
        AddCommand("css_unpause", "Unpauses the game.", _playerCommands.UnpauseCommand);

        Server.ExecuteCommand("exec gamestart.cfg");

        _gameState = GameState.Live;
        warmupHelp = false;

        Logger.LogInformation("Game state set to Live.");
    }

    private void SetGameStateWarmup()
    {
        // Important: As this is just to set the plugins gamestate, we do not execute any config here.
        AddCommand("css_ready", "Marks the player as ready for the game.", _playerCommands.ReadyCommand);
        AddCommand("css_unready", "Marks the player as not ready for the game.", _playerCommands.UnreadyCommand);
        RemoveCommand("css_pause", _playerCommands.PauseCommand);
        RemoveCommand("css_unpause", _playerCommands.UnpauseCommand);

        Server.ExecuteCommand("exec warmup.cfg");

        _gameState = GameState.Warmup;

        Logger.LogInformation("Game state set to Warmup.");


        if (!warmupHelp)
        {
            warmupHelp = true;
            PrintWarmupHelp();
        }
    }

    private void PrintWarmupHelp()
    {
        base.AddTimer(15, () =>
        {
            if (_gameState != GameState.Warmup) return;
            Server.PrintToChatAll(blue.ToColoredChat("[Warmup]") + white.ToColoredChat(" Use !ready to mark yourself as ready. Use !help for a list of commands."));
            PrintWarmupHelp();
        });
    }

    [GameEventHandler]
    public HookResult OnMatchStart(EventBeginNewMatch matchStart, GameEventInfo info)
    {
        SetGameStateLive();

        if (_discordWebhook == null)
        {
            Logger.LogError("Discord webhook is not initialized.");
            return HookResult.Continue;
        }

        var names = GetTeamNames();

        _discordWebhook.SendMessage($"Match started on map {Server.MapName} with teams: {names.Item1} vs {names.Item2}", 0xFFFFFF);
        return HookResult.Continue;
    }

    [ConsoleCommand("teamnames", "Sets the team names for Team 1 and Team 2.")]
    [CommandHelper(minArgs: 2, usage: "<team1name> <team2name>", whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void SetTeamNames(CCSPlayerController? player, CommandInfo command)
    {
        var team1Name = command.GetArg(1);
        var team2Name = command.GetArg(2);

        RenameTeams(team1Name, team2Name);
        Logger.LogInformation($"Team names set: {team1Name} : {team2Name}");
    }

    [ConsoleCommand("reload_webhook", "Reloads the Discord webhook configuration.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void ReloadWebhook(CCSPlayerController? player, CommandInfo command)
    {
        var config = ConfigManager.Load<DiscordConfig>("CSSDiscordWebhook");
        _discordWebhook.OnConfigParsed(config);
        Logger.LogInformation("Discord webhook configuration reloaded.");
        Logger.LogInformation($"Discord Webhook URL: {config.WebhookUrl}");
        Logger.LogInformation($"Instance Name: {config.InstanceName}");
    }

    [ConsoleCommand("info_team1", "Prints a list with all Players on Team 1 and the current Teamname.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void PrintTeam1(CCSPlayerController? player, CommandInfo command)
    {
        var mock = GetMatchMock();

        var team1Name = string.IsNullOrEmpty(mock.Team1!.ClanTeamname) ? Enum.GetName(typeof(CsTeam), mock.Team1.TeamNum) : mock.Team1!.ClanTeamname;
        Logger.LogInformation($"Team 1: {team1Name} (Score: {mock.Team1.Score})");

        foreach (var playerController in mock.Team1!.PlayerControllers.Select(p => p.Value))
        {
            if (playerController != null)
            {
                Logger.LogInformation($" L {playerController.PlayerName} (ID: {playerController.SteamID})");
            }
        }
    }

    [ConsoleCommand("info_team2", "Prints a list with all Players on Team 2 and the current Teamname.")]
    [CommandHelper(whoCanExecute: CommandUsage.SERVER_ONLY)]
    public void PrintPlayersTeam2(CCSPlayerController? player, CommandInfo command)
    {
        var mock = GetMatchMock();

        var team2Name = string.IsNullOrEmpty(mock.Team2!.ClanTeamname) ? Enum.GetName(typeof(CsTeam), mock.Team2.TeamNum) : mock.Team2!.ClanTeamname;
        Logger.LogInformation($"Team 1: {team2Name} (Score: {mock.Team2.Score})");

        foreach (var playerController in mock.Team2!.PlayerControllers.Select(p => p.Value))
        {
            if (playerController != null)
            {
                Logger.LogInformation($" L {playerController.PlayerName} (ID: {playerController.SteamID})");
            }
        }
    }

    /// <summary>
    /// Method for testing when Game Events trigger.
    /// </summary>
    private void SendTestMessage(string eventName)
    {
        if (_discordWebhook == null)
        {
            Logger.LogError("Discord webhook is not initialized.");
            return;
        }

        _discordWebhook.SendMessage($"Event {eventName} triggered.", 0xFFFFFF);
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
        var names = GetTeamNames(match);
        if (match.Team1!.Score < 13 && match.Team2!.Score < 13)
        {
            // Match ongoing
        }
        else if (match.Team1.Score == 13 && match.Team2!.Score < 13)
        {
            // Team 1 wins
            var message = $"Team {names.Item1} won the Game: [{match.Team1.Score}:{match.Team2.Score}] against Team {names.Item2}";
            _discordWebhook.SendMessage(message, 0x00FF00);
        }
        else if (match.Team2!.Score == 13 && match.Team1.Score < 13)
        {
            // Team 2 wins
            var message = $"Team {names.Item2} won the Game: [{match.Team2.Score}:{match.Team1.Score}] against Team {names.Item1}";
            _discordWebhook.SendMessage(message, 0x00FF00);
        }
        else if (match.Team1.Score == 16 && match.Team2.Score < 15)
        {
            // Team 1 wins in Overtime
            var message = $"Team {names.Item1} won the Game: [{match.Team1.Score}:{match.Team2.Score}] against Team {names.Item2}";
            _discordWebhook.SendMessage(message, 0x00FF00);
        }
        else if (match.Team2.Score == 16 && match.Team1.Score < 15)
        {
            // Team 2 wins in Overtime
            var message = $"Team {names.Item2} won the Game: [{match.Team2.Score}:{match.Team1.Score}] against Team {names.Item1}";
            _discordWebhook.SendMessage(message, 0x00FF00);
        }
        else if (match.Team1.Score == 15 || match.Team2.Score == 15)
        {
            // Draw
            var message = $"The Game ended in a draw: {names.Item1} [{match.Team1.Score}:{match.Team2.Score}] {names.Item2}";
            _discordWebhook.SendMessage(message, 0x00FF00);
        }
        return HookResult.Continue;
    }

    private static void RenameTeams(string? team1Name = null, string? team2Name = null)
    {
        if (!string.IsNullOrWhiteSpace(team1Name)) Server.ExecuteCommand($"mp_teamname_1 {team1Name}");
        if (!string.IsNullOrWhiteSpace(team2Name)) Server.ExecuteCommand($"mp_teamname_2 {team2Name}");
    }

    private MatchMock GetMatchMock()
    {
        MatchMock mock = new();

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

    /// <summary>
    /// Used to provide Timers in other classes without creating circular injection problems.
    /// <para />
    /// Provides the default <see cref="BasePlugin.Timer"/> implementation.
    /// </summary>
    public void AddTimer(float interval, Action callback)
    {
        base.AddTimer(interval, callback);
    }

    /// <returns>Tuple[TeamName1, TeamName2]</returns>
    private Tuple<string, string> GetTeamNames(MatchMock? providedMock = null)
    {
        var mock = providedMock ?? GetMatchMock();

        var team1Name = string.IsNullOrEmpty(mock.Team1!.ClanTeamname) ? Enum.GetName(typeof(CsTeam), mock.Team1.TeamNum) : mock.Team1!.ClanTeamname;
        var team2Name = string.IsNullOrEmpty(mock.Team2!.ClanTeamname) ? Enum.GetName(typeof(CsTeam), mock.Team2.TeamNum) : mock.Team2!.ClanTeamname;

        return new Tuple<string, string>(team1Name!, team2Name!);
    }
}