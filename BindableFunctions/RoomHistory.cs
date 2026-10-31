using GlobalEnums;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace DebugMod;

public static partial class BindableFunctions
{
    private class RoomTransition(string fromScene, string fromGate, string toScene, string toGate)
    {
        public readonly string FromScene = fromScene;
        public readonly string FromGate = fromGate;
        public readonly string ToScene = toScene;
        public readonly string ToGate = toGate;
    }

    // distinct type so other mods can tell room history jumps apart from real gate transitions
    private class DebugModRoomChangeSceneLoadInfo : GameManager.SceneLoadInfo { }

    private static readonly List<RoomTransition> roomHistory = [];
    private static int roomHistoryPosition;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TransitionPoint), nameof(TransitionPoint.DoSceneTransition))]
    private static void TransitionPoint_DoSceneTransition(TransitionPoint __instance)
    {
        roomHistory.RemoveRange(roomHistoryPosition, roomHistory.Count - roomHistoryPosition);
        roomHistory.Add(new RoomTransition(__instance.gameObject.scene.name, __instance.name, __instance.targetScene, __instance.entryPoint));
        roomHistoryPosition = roomHistory.Count;
    }

    private static bool applyEntrySkipImmediately;
    private static bool faceEntryGate;
    private static TransitionPoint customFadeGate;

    // EntrySkip moves the hero via MovePosition, which is discarded if ShoveOff writes the transform before the next physics step
    [HarmonyPrefix]
    [HarmonyPatch(typeof(HeroController), nameof(HeroController.FinishedEnteringScene))]
    private static void HeroController_FinishedEnteringScene(HeroController __instance)
    {
        if (customFadeGate)
        {
            customFadeGate.SetCustomFade(false);
        }
        customFadeGate = null;

        if (!applyEntrySkipImmediately) return;
        applyEntrySkipImmediately = false;
        bool faceGate = faceEntryGate;
        faceEntryGate = false;

        float direction = __instance.gatePosition switch
        {
            GatePosition.left => 1f,
            GatePosition.right => -1f,
            _ => 0f,
        };
        if (direction == 0f) return;

        Vector2 position = __instance.rb2d.position + new Vector2(__instance.GetRunSpeed() * direction * 0.33f, 0f);
        __instance.rb2d.position = position;
        __instance.transform.SetPosition2D(position);

        if (faceGate)
        {
            if (direction > 0f) __instance.FaceLeft();
            else __instance.FaceRight();
        }
    }

    // the scene fade-in starts from black, which flashes since the screen was never faded out
    private static void SuppressEntryFadeIn(bool forceDirect)
    {
        HeroController hero = HeroController.instance;
        hero.heroInPosition -= SuppressEntryFadeIn;
        TransitionPoint gate = hero.sceneEntryGate;
        if (gate && !gate.customFade)
        {
            gate.SetCustomFade(true);
            customFadeGate = gate;
        }
    }

    [BindableMethod(name = "MISC_PREVIOUSROOM", category = "CATEGORY_MISC")]
    public static void GoToPreviousRoom()
    {
        if (roomHistoryPosition == 0)
        {
            DebugMod.LogConsole("No previous room");
            return;
        }

        RoomTransition transition = roomHistory[roomHistoryPosition - 1];
        if (GoToRoom(transition.FromScene, transition.FromGate))
        {
            faceEntryGate = true;
            roomHistoryPosition--;
        }
    }

    [BindableMethod(name = "MISC_NEXTROOM", category = "CATEGORY_MISC")]
    public static void GoToNextRoom()
    {
        if (roomHistoryPosition == roomHistory.Count)
        {
            DebugMod.LogConsole("No next room");
            return;
        }

        RoomTransition transition = roomHistory[roomHistoryPosition];
        if (GoToRoom(transition.ToScene, transition.ToGate))
        {
            roomHistoryPosition++;
        }
    }

    private static bool GoToRoom(string scene, string gate)
    {
        if (GameManager.instance.IsInSceneTransition)
        {
            DebugMod.LogConsole("Cannot change room during a scene transition");
            return false;
        }

        if (HeroController.instance.cState.dead)
        {
            DebugMod.LogConsole("Cannot change room while dead");
            return false;
        }

        // Unity activates scene loads in order, so a load queued behind an unconsumed preload deadlocks
        if (ScenePreloader.HasPendingOperations)
        {
            DebugMod.LogConsole("Cannot change room while a scene preload is pending");
            return false;
        }

        DebugMod.LogConsole($"Going to {scene} ({gate})");
        applyEntrySkipImmediately = true;
        if (HeroController.instance is { } hero)
        {
            hero.IgnoreInput();
            hero.heroInPosition -= SuppressEntryFadeIn;
            hero.heroInPosition += SuppressEntryFadeIn;
            // LeaveScene without a gate keeps the stale transition velocity, which makes the hero walk off
            hero.transition_vel = Vector2.zero;
        }
        GameManager.instance.BeginSceneTransition(new DebugModRoomChangeSceneLoadInfo
        {
            SceneName = scene,
            EntryGateName = gate,
            EntrySkip = true,
            EntryDelay = 0f,
            PreventCameraFadeOut = true,
            WaitForSceneTransitionCameraFade = false,
            Visualization = GameManager.SceneLoadVisualizations.Default,
        });
        return true;
    }
}
