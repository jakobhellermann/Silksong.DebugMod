using BepInEx;
using BepInEx.Bootstrap;
using System;

namespace DebugMod.Helpers;

internal static partial class InteropHelper
{
    internal static BaseUnityPlugin GetPlugin(string id)
    {
        return Chainloader.PluginInfos[id].Instance;
    }

    internal static bool IsModInstalled(string id)
    {
        return Chainloader.PluginInfos.ContainsKey(id);
    }

    internal static bool IsModInstalled(string id, Version minVersion)
    {
        if (Chainloader.PluginInfos.TryGetValue(id, out PluginInfo info))
        {
            return info.Metadata.Version >= minVersion;
        }

        return false;
    }

    internal static bool IsModInstalled(string id, string minVersion)
    {
        return IsModInstalled(id, new Version(minVersion));
    }
}
