using HarmonyLib;

namespace DebugMod.SaveStates;

public static partial class SceneWatcher
{
    [HarmonyPatch(typeof(global::SceneManager), nameof(global::SceneManager.Start))]
    [HarmonyPostfix]
    private static void SceneManager_Start(global::SceneManager __instance) => OnSceneManagerStart(__instance.gameObject.scene);
}
