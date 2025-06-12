namespace css_discord_webhook.Util;

public enum ChatColors
{
    white = 1,
    darkred = 2,
    purple = 3,
    green = 4,
    lightgreen = 5,
    slimegreen = 6,
    red = 7,
    grey = 8,
    yellow = 9,
    invisible = 10,
    lightblue = 11,
    blue = 12,
    lightpurple = 13,
    pink = 14,
    fadedred = 15,
    gold = 16
}

public static class Extensions
{
    public static string ToColoredChat(this ChatColors color, string text)
    {
        return $"\u200B{(char)color}{text}";
    }
}