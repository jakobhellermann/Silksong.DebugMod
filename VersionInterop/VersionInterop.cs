using System.Diagnostics.CodeAnalysis;

namespace DebugMod.VersionInterop;

public static class VersionInterop
{
    internal static void Foo()
    {
#if SILKSONG
        // do this
#endif

#if HK1512620
        // do that
#endif
    }
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class Extensions {
    extension(PlayerData playerData)
    {

    }

    extension(HeroController hero)
    {
    }
}
