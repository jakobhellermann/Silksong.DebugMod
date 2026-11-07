using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugMod.SaveStates;

public static partial class SceneWatcher
{
    [HarmonyPatch(typeof(global::SceneManager), nameof(global::SceneManager.Start))]
    [HarmonyPostfix]
    private static void SceneManager_Start(global::SceneManager __instance) => OnSceneManagerStart(__instance.gameObject.scene);

    private static partial bool HasSceneManager(Scene scene) =>
        Object.FindObjectsByType<global::SceneManager>(FindObjectsSortMode.None).Any(m => m.gameObject.scene == scene);

    private static partial void SetGameSpecificSceneName(GameManager gameManager, string sceneName) { }
}
