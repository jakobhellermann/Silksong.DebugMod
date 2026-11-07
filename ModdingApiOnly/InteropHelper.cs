using Modding;
using System;

namespace DebugMod.Helpers;

internal static partial class InteropHelper
{
    internal static IMod GetPlugin(string id)
    {
        return Modding.ModHooks.GetMod(id);
    }

    internal static bool IsModInstalled(string id)
    {
        return Modding.ModHooks.GetMod(id) != null;
    }

    internal static bool IsModInstalled(string id, Version minVersion)
    {
        if (Modding.ModHooks.GetMod(id) is IMod mod)
        {
            return new Version(mod.GetVersion()) >= minVersion;
        }

        return false;
    }

    internal static bool IsModInstalled(string id, string minVersion)
    {
        return IsModInstalled(id, new Version(minVersion));
    }
}
