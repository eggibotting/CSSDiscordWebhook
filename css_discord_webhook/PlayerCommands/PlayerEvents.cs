using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using css_discord_webhook.Util;
using Microsoft.Extensions.DependencyInjection;

namespace css_discord_webhook.Player;

public class PlayerEvents(PlayerMethods playerMethods)
{
    private readonly PlayerMethods _playerMethods = playerMethods;

    [GameEventHandler]
    public HookResult OnPlayerSpawned(EventPlayerSpawn playerSpawned, GameEventInfo info)
    {
        // CenterHtmlMenu menu = new("test", this);
        // menu.Open(playerSpawned.Userid!);
        playerSpawned.Userid!.PrintToChat($"\u200B{Convert.ToChar(4)}Use !ready to mark yourself as ready. Use !unready to unmark yourself.");
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerDisconnect(EventPlayerDisconnect disconnect, GameEventInfo info)
    {
        _playerMethods.UnreadyPlayer(disconnect.Userid!);
        return HookResult.Continue;
    }

    [GameEventHandler]
    public HookResult OnPlayerTeamChange(EventPlayerTeam teamChange, GameEventInfo info)
    {
        _playerMethods.UnreadyPlayer(teamChange.Userid!);
        return HookResult.Continue;
    }

    // TODO: Find trigger that works for manually started/ended warmups

    /// <summary>
    /// Triggers, when a match is starting (only after warmups, also after <c>mp_restartgame 1</c>)
    /// </summary>
    [GameEventHandler]
    public HookResult OnGameRestart(EventBeginNewMatch roundStart, GameEventInfo info)
    {
        _playerMethods.GameState = GameState.Live;
        _playerMethods.ResetReadyStatus();

        return HookResult.Continue;
    }

    /// <summary>
    /// Triggers, when warmup naturally ends (not after <c>mp_warmup_end</c>)
    /// </summary>
    [GameEventHandler]
    public HookResult OnWarmupEnd(EventWarmupEnd warmupEnd, GameEventInfo info)
    {
        _playerMethods.GameState = GameState.Live;
        _playerMethods.ResetReadyStatus();

        return HookResult.Continue;
    }

    /// <summary>
    /// Triggers, when warmup starts (not after <c>mp_warmup_start</c>)
    /// </summary>
    [GameEventHandler]
    public HookResult OnWarmupStart(EventRoundAnnounceWarmup warmupEnd, GameEventInfo info)
    {
        _playerMethods.GameState = GameState.Warmup;
        _playerMethods.ResetReadyStatus();

        return HookResult.Continue;
    }
}