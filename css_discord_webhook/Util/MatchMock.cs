using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Utilities;

namespace css_discord_webhook.Util;

class MatchMock
{
    public string MapName { get; set; } = string.Empty;
    public GameState GameState { get; set; }

    // Can't be used to set Properties, as References handle oddly.
    public CCSTeam? Team1 { get; set; }
    public CCSTeam? Team2 { get; set; }
}