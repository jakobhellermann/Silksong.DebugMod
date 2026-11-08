using DebugMod.Helpers;
using DebugMod.SaveStates;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DebugMod.UI.CommandPalette;

public static class CommandPaletteCommands
{
    public static void Initialize()
    {
        CommandPaletteRegistry registry = DebugMod.CommandPaletteRegistry;
        
        foreach (var category in DebugMod.bindActions.Values.GroupBy(action => action.Category))
        {
            registry.RegisterSubmenu(
                () => Localization.Get(category.Key),
                () => category.Select<BindAction, CommandPaletteItem>(action => new CommandPaletteItem.ActionItem(() => Localization.Get(action.Name), action.Action))
            );
        }
        
        registry.RegisterSubmenu(() => Localization.Get("COMMANDPALETTE_SAVESTATE_FILES"), FileSavestates);
        registry.RegisterSubmenu(() => Localization.Get("COMMANDPALETTE_WARP"), TeleportPoints);
    }
    
    #region Teleport

    private static readonly Lazy<Dictionary<string, List<string>>> scenesByArea =
        new(() => JsonUtil.Deserialize<Dictionary<string, List<string>>>("DebugMod.Resources.scenes_by_area.jsonc"));

#if HK1578_OR_OLDER
    private static readonly Lazy<Dictionary<string, List<string>>> transitionGates =
        new(() => JsonUtil.Deserialize<Dictionary<string, List<string>>>("DebugMod.Resources.transition_gates.jsonc"));
#endif

    private static IEnumerable<CommandPaletteItem> TeleportPoints()
    {
#if HK1578_OR_OLDER
        Dictionary<string, List<string>> gatesByScene = transitionGates.Value;
#else
        Dictionary<string, SceneTeleportMap.SceneInfo> teleportMap = SceneTeleportMap.GetTeleportMap();
        if (teleportMap == null) yield break;
        Dictionary<string, List<string>> gatesByScene = teleportMap.ToDictionary(kv => kv.Key, kv => kv.Value.TransitionGates);
#endif

        foreach ((string area, List<string> scenes) in scenesByArea.Value)
        {
#if HK1578_OR_OLDER
            List<string> existingScenes = scenes.Where(Application.CanStreamedLevelBeLoaded).ToList();
#else
            List<string> existingScenes = scenes;
#endif
            yield return new CommandPaletteItem.SubmenuItem(area, () => existingScenes.Select<string, CommandPaletteItem>(scene =>
                new CommandPaletteItem.SubmenuItem(
                    scene,
                    () => gatesByScene[scene].OrderBy(g => g).Select<string, CommandPaletteItem>(gate =>
                        new CommandPaletteItem.ActionItem(gate, () => GameManager.instance.StartCoroutine(Teleport(scene, gate)))),
                    searchChildren: false
                )));
        }
    }

    private static IEnumerator Teleport(string scene, string gate)
    {
        if (UIManager.instance.uiState.ToString() == "PAUSED")
            yield return GameManager.instance.PauseGameToggle(false);
        
        GameManager.instance.BeginSceneTransition(new GameManager.SceneLoadInfo
        {
            SceneName = scene,
            EntryGateName = gate,
        });
    }
    
    #endregion
    
    #region Savestates

    private static IEnumerable<CommandPaletteItem> FileSavestates()
    {
        foreach (SaveState state in SaveStateManager.AllSavestates)
        {
            SaveState snapshot = new() { data = state.data.DeepCopy() };
            yield return new CommandPaletteItem.ActionItem(state.ToString(), () =>
            {
                SaveStateManager.SetQuickState(snapshot);
                SaveStateManager.LoadState(SaveStateManager.GetQuickState());
            });
        }
    }
    
    #endregion
}
