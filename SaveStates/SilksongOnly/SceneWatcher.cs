using HarmonyLib;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DebugMod.SaveStates;

public static partial class SceneWatcher
{
    [HarmonyPatch(typeof(CustomSceneManager), nameof(CustomSceneManager.Start))]
    [HarmonyPostfix]
    private static void CustomSceneManager_Start(CustomSceneManager __instance) => OnSceneManagerStart(__instance.gameObject.scene);

    private static partial bool HasSceneManager(Scene scene) =>
        Object.FindObjectsByType<CustomSceneManager>(FindObjectsSortMode.None).Any(m => m.gameObject.scene == scene);

    private static partial void SetGameSpecificSceneName(GameManager gameManager, string sceneName)
    {
        if (sceneName != gameManager.sceneName)
        {
            gameManager.lastSceneName = gameManager.sceneName;
        }
        gameManager.rawSceneName = sceneName;
        gameManager.sceneNameHash = sceneName.GetHashCode();
    }
}
