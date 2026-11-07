using BepInEx;

namespace DebugMod;

[BepInAutoPlugin("io.github.hk-speedrunning.debugmod")]
public partial class DebugMod
{
    public static void LogDebug(string message)
    {
        instance.Logger.LogDebug(message);
    }

    public static void Log(string message)
    {
        instance.Logger.LogInfo(message);
    }

    public static void LogWarn(string message)
    {
        instance.Logger.LogWarning(message);
    }

    public static void LogError(string message)
    {
        instance.Logger.LogError(message);
    }
}
