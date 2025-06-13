using CounterStrikeSharp.API;
using static CounterStrikeSharp.API.Utilities;
using CounterStrikeSharp.API.Modules.Extensions;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using CSSDiscordWebhook.Discord;
using CSSDiscordWebhook.Util;
using static CSSDiscordWebhook.Util.ChatColors;

namespace CSSDiscordWebhook.Player;

public class PlayerMethods(DiscordWebhook discordWebhook)
{
    private readonly Dictionary<CsTeam, HashSet<CCSPlayerController>> playerReadyStatus = new()
    {
        {CsTeam.Terrorist, []},
        {CsTeam.CounterTerrorist, []}
    };
    private CsTeam? pausedTeam = null;
    public GameState GameState { get; set; } = GameState.Warmup;
    private readonly DiscordWebhook _discord = discordWebhook;
    public ITimerService? Timer { get; set; }

    public void ReadyPlayer(CCSPlayerController player)
    {
        if (GameState == GameState.Live) return;

        var team = player.Team;

        if (!playerReadyStatus.TryGetValue(team, out var set)) return;

        if (set.Add(player))
        {
            PrintPlayerReadyStatus();
            if (AllPlayersReady()) StartGame();
        }
    }

    public void UnreadyPlayer(CCSPlayerController player)
    {
        if (GameState == GameState.Live) return;
        var team = player.Team;

        if (!playerReadyStatus.TryGetValue(team, out HashSet<CCSPlayerController>? set)) return;

        if (set.Remove(player)) PrintPlayerReadyStatus();
    }

    public void PauseGame(CsTeam team)
    {
        if (pausedTeam.HasValue || GameState == GameState.Warmup) return;

        pausedTeam = team;

        var mock = GetMatchMock();
        var teamName = mock.Team1?.TeamNum == (int)team ? mock.Team1.ClanTeamname : mock.Team2?.ClanTeamname;

        Server.PrintToChatAll(red.ToColoredChat($"Game paused for team: {(string.IsNullOrWhiteSpace(teamName) ? team : teamName)}. Use !unpause to resume."));

        Server.ExecuteCommand("mp_pause_match");
    }

    public void UnpauseGame(CsTeam team)
    {
        if (!pausedTeam.HasValue || pausedTeam.Value != team || GameState == GameState.Warmup) return;

        pausedTeam = null;

        Server.PrintToChatAll(green.ToColoredChat("Game unpaused. Continuing match..."));

        Server.ExecuteCommand("mp_unpause_match");

    }

    public void CallAdmin(CCSPlayerController player, string message)
    {
        _discord.SendMessage(
            $"Admin call from {player.PlayerName}: {(!string.IsNullOrWhiteSpace(message) ? message : "No message provided")}",
            0xFF0000
        );
    }

    private void PrintPlayerReadyStatus()
    {
        Server.PrintToChatAll(blue.ToColoredChat(
            $"Ready CT: {playerReadyStatus[CsTeam.CounterTerrorist].Count}/5 | Ready T: {playerReadyStatus[CsTeam.Terrorist].Count}/5"
        ));
    }

    private bool AllPlayersReady()
    {
        // return playerReadyStatus[CsTeam.Terrorist].Count == 1;
        return GetPlayers().Count == 10 && playerReadyStatus.All(kvp => kvp.Value.Count == 5);
    }

    private void StartGame()
    {
        Countdown(3, ExecGameStart);
    }

    private void Countdown(int secondsRemaining, Action callback)
    {
        if (secondsRemaining > 0)
        {
            if (!AllPlayersReady()) return;
            Server.PrintToChatAll($"Game starts in {secondsRemaining}...");
            Timer!.AddTimer(1, () => Countdown(secondsRemaining - 1, callback));
        }
        else
        {
            if (!AllPlayersReady()) return;
            callback.Invoke();
        }
    }

    private void ExecGameStart()
    {
        GameState = GameState.Live;
        Server.PrintToChatAll(green.ToColoredChat("GameLive - GL HF!!!"));
        Server.ExecuteCommand("exec gamestart.cfg");
    }

    internal void ResetReadyStatus()
    {
        playerReadyStatus[CsTeam.Terrorist] = [];
        playerReadyStatus[CsTeam.CounterTerrorist] = [];
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
        return mock;
    }
}