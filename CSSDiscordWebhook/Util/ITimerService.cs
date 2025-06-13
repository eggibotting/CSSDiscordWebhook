using System;

namespace CSSDiscordWebhook.Util;

public interface ITimerService
{
    void AddTimer(float interval, Action callback);
}