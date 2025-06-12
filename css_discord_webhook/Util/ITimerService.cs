using System;

namespace css_discord_webhook.Util;

public interface ITimerService
{
    void AddTimer(float interval, Action callback);
}