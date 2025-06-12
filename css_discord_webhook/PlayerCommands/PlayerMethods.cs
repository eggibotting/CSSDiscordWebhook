using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using css_discord_webhook.Discord;
using css_discord_webhook.Util;

namespace css_discord_webhook.Player;

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

        if (!playerReadyStatus.TryGetValue(team, out HashSet<CCSPlayerController>? set)) return;

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

        Server.PrintToChatAll($"Game paused for team: {team}. Use !unpause to resume.");

        Server.ExecuteCommand("mp_pause_match");
    }

    public void UnpauseGame(CsTeam team)
    {
        if (!pausedTeam.HasValue || pausedTeam.Value != team || GameState == GameState.Warmup) return;

        pausedTeam = null;

        Server.PrintToChatAll("Game unpaused. Coninuing match...");

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
        Server.PrintToChatAll(
            $"Ready CT: {playerReadyStatus[CsTeam.CounterTerrorist].Count}/5 | Ready T: {playerReadyStatus[CsTeam.Terrorist].Count}/5"
        );
    }

    private bool AllPlayersReady()
    {
        return playerReadyStatus[CsTeam.Terrorist].Count == 1;
        //return GetPlayers().Count == 10 && playerReadyStatus.All(kvp => kvp.Value.Count == 5);
    }

    private void StartGame()
    {
        Countdown(3, ExecGameStart);
    }

    private void Countdown(int secondsRemaining, Action callback)
    {
        if (secondsRemaining > 0)
        {
            Server.PrintToChatAll($"Game starts in {secondsRemaining}...");
            Timer!.AddTimer(1, () => Countdown(secondsRemaining - 1, callback));
        }
        else
        {
            callback.Invoke();
        }
    }

    private void ExecGameStart()
    {
        GameState = GameState.Live;
        Server.PrintToChatAll("GameLive - GL HF!!!");
        // Server.ExecuteCommand("exec ") // TODO: add proper GameLive Config
    }

    internal void ResetReadyStatus()
    {
        playerReadyStatus[CsTeam.Terrorist] = [];
        playerReadyStatus[CsTeam.CounterTerrorist] = [];
    }
}