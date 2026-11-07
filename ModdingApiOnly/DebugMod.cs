using Modding;

namespace DebugMod;

public partial class DebugMod : ITogglableMod
{
    public const string Id = "io.github.hk-speedrunning.debugmod";
    public static string Version { get; } = typeof(DebugMod).Assembly.GetName().Version.ToString(3);

    public override string GetVersion() => Version;

    public override void Initialize()
    {
        Awake();
        OnEnable();
    }

    public void Unload()
    {
        OnDisable();
        OnDestroy();
        DestroyCoroutineHost();
    }

    public new static void LogDebug(string message)
    {
        instance.Logger.LogDebug(message);
    }

    public new static void Log(string message)
    {
        instance.Logger.LogInfo(message);
    }

    public new static void LogWarn(string message)
    {
        instance.Logger.LogWarning(message);
    }

    public new static void LogError(string message)
    {
        instance.Logger.LogError(message);
    }
}
