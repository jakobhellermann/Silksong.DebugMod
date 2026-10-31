using DebugMod.Helpers;
using DebugMod.MonoBehaviours;
using DebugMod.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlobalEnums;
using HarmonyLib;
using HutongGames.PlayMaker;
using HutongGames.PlayMaker.Actions;
using UnityEngine;
using UnityEngine.Playables;

namespace DebugMod;

[HarmonyPatch]
public static partial class BindableFunctions
{
    [BindableMethod(name = "MISC_TOGGLEACT3", category = "CATEGORY_MISC")]
    public static void ToggleAct3()
    {
        PlayerData.instance.blackThreadWorld = !PlayerData.instance.blackThreadWorld;
        DebugMod.LogConsole("Act 3 world is now " + (PlayerData.instance.blackThreadWorld ? "enabled" : "disabled")
            + ", reload the scene to apply changes");
    }

    [BindableMethod(name = "MISC_SETHAZARDRESPAWN", category = "CATEGORY_MISC")]
    public static void SetHazardRespawn()
    {
        Vector3 manualRespawn = DebugMod.RefKnight.transform.position;
        HeroController.instance.SetHazardRespawn(manualRespawn, false);
        DebugMod.LogConsole("Manual respawn point on this map set to" + manualRespawn);
    }

    [BindableMethod(name = "MISC_HAZARDRESPAWN", category = "CATEGORY_MISC")]
    public static void Respawn()
    {
        if (GameManager.instance.IsGameplayScene() && !HeroController.instance.cState.dead &&
            PlayerData.instance.health > 0)
        {
            if (UIManager.instance.uiState.ToString() == "PAUSED")
            {
                InputHandler.Instance.StartCoroutine(GameManager.instance.PauseGameToggle(false));
                GameManager.instance.HazardRespawn();
                DebugMod.LogConsole("Closing pause menu and respawning...");
                return;
            }

            if (UIManager.instance.uiState.ToString() == "PLAYING")
            {
                HeroController.instance.RelinquishControl();
                GameManager.instance.HazardRespawn();
                HeroController.instance.RegainControl();
                DebugMod.LogConsole("Respawn signal sent");
                return;
            }

            DebugMod.LogConsole("How are you seeing this??? (please create a bug report)");
        }
    }

    [BindableMethod(name = "MISC_DAMAGESELF", category = "CATEGORY_MISC")]
    public static void SelfDamage()
    {
        if (PlayerData.instance.health <= 0)
        {
            DebugMod.LogConsole("Cannot damage self: health <= 0");
        }
        else if (HeroController.instance.cState.dead)
        {
            DebugMod.LogConsole("Cannot damage self: player is dead");
        }
        else if (!GameManager.instance.IsGameplayScene())
        {
            DebugMod.LogConsole("Cannot damage self: not a gameplay scene");
        }
        else if (HeroController.instance.cState.recoiling)
        {
            DebugMod.LogConsole("Cannot damage self: player is recoiling");
        }
        else if (HeroController.instance.cState.invulnerable)
        {
            DebugMod.LogConsole("Cannot damage self: player is invulnerable");
        }
        else
        {
            HeroController.instance.DamageSelf(1);
            DebugMod.LogConsole("Attempting self damage");
        }
    }

    [BindableMethod(name = "MISC_KILLSELF", category = "CATEGORY_MISC")]
    public static void KillSelf()
    {
        if (!HeroController.instance.cState.dead && !HeroController.instance.cState.transitioning)
        {
            HeroController.instance.StartCoroutine(HeroController.instance.Die(false, false));
            DebugMod.LogConsole("Killed player");
        }
    }

    [BindableMethod(name = "MISC_BREAKCOCOON", category = "CATEGORY_MISC")]
    public static void BreakCocoon()
    {
        HeroController.instance?.CocoonBroken();
        EventRegister.SendEvent("BREAK HERO CORPSE");
    }

    private static string saveLevelStateAction;

    [BindableMethod(name = "MISC_RESETCURRENTSCENEDATA", category = "CATEGORY_MISC")]
    public static void ResetCurrentScene()
    {
        saveLevelStateAction = GameManager.instance.GetSceneNameString();
        DebugMod.LogConsole("Clearing scene data from this scene, reload the scene to apply changes");
    }

    [BindableMethod(name = "MISC_BLOCKSCENEDATACHANGES", category = "CATEGORY_MISC")]
    public static void BlockCurrentSceneChanges()
    {
        saveLevelStateAction = "block";
        DebugMod.LogConsole("Scene data changes made since entering this scene will not be saved");
    }

    private static readonly (string objectPrefix, string fsmName)[] skippableFsms =
    [
        ("Weakness Scene", "Control"),
        ("Churchkeeper Intro Scene", "Control"),
        ("Silk Heart", "Control"),
        ("door_", "Wake Up"),
        ("memory_orb", "Control"),
        ("Memory Control", "Memory Control"),
        ("Silk Heart Memory Return", "Silk Heart Memory Return"),
        ("Bone Beast NPC", "Interaction"),
        ("Bone Beast Corpse", "Death"),
    ];

    [BindableMethod(name = "MISC_SKIPWAIT", category = "CATEGORY_MISC")]
    public static void SkipWait()
    {
        GameManager gm = GameManager.instance;
        if (gm.GameState == GameState.CUTSCENE)
        {
            SkipPromptMode mode = gm.inputHandler.SkipMode;
            if (mode is not (SkipPromptMode.SKIP_INSTANT or SkipPromptMode.SKIP_PROMPT))
            {
                DebugMod.LogConsole($"Cutscene is not skippable right now ({mode})");
                return;
            }

            if (!gm.inputHandler.skippingCutscene)
            {
                DebugMod.LogConsole("Skipping cutscene");
                gm.inputHandler.skippingCutscene = true;
                gm.SkipCutscene();
            }
            return;
        }

        if (SkipUIMsg())
        {
            return;
        }

        DialogueBox box = DialogueBox._instance;
        if (box && box.isDialogueRunning)
        {
            if (!fastForwardingDialogue)
            {
                DebugMod.instance.StartCoroutine(FastForwardDialogue(box));
            }
            return;
        }

        foreach (PlayMakerFSM fsm in Object.FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None))
        {
            bool skippable = skippableFsms.Any(s => fsm.FsmName == s.fsmName && fsm.gameObject.name.StartsWith(s.objectPrefix))
                || IsStunningRoar(fsm)
                || (fsm.isActiveAndEnabled && IsPromptFsm(fsm));
            if (skippable && SkipFsm(fsm))
            {
                return;
            }
        }

        DebugMod.LogConsole("Nothing to skip");
    }

    private static readonly Dictionary<UIMsgProxy, IEnumerator> runningUIMsgs = [];

    [HarmonyPostfix]
    [HarmonyPatch(typeof(UIMsgBase<PowerUpGetMsg.PowerUps>), nameof(UIMsgBase<PowerUpGetMsg.PowerUps>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItemSkill>), nameof(UIMsgBase<ToolItemSkill>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolCrest>), nameof(UIMsgBase<ToolCrest>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItem>), nameof(UIMsgBase<ToolItem>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItemType>), nameof(UIMsgBase<ToolItemType>.DoMsg))]
    private static void UIMsgBase_DoMsg(UIMsgProxy __instance, IEnumerator __result)
    {
        runningUIMsgs[__instance] = __result;
    }

    // Runs the message coroutine to its end synchronously, so its hide and afterMsg callback happen as on a real press
    private static bool SkipUIMsg()
    {
        foreach (UIMsgProxy destroyed in runningUIMsgs.Keys.Where(m => !m).ToList())
        {
            runningUIMsgs.Remove(destroyed);
        }

        KeyValuePair<UIMsgProxy, IEnumerator> running = runningUIMsgs.FirstOrDefault(m => m.Key.gameObject.activeInHierarchy);
        if (running.Key == null) return false;

        (UIMsgProxy msg, IEnumerator routine) = (running.Key, running.Value);
        runningUIMsgs.Remove(msg);
        FieldInfo wasPressed = routine.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            .SingleOrDefault(f => f.Name.Contains("wasPressed"))
            ?? throw new System.MissingFieldException(routine.GetType().FullName, "wasPressed");

        DebugMod.LogConsole($"Skipping message {msg.name}");
        msg.StopCoroutine(routine);
        for (int steps = 0; routine.MoveNext(); steps++)
        {
            if (steps == 1000) throw new System.InvalidOperationException($"{msg.name} did not finish after {steps} steps");
            wasPressed.SetValue(routine, true);
        }

        return true;
    }

    private static bool IsPromptFsm(PlayMakerFSM fsm) =>
        fsm.FsmStates.Any(state => state.Actions.OfType<ListenForPromptContinue>().Any());

    private enum SkipResult { Nothing, Refused, Done }

    // keeps skipping until the fsm reaches something that can't be skipped
    private static bool SkipFsm(PlayMakerFSM fsm)
    {
        SkipResult result = SkipResult.Nothing;
        bool progressed = false;
        for (int steps = 0; (result = SkipStep(fsm)) == SkipResult.Done; steps++)
        {
            progressed = true;
            if (steps == 20)
            {
                DebugMod.LogConsole($"Stopped skipping {fsm.gameObject.name}/{fsm.FsmName} after {steps} steps, it seems to loop");
                break;
            }
        }

        return progressed || result == SkipResult.Refused;
    }

    private static bool fastForwardingDialogue;

    // Advances line by line instead of ending the conversation, so per-line events and end callbacks still fire
    private static IEnumerator FastForwardDialogue(DialogueBox box)
    {
        DebugMod.LogConsole("Fast-forwarding dialogue");
        fastForwardingDialogue = true;
        try
        {
            while (box && box.isDialogueRunning)
            {
                box.currentRevealSpeed = 100_000f;
                box.waitingToAdvance = false;
                yield return null;
            }
        }
        finally
        {
            fastForwardingDialogue = false;
            if (box) box.currentRevealSpeed = box.regularRevealSpeed;
        }
    }

    private static SkipResult SkipStep(PlayMakerFSM fsm)
    {
        FsmState state = fsm.Fsm.ActiveState;
        if (state == null) return SkipResult.Nothing;

        if (state.Actions.OfType<ListenForPromptContinue>().FirstOrDefault(a => a.Enabled) is { } prompt)
        {
            DebugMod.LogConsole($"Continuing prompt {fsm.gameObject.name}/{fsm.FsmName}: {state.Name}");
            fsm.Fsm.Event(prompt.ContinuePressed);
            return SkipResult.Done;
        }

        // a sequence only has entered the actions up to its current one
        FsmStateAction[] entered = state.IsSequence ? state.Actions.Take(state.ActiveActionIndex + 1).ToArray() : state.Actions;
        Wait wait = entered.OfType<Wait>().FirstOrDefault(w => w is { Enabled: true, Finished: false });
        string name = $"{fsm.gameObject.name}/{fsm.FsmName}: {state.Name}";
        if (wait == null)
        {
            SkipResult timeline = FinishTimeline(entered, name);
            if (timeline != SkipResult.Nothing) return timeline;
            SkipResult animation = FinishAnimation(fsm, entered, name);
            return animation != SkipResult.Nothing ? animation : FastForwardEventSource(fsm, state, name);
        }

        float remaining = wait.time.Value - wait.timer;
        float endTime = state.StateTime + remaining;

        List<string> unhandled = entered
            .Where(a => a != wait && a.Enabled && !a.Finished && !CanCompleteAction(a))
            .Select(a => a.GetType().Name)
            .ToList();
        if (unhandled.Count > 0)
        {
            DebugMod.LogConsole($"Cannot skip wait in {name}, unhandled actions: {string.Join(", ", unhandled)}");
            return SkipResult.Refused;
        }

        DebugMod.LogConsole($"Skipping wait in {name}");
        foreach (FsmStateAction action in entered.Where(a => a != wait && a.Enabled))
        {
            CompleteAction(action, remaining, endTime);
        }

        // delayed events of this fsm are discarded on state exit, so fire the ones due before the wait ends
        if (!fsm.Fsm.keepDelayedEventsOnStateExit)
        {
            foreach (DelayedEvent delayedEvent in fsm.Fsm.DelayedEvents.ToList())
            {
                if (delayedEvent.Finished || delayedEvent.Timer > remaining) continue;
                delayedEvent.timer = -1f;
                delayedEvent.Update();
                fsm.Fsm.DelayedEvents.Remove(delayedEvent);
            }
        }

        if (fsm.Fsm.ActiveState != state) return SkipResult.Done;

        if (wait.finishEvent != null)
        {
            fsm.Fsm.Event(wait.finishEvent);
        }
        else
        {
            // what the state's next update would do: advance the sequence or send FINISHED
            wait.Finish();
            state.CheckAllActionsFinished();
        }

        return SkipResult.Done;
    }

    // jumps the director to its end and does what PlayTimeline.End would on its next update
    private static SkipResult FinishTimeline(FsmStateAction[] entered, string name)
    {
        if (entered.OfType<PlayTimeline>().FirstOrDefault(a => a.Enabled && !a.Finished) is not { } play) return SkipResult.Nothing;

        PlayableDirector director = play.timeline;
        DebugMod.LogConsole($"Finishing timeline {director.name} in {name}");
        director.time = director.duration;
        director.Evaluate();
        play.Fsm.Event(play.FinishEvent);
        play.Finish();
        return SkipResult.Done;
    }

    // advances the watched clip to its next trigger frame or its end, so frame events and callbacks fire as they would naturally
    private static SkipResult FinishAnimation(PlayMakerFSM fsm, FsmStateAction[] entered, string name)
    {
        foreach (FsmStateAction action in entered.Where(a => a.Enabled && !a.Finished))
        {
            (FsmOwnerDefault target, FsmEvent triggerEvent, FsmEvent completeEvent) = action switch
            {
                Tk2dWatchAnimationEvents a => (a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent),
                Tk2dWatchAnimationEventsV3 a => (a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent),
                Tk2dPlayAnimationWithEvents a => (a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent),
                Tk2dPlayAnimationWithEventsV3 a => (a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent),
                _ => (null, null, null),
            };
            if (triggerEvent == null && completeEvent == null) continue;

            tk2dSpriteAnimator animator = fsm.Fsm.GetOwnerDefaultTarget(target)?.GetComponent<tk2dSpriteAnimator>();
            if (!animator || !animator.Playing) continue;

            tk2dSpriteAnimationClip clip = animator.CurrentClip;
            int? targetFrame = null;
            if (triggerEvent != null)
            {
                targetFrame = Enumerable.Range((int)animator.clipTime + 1, clip.frames.Length - 1 - (int)animator.clipTime)
                    .Cast<int?>()
                    .FirstOrDefault(i => clip.frames[i.Value].triggerEvent);
            }
            if (targetFrame == null && completeEvent != null)
            {
                if (clip.wrapMode != tk2dSpriteAnimationClip.WrapMode.Once)
                {
                    DebugMod.LogConsole($"Cannot finish looping animation {clip.name} in {name}");
                    return SkipResult.Refused;
                }
                targetFrame = clip.frames.Length;
            }
            if (targetFrame == null)
            {
                DebugMod.LogConsole($"No trigger frame left in animation {clip.name} in {name}");
                return SkipResult.Refused;
            }

            DebugMod.LogConsole($"Advancing animation {clip.name} to frame {targetFrame} in {name}");
            animator.UpdateAnimation((targetFrame.Value - animator.clipTime) / animator.clipFps + 0.001f);
            return SkipResult.Done;
        }

        return SkipResult.Nothing;
    }

    // events sent by game systems outside the fsm, with a way to run that system up to sending it
    private static readonly Dictionary<string, System.Func<string>> eventSourceFastForwards = new()
    {
        ["REGENERATED SILK CHUNK"] = FastForwardSilkRegen,
    };

    private static SkipResult FastForwardEventSource(PlayMakerFSM fsm, FsmState state, string name)
    {
        string eventName = state.Transitions.Select(t => t.EventName).FirstOrDefault(eventSourceFastForwards.ContainsKey);
        if (eventName == null) return SkipResult.Nothing;

        string error = eventSourceFastForwards[eventName]();
        if (error == null && fsm.Fsm.ActiveState == state) error = "the event did not leave the state";
        DebugMod.LogConsole(error == null
            ? $"Fast-forwarded {eventName} in {name}"
            : $"Cannot fast-forward {eventName} in {name}: {error}");
        return error == null ? SkipResult.Done : SkipResult.Refused;
    }

    // runs the pending silk regen timers out the way HeroController.Update does, returns why not if it can't
    private static string FastForwardSilkRegen()
    {
        HeroController hero = HeroController.instance;
        if (hero.isSilkRegenBlocked || !hero.hasSilkSpoolAppeared) return "silk regen is blocked";

        if (hero.silkRegenDelayLeft > 0f)
        {
            hero.silkRegenDelayLeft = 0f;
            hero.StartSilkRegen();
        }
        if (hero.silkRegenDurationLeft > 0f)
        {
            hero.silkRegenDurationLeft = 0f;
            hero.DoSilkRegen();
        }

        return null;
    }

    // the roar ends via StartRoarEmitter.OnExit or a StopRoarEmitter in a following state
    private static bool IsStunningRoar(PlayMakerFSM fsm) =>
        fsm.isActiveAndEnabled
        && fsm.Fsm.ActiveState is { } state
        && state.Actions.Any(a => a.Enabled && a switch
        {
            StartRoarEmitter roar => roar.stunHero.Value,
            StartRoarEmitterV2 roar => roar.stunHero.Value,
            _ => false,
        });

    private static bool CanCompleteAction(FsmStateAction action) => action
        is CameraBlurPlaneFade
        or StartRoarEmitter
        or StartRoarEmitterV2
        or ActivateGameObjectDelay
        or AudioPlayerOneShotSingle
        or AudioPlayerOneShot;

    private static void CompleteAction(FsmStateAction action, float remaining, float endTime)
    {
        switch (action)
        {
            case CameraBlurPlaneFade { Finished: false } blur:
                float t = blur.Curve.curve.Evaluate(Mathf.Clamp01(endTime / blur.Duration.Value));
                CameraBlurPlane.Spacing = Mathf.Lerp(blur.From.Spacing.Value, blur.To.Spacing.Value, t);
                CameraBlurPlane.Vibrancy = Mathf.Lerp(blur.From.Vibrancy.Value, blur.To.Vibrancy.Value, t);
                if (blur.From.MaskLerp != null && blur.To.MaskLerp != null)
                {
                    CameraBlurPlane.MaskLerp = Mathf.Lerp(blur.From.MaskLerp.Value, blur.To.MaskLerp.Value, t);
                }
                break;
            case ActivateGameObjectDelay { Finished: false } activate when activate.timer <= remaining:
                activate.DoActivateGameObject();
                break;
            case StartRoarEmitter { didRoar: false } roar when roar.delay.Value - roar.timer <= remaining:
                roar.StartRoar();
                break;
            case StartRoarEmitterV2 { didRoar: false } roar when roar.delay.Value - roar.timer <= remaining:
                roar.StartRoar();
                break;
            case ScreenFader fader when fader.Fsm.ActiveState.StateTime < fader.duration.Value && endTime >= fader.duration.Value:
                ScreenFaderUtils.SetColour(fader.endColour.Value);
                break;
            case ScreenFaderAlpha alphaFader when endTime - remaining < alphaFader.Duration.Value:
                // the screen fader eases linearly, so the colour at the end of the wait can be interpolated from now
                float now = endTime - remaining;
                float duration = alphaFader.Duration.Value;
                Color current = ScreenFaderUtils.GetColour();
                Color target = current with { a = alphaFader.EndAlpha.Value };
                if (endTime >= duration)
                {
                    ScreenFaderUtils.SetColour(target);
                }
                else
                {
                    Color atWaitEnd = Color.Lerp(current, target, (endTime - now) / (duration - now));
                    ScreenFaderUtils.Fade(atWaitEnd, target, duration - endTime);
                }
                break;
        }
    }

    [BindableMethod(name = "MISC_QUEUEWALLJUMPINTERRUPT", category = "CATEGORY_MISC")]
    public static void QueueWallJump()
    {
        HeroController.instance.queuedWallJumpInterrupt = true;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveLevelState))]
    [HarmonyPrefix]
    private static bool GameManager_SaveLevelState_Prefix()
    {
        if (saveLevelStateAction == "block")
        {
            saveLevelStateAction = null;
            return false;
        }

        return true;
    }

    [HarmonyPatch(typeof(GameManager), nameof(GameManager.SaveLevelState))]
    [HarmonyPostfix]
    private static void GameManager_SaveLevelState_Postfix()
    {
        if (saveLevelStateAction != null && saveLevelStateAction != "block")
        {
            SceneData.instance.persistentBools.scenes.Remove(saveLevelStateAction);
            SceneData.instance.persistentInts.scenes.Remove(saveLevelStateAction);
            SceneData.instance.geoRocks.scenes.Remove(saveLevelStateAction);

            saveLevelStateAction = null;
        }
    }

    [BindableMethod(name = "MISC_LOCKKEYBINDS", category = "CATEGORY_MISC")]
    public static void ToggleLockKeyBinds()
    {
        DebugMod.KeyBindLock = !DebugMod.KeyBindLock;
        DebugMod.LogConsole($"{(DebugMod.KeyBindLock ? "Removing" : "Adding")} the ability to use keybinds");
    }

    [BindableMethod(name = "MISC_RESETCHEATS", category = "CATEGORY_MISC")]
    public static void Reset()
    {
        var pd = PlayerData.instance;
        var HC = HeroController.instance;
        var GC = GameCameras.instance;

        //nail damage
        DebugMod.extraNailDamage = 0;
        PlayerData.instance.nailUpgrades = 0;
        PlayMakerFSM.BroadcastEvent("UPDATE NAIL DAMAGE");

        //Hero Light
        GameObject gameObject = DebugMod.RefKnight.transform.Find("HeroLight").gameObject;
        Color color = gameObject.GetComponent<SpriteRenderer>().color;
        color.a = 0.7f;
        gameObject.GetComponent<SpriteRenderer>().color = color;

        //Hide Hero
        tk2dSprite component = DebugMod.RefKnight.GetComponent<tk2dSprite>();
        color = component.color; color.a = 1f;
        component.color = color;

        // noclip
        DebugMod.noclip = false;
        DebugMod.RefKnight.GetComponent<Rigidbody2D>().constraints &= ~RigidbodyConstraints2D.FreezePosition;

        //rest all is self explanatory
        TimeScale.Reset();
        GC.tk2dCam.ZoomFactor = 1f;
        VisualMaskHelper.vignetteDisabled = false;
        DebugMod.HC.vignette.enabled = true;
        EnemiesPanel.hpBars = false;
        pd.infiniteAirJump = false;
        DebugMod.infiniteSilk = false;
        DebugMod.infiniteHP = false;
        pd.isInvincible = false;

        DebugMod.LogConsole("Reset all gameplay toggles");
    }
}