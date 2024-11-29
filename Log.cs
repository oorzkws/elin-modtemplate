using BepInEx.Logging;

namespace ElinModTemplate;

public static class Logging {
    internal static void Log(object payload, LogLevel level = LogLevel.Info) {
        // This doesn't error, Rider just hasn't caught up
        ElinModTemplate.Logger.Log(level, payload);
    }
}