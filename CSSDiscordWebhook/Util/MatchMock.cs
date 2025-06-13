using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;

namespace CSSDiscordWebhook.Util;

class MatchMock
{
    public string MapName { get; } = Server.MapName;

    /// <summary>
    /// Can't be used to set the team names, use <c>mp_teamname_1</c> and <c>mp_teamname_2</c> instead.
    /// </summary>
    public CCSTeam? Team1 { get; set; }
    /// <summary>
    /// Can't be used to set the team names, use <c>mp_teamname_1</c> and <c>mp_teamname_2</c> instead.
    /// </summary>
    public CCSTeam? Team2 { get; set; }
}