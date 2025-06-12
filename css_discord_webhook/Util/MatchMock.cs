using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using static CounterStrikeSharp.API.Utilities;

namespace css_discord_webhook.Util;

class MatchMock
{
    public string MapName { get; set; } = string.Empty;

    /// <summary>
    /// Can't be used to set the team names, use <c>mp_teamname_1</c> and <c>mp_teamname_2</c> instead.
    /// </summary>
    public CCSTeam? Team1 { get; set; }
    /// <summary>
    /// Can't be used to set the team names, use <c>mp_teamname_1</c> and <c>mp_teamname_2</c> instead.
    /// </summary>
    public CCSTeam? Team2 { get; set; }
}