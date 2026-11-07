#if HK1221_OR_OLDER
using Modding;
using System;
using System.Collections;
using UnityEngine;

namespace DebugMod;

// Static view of the instance hooks of the Modding API before 1.3.1.5
internal static class ModHooks
{
    public static event AfterSavegameLoadHandler AfterSavegameLoadHook
    {
        add => Modding.ModHooks.Instance.AfterSavegameLoadHook += value;
        remove => Modding.ModHooks.Instance.AfterSavegameLoadHook -= value;
    }

    public static event VoidHandler BeforePlayerDeadHook
    {
        add => Modding.ModHooks.Instance.BeforePlayerDeadHook += value;
        remove => Modding.ModHooks.Instance.BeforePlayerDeadHook -= value;
    }

    public static event BeforeSceneLoadHandler BeforeSceneLoadHook
    {
        add => Modding.ModHooks.Instance.BeforeSceneLoadHook += value;
        remove => Modding.ModHooks.Instance.BeforeSceneLoadHook -= value;
    }

    public static event ColliderCreateHandler ColliderCreateHook
    {
        add => Modding.ModHooks.Instance.ColliderCreateHook += value;
        remove => Modding.ModHooks.Instance.ColliderCreateHook -= value;
    }

    public static event CursorHandler CursorHook
    {
        add => Modding.ModHooks.Instance.CursorHook += value;
        remove => Modding.ModHooks.Instance.CursorHook -= value;
    }

    public static event NewGameHandler NewGameHook
    {
        add => Modding.ModHooks.Instance.NewGameHook += value;
        remove => Modding.ModHooks.Instance.NewGameHook -= value;
    }

    public static event TakeHealthProxy TakeHealthHook
    {
        add => Modding.ModHooks.Instance.TakeHealthHook += value;
        remove => Modding.ModHooks.Instance.TakeHealthHook -= value;
    }

    private static event Action finishedLoadingModsHook;
    private static bool waitingForMods;

    public static event Action FinishedLoadingModsHook
    {
        add
        {
            if (ModLoader.Loaded)
            {
                value();
                return;
            }

            finishedLoadingModsHook += value;
            if (!waitingForMods)
            {
                waitingForMods = true;
                DebugMod.instance.StartCoroutine(WaitForMods());
            }
        }
        remove => finishedLoadingModsHook -= value;
    }

    private static IEnumerator WaitForMods()
    {
        yield return new WaitUntil(() => ModLoader.Loaded);
        waitingForMods = false;
        finishedLoadingModsHook?.Invoke();
    }
}
#endif
