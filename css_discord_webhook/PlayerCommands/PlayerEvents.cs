using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
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

}