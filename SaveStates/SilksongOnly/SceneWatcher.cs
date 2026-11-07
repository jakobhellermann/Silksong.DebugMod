using HarmonyLib;

namespace DebugMod.SaveStates;

public static partial class SceneWatcher
{
    [HarmonyPatch(typeof(CustomSceneManager), nameof(CustomSceneManager.Start))]
    [HarmonyPostfix]
    private static void CustomSceneManager_Start(CustomSceneManager __instance) => OnSceneManagerStart(__instance.gameObject.scene);
}
