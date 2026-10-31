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
        ("Lace NPC Citadel Meet", "Control"),
        ("Boss Scene", "Sequence"),
        ("Mapper NPC", "Ring Shout"), // Shakra's entrance before her first meeting
    ];

    [BindableMethod(name = "MISC_SKIPWAIT", category = "CATEGORY_MISC")]
    public static void SkipWait()
    {
        LogSkip($"Skip requested (frame {Time.frameCount})");
        if (skipping)
        {
            LogSkip("Still skipping");
            return;
        }
        SkipEverything();
    }

    // true when it progressed
    private static bool SkipCutscene(GameManager gm)
    {
        // a cinematic still starting or showing its act card, during which the game doesn't allow skipping
        if (HasRoutines<CinematicPlayer>(IsTriggeredCinematic))
        {
            LogSkip($"Fast-forwarding cinematic {string.Join(", ", skipRoutines.Where(r => r.Owner is CinematicPlayer p && IsTriggeredCinematic(p)))}");
            FastForwardRoutines<CinematicPlayer>(IsTriggeredCinematic);
            return true;
        }

        SkipPromptMode mode = gm.inputHandler.SkipMode;
        if (mode is not (SkipPromptMode.SKIP_INSTANT or SkipPromptMode.SKIP_PROMPT))
        {
            LogSkip($"Cutscene is not skippable right now ({mode})");
            return false;
        }
        if (gm.inputHandler.skippingCutscene) return false;

        LogSkip("Skipping cutscene");
        gm.inputHandler.skippingCutscene = true;
        gm.SkipCutscene();
        return true;
    }

    // a preloaded cinematic's start routine waits for its trigger
    private static bool IsTriggeredCinematic(CinematicPlayer player) =>
        player.playTrigger != CinematicPlayer.MovieTrigger.ManualPlayPreloaded || player.allowPlay;

    private static bool skipping;

    // bounds fsms that keep progressing forever
    private const int MaxSkipRounds = 50;

    // each kind of skip uncovers others (a message ends a state, a state starts a dialogue), so repeat until none progresses
    private static void SkipEverything()
    {
        skipping = true;
        bool textPrintSkip = CheatManager.IsTextPrintSkipEnabled;
        CheatManager.IsTextPrintSkipEnabled = true;
        finishedDialogue = false;
        checkedSends.Clear();
        SceneLoad loadBefore = GameManager.instance.LastSceneLoad;
        try
        {
            bool skippedAny = false;
            for (int round = 0; ; round++)
            {
                if (round == MaxSkipRounds)
                {
                    LogSkip($"stopped after {round} rounds, it seems to loop");
                    break;
                }

                FinishStartedRoutines();

                // a triggered cinematic only enters the cutscene state on its next update
                if (GameManager.instance.GameState == GameState.CUTSCENE || HasRoutines<CinematicPlayer>(IsTriggeredCinematic))
                {
                    if (!SkipCutscene(GameManager.instance)) break;
                    skippedAny = true;
                    continue;
                }
                if (SkipUIMsg())
                {
                    skippedAny = true;
                    continue;
                }
                if (HasRoutines<DialogueBox>())
                {
                    skippedAny = true;
                    FastForwardDialogue();
                    continue;
                }
                SkipResult fsms = SkipFsms();
                if (fsms == SkipResult.Nothing) break;
                skippedAny = true;
                if (fsms == SkipResult.Break) break;
            }

            if (!skippedAny)
            {
                // being stuck without control is when chains silently ending matter, so name the candidates then
                List<string> candidates = HeroController.instance.acceptingInput
                    ? []
                    : SkippableFsms().Select(fsm => $"{FsmName(fsm)} in {fsm.ActiveStateName}, {Awaiting(fsm.Fsm)}").ToList();
                LogSkip(candidates.Count == 0 ? "Nothing to skip" : $"Nothing to skip: {string.Join("; ", candidates)}");
            }
        }
        finally
        {
            skipping = false;
            CheatManager.IsTextPrintSkipEnabled = textPrintSkip;
        }

        // the close animation would still play out after an instantly ended conversation
        if (finishedDialogue && DialogueBox._instance is { isBoxOpen: false }) DialogueBox.HideInstant();

        GameManager gm = GameManager.instance;
        if (gm.IsInSceneTransition && gm.LastSceneLoad != loadBefore) ContinueAfterSceneLoad(gm.LastSceneLoad);
    }

    // a scene load can't be skipped, but the request goes on once the hero has entered the scene it loaded
    private static SceneLoad pendingContinuation;

    private static void ContinueAfterSceneLoad(SceneLoad load)
    {
        LogSkip($"waiting for the load of {load.TargetSceneName} to continue");
        pendingContinuation = load;
    }

    // after the entry gate's AfterEntry, which sends FINISH ENTRY to custom entry fsms like waking up on the ground
    [HarmonyPostfix]
    [HarmonyPatch(typeof(HeroController), "FinishedEnteringScene")]
#pragma warning disable HARMONIZE001
    private static void ContinueOnceEntered()
#pragma warning restore HARMONIZE001
    {
        if (pendingContinuation is not { } load) return;
        pendingContinuation = null;
        if (GameManager.instance.LastSceneLoad != load)
        {
            LogSkip($"not continuing, another scene load replaced the one of {load.TargetSceneName}");
            return;
        }
        LogSkip($"continuing after entering {load.TargetSceneName}");
        if (!skipping) SkipEverything();
    }

    private static SkipResult SkipFsms()
    {
        foreach (PlayMakerFSM fsm in SkippableFsms())
        {
            // one fsm that is stuck must not keep the others from being skipped
            SkipResult result = SkipChain(fsm.Fsm, FsmName(fsm), SkipUntil(fsm));
            if (result is SkipResult.Done or SkipResult.Break) return result;
        }
        return SkipRoarLock() ? SkipResult.Done : SkipResult.Nothing;
    }

    private static IEnumerable<PlayMakerFSM> SkippableFsms() => Object.FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None)
        .Where(fsm => IsSkippableThroughout(fsm) || IsConversation(fsm) || IsHeldPrompt(fsm) || IsStunningRoar(fsm));

    private static bool IsSkippableThroughout(PlayMakerFSM fsm) =>
        skippableFsms.Any(s => fsm.FsmName == s.fsmName && fsm.gameObject.name.StartsWith(s.objectPrefix))
        || (fsm.isActiveAndEnabled && IsPromptFsm(fsm))
        || IsPerformanceAudience(fsm);

    // an fsm is skipped only as long as what made it skippable lasts, e.g. a boss intro's conversation or roar is
    // skipped but not the fight that follows
    private static System.Func<bool> SkipUntil(PlayMakerFSM fsm)
    {
        if (IsSkippableThroughout(fsm)) return null;
        if (IsConversation(fsm) || IsHeldPrompt(fsm)) return () => HeroController.instance.acceptingInput;
        return () => !IsStunningRoar(fsm);
    }

    // reacts to the hero playing the needolin while she is, e.g. a gate whose locks open the longer she plays, or a choir
    // answering her melody
    private static bool IsPerformanceAudience(PlayMakerFSM fsm) =>
        HeroPerformanceRegion.IsPerforming
        && fsm.isActiveAndEnabled
        && fsm.FsmStates.Any(state => state.Actions.Any(a => a.Enabled && a is CheckHeroPerformanceRegionBase));

    private const string RoarExit = "ROAR EXIT";

    // a stunning roar locks the hero until some fsm sends ROAR EXIT, so skip that fsm until it does
    private static bool SkipRoarLock()
    {
        GameObject hero = HeroController.instance.gameObject;
        if (!IsRoarLocked(hero)) return false;

        foreach (PlayMakerFSM fsm in Object.FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None))
        {
            if (fsm.gameObject == hero || !fsm.isActiveAndEnabled || !SendsRoarExit(fsm, hero)) continue;

            SkipChain(fsm.Fsm, $"{FsmName(fsm)}: releases the roar lock", () => !IsRoarLocked(hero));
            if (!IsRoarLocked(hero)) return true;
        }
        return false;
    }

    private static bool IsRoarLocked(GameObject hero) => hero
        .GetComponents<PlayMakerFSM>()
        .Any(fsm => fsm.Fsm.ActiveState is { } state && state.Transitions.Any(t => t.EventName == RoarExit));

    private static bool SendsRoarExit(PlayMakerFSM fsm, GameObject hero) =>
        fsm.FsmStates.SelectMany(state => state.Actions).Any(a => a switch
        {
            SendEventByName send => send.sendEvent.Value == RoarExit && Targets(fsm, send.eventTarget, hero),
            SendEventByNameV2 send => send.sendEvent.Value == RoarExit && Targets(fsm, send.eventTarget, hero),
            // sends ROAR EXIT itself
            CallMethodProper call => call.behaviour.Value == nameof(HeroController) && call.methodName.Value == nameof(HeroController.StopRoarLock)
                && fsm.Fsm.GetOwnerDefaultTarget(call.gameObject) == hero,
            _ => false,
        });

    private static bool Targets(PlayMakerFSM fsm, FsmEventTarget target, GameObject go) =>
        target.target is FsmEventTarget.EventTarget.GameObject or FsmEventTarget.EventTarget.GameObjectFSM
        && fsm.Fsm.GetOwnerDefaultTarget(target.gameObject) == go;

    // AnimateUp only starts its animation in the next Update, where it would override an AnimateDown from the same frame
    [HarmonyPrefix]
    [HarmonyPatch(typeof(InvAnimateUpAndDown), nameof(InvAnimateUpAndDown.AnimateDown))]
    private static void DropPendingAnimateUp(InvAnimateUpAndDown __instance)
    {
        if (!skipping || !__instance.readyingAnimUp) return;

        __instance.readyingAnimUp = false;
        LogSkip($"{__instance.name}: dropped pending AnimateUp for AnimateDown");
    }

    // runs a shown message to its end within the frame, so its hide and afterMsg callback happen as on a real press
    private static bool SkipUIMsg()
    {
        if (!HasRoutines<UIMsgProxy>()) return false;

        LogSkip($"Skipping message {string.Join(", ", skipRoutines.Where(r => r.Owner is UIMsgProxy).Select(r => r.Owner.name))}");
        FastForwardRoutines<UIMsgProxy>();
        return true;
    }

    // an npc conversation holding the hero, e.g. waiting for its shop to close between lines
    private static bool IsConversation(PlayMakerFSM fsm) =>
        fsm.isActiveAndEnabled
        && !HeroController.instance.acceptingInput
        && fsm.FsmStates.Any(state => state.Actions.Any(a => a is RunDialogueBase));

    // a prompt in the world, e.g. to bind after a boss: one gameplay button continues while the hero is held
    private static bool IsHeldPrompt(PlayMakerFSM fsm) =>
        fsm.isActiveAndEnabled
        && !HeroController.instance.acceptingInput
        && !fsm.transform.IsChildOf(HeroController.instance.transform)
        && fsm.Fsm.ActiveState is { } state
        && IsButtonPrompt(state)
        && state.Actions.Any(a => a.Enabled && a is ListenForCast or ListenForJump or ListenForAttack);

    private static bool IsPromptFsm(PlayMakerFSM fsm) =>
        fsm.FsmStates.Any(state => state.Actions.OfType<ListenForPromptContinue>().Any());

    // nesting of fsm chains, so the log shows which fsm was skipped on behalf of which
    private static int skipDepth;

    // chain headers are only logged once something in the chain is, so idle fsms stay silent
    private static readonly List<string> pendingHeaders = [];

    private static void LogSkip(string message)
    {
        foreach (string header in pendingHeaders) WriteSkip(header);
        pendingHeaders.Clear();
        WriteSkip(new string(' ', 2 * skipDepth) + message);
    }

    private static void WriteSkip(string message)
    {
        DebugMod.Log(message);
        DebugMod.LogConsole(message);
    }

    private static string FsmName(PlayMakerFSM fsm) => $"{fsm.gameObject.name}/{fsm.FsmName}";

    private static string FsmName(Fsm fsm) => $"{fsm.GameObject.name}/{fsm.Name}";

    private static string Awaiting(Fsm fsm) =>
        fsm.ActiveState?.Transitions.Select(t => t.EventName).Distinct().ToList() is { Count: > 0 } events
            ? $"waiting on {string.Join(", ", events)}"
            : "nothing to skip";

    private static string Transition(Fsm fsm, FsmState from) =>
        fsm.ActiveState == from ? "" : $" → {fsm.ActiveStateName}";

    // Break: progressed up to a point that ends the whole request, e.g. a roar that takes a request of its own
    private enum SkipResult { Nothing, Refused, Done, Break }

    private const int MaxSkipSteps = 100;

    // keeps skipping until the fsm reaches something that can't be skipped, or until reached returns true
    private static SkipResult SkipChain(Fsm fsm, string header, System.Func<bool> reached = null)
    {
        int pendingIndex = pendingHeaders.Count;
        pendingHeaders.Add(new string(' ', 2 * skipDepth) + header);
        skipDepth++;
        try
        {
            bool progressed = false;
            bool startedInRoar = IsStunningRoar(fsm.ActiveState);
            for (int steps = 0; reached?.Invoke() != true; steps++)
            {
                if (steps == MaxSkipSteps)
                {
                    LogSkip($"{fsm.ActiveStateName}: stopped after {steps} steps, it seems to loop");
                    break;
                }

                SkipResult result = SkipStep(fsm);
                FinishStartedRoutines();
                if (result == SkipResult.Nothing)
                {
                    if (progressed) LogSkip($"{fsm.ActiveStateName}: end of chain, {Awaiting(fsm)}");
                    return progressed ? SkipResult.Done : SkipResult.Nothing;
                }
                if (result == SkipResult.Refused) return progressed ? SkipResult.Done : SkipResult.Refused;
                if (result == SkipResult.Break) return result;
                progressed = true;

                if (!startedInRoar && IsStunningRoar(fsm.ActiveState))
                {
                    LogSkip($"{fsm.ActiveStateName}: stunning roar, stopping");
                    return SkipResult.Break;
                }
            }
            return SkipResult.Done;
        }
        finally
        {
            skipDepth--;
            if (pendingHeaders.Count > pendingIndex) pendingHeaders.RemoveRange(pendingIndex, pendingHeaders.Count - pendingIndex);
        }
    }

    // running coroutines of the dialogue box, cinematics, scene preloads and messages, oldest first
    private static readonly List<SkipRoutine> skipRoutines = [];
    private static bool finishedDialogue;

    // bounds routines that wait on something that doesn't happen without time passing, e.g. while the game is paused
    private const int MaxRoutineSteps = 10_000;

    // a game coroutine that runs to its end within the frame while skipping, so everything it does along the way still
    // happens, just without waiting. One started while skipping first returns to its starter like any coroutine, whose
    // code after StartCoroutine (e.g. adding the input blocker the routine's end removes) must run before that end
    private sealed class SkipRoutine(MonoBehaviour owner, IEnumerator inner) : IEnumerator
    {
        public readonly MonoBehaviour Owner = owner;
        public readonly bool StartedWhileSkipping = skipping;
        public bool StartsVideo;
        private readonly IEnumerator inner = inner;
        private bool done;

        public object Current => inner.Current;

        public override string ToString() => $"{Owner.name} {inner.GetType().Name}";

        public bool MoveNext() => !done && Step();

        public void Reset() => throw new System.NotSupportedException();

        public void Finish()
        {
            if (Owner is DialogueBox) finishedDialogue = true;
            for (int steps = 0; ; steps++)
            {
                if (steps == MaxRoutineSteps) throw new System.InvalidOperationException($"{inner} did not finish after {steps} steps");
                if (Owner is DialogueBox box)
                {
                    box.waitingToAdvance = false;
                    box.currentRevealSpeed = 100_000f;
                }
                if (Owner is UIMsgProxy) PressToContinue();
                if (!(StartsVideo ? StepStartingVideo() : Step()) || done) return;

                switch (inner.Current)
                {
                    case SkipRoutine nested:
                        nested.Finish();
                        break;
                    // the routines it started since
                    case Coroutine:
                        while (skipRoutines[^1] != this) skipRoutines[^1].Finish();
                        break;
                    case WaitForSeconds when Owner is CinematicPlayer { actCard: { } card } && card.isActiveAndEnabled:
                        // the act card stays up until its animation has played
                        card.Update(0f);
                        AnimatorStateInfo info = card.GetCurrentAnimatorStateInfo(0);
                        card.Update(info.length * Mathf.Max(0f, 1f - info.normalizedTime));
                        break;
                    case null or WaitForSeconds:
                        break;
                    default:
                        throw new System.InvalidOperationException($"can't skip {inner} waiting on {inner.Current}");
                }
            }
        }

        // a message waits for a press of its own
        private void PressToContinue()
        {
            FieldInfo wasPressed = inner.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .SingleOrDefault(f => f.Name.Contains("wasPressed"))
                ?? throw new System.MissingFieldException(inner.GetType().FullName, "wasPressed");
            wasPressed.SetValue(inner, true);
        }

        // the video engine only starts playing on a later frame, which a skipped video doesn't need
        private bool StepStartingVideo()
        {
            startingVideo = ((CinematicPlayer)Owner).cinematicVideoPlayer;
            try
            {
                return Step();
            }
            finally
            {
                startingVideo = null;
            }
        }

        private bool Step()
        {
            if (inner.MoveNext()) return true;
            done = true;
            skipRoutines.Remove(this);
            return false;
        }

        public void Stop()
        {
            done = true;
            skipRoutines.Remove(this);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.RunDialogue))]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.PrintText))]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.LineEndedWait))]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.Close))]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.CloseAndEnd))]
    [HarmonyPatch(typeof(CinematicPlayer), nameof(CinematicPlayer.StartVideo))]
    [HarmonyPatch(typeof(CinematicPlayer), nameof(CinematicPlayer.Skip))]
    [HarmonyPatch(typeof(CinematicPlayer), nameof(CinematicPlayer.FinishInGameVideo))]
    // registers its load only on the next frame, too late for a transition the skip starts right away
    [HarmonyPatch(typeof(ScenePreloader), nameof(ScenePreloader.LoadRoutine))]
    [HarmonyPatch(typeof(UIMsgBase<PowerUpGetMsg.PowerUps>), nameof(UIMsgBase<PowerUpGetMsg.PowerUps>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItemSkill>), nameof(UIMsgBase<ToolItemSkill>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolCrest>), nameof(UIMsgBase<ToolCrest>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItem>), nameof(UIMsgBase<ToolItem>.DoMsg))]
    [HarmonyPatch(typeof(UIMsgBase<ToolItemType>), nameof(UIMsgBase<ToolItemType>.DoMsg))]
    private static void TrackSkipRoutine(MonoBehaviour __instance, MethodBase __originalMethod, ref IEnumerator __result)
    {
        SkipRoutine routine = new(__instance, __result) { StartsVideo = __originalMethod.Name == nameof(CinematicPlayer.StartVideo) };
        skipRoutines.Add(routine);
        __result = routine;
    }

    private static TeamCherry.Cinematics.CinematicVideoPlayer startingVideo;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(TeamCherry.Cinematics.EmbeddedCinematicVideoPlayer), nameof(TeamCherry.Cinematics.EmbeddedCinematicVideoPlayer.IsPlaying), MethodType.Getter)]
    private static bool AssumeStartedVideo(TeamCherry.Cinematics.EmbeddedCinematicVideoPlayer __instance, ref bool __result)
    {
        if (__instance != startingVideo) return true;
        __result = true;
        return false;
    }

    // a new conversation stops all of the box's coroutines
    [HarmonyPrefix]
    [HarmonyPatch(typeof(DialogueBox), nameof(DialogueBox.ShowShared))]
    private static void DropDialogueRoutines(DialogueBox __instance)
    {
        foreach (SkipRoutine routine in skipRoutines.Where(r => r.Owner == __instance).ToList()) routine.Stop();
    }

    private static bool HasRoutines<T>(System.Func<T, bool> owner = null)
    {
        DropStoppedRoutines();
        return skipRoutines.Any(r => r.Owner is T o && (owner == null || owner(o)));
    }

    // innermost first, so a routine resumes only once what it waits on has finished
    private static void FastForwardRoutines<T>(System.Func<T, bool> owner = null)
    {
        DropStoppedRoutines();
        while (skipRoutines.LastOrDefault(r => r.Owner is T o && (owner == null || owner(o))) is { } routine) routine.Finish();
    }

    private static void FinishStartedRoutines()
    {
        DropStoppedRoutines();
        while (skipRoutines.LastOrDefault(r => r.StartedWhileSkipping) is { } routine)
        {
            LogSkip($"running {routine} to its end");
            routine.Finish();
        }
    }

    // unity stops an object's coroutines when it is deactivated or destroyed, e.g. with its scene
    private static void DropStoppedRoutines()
    {
        foreach (SkipRoutine routine in skipRoutines.Where(r => !r.Owner || !r.Owner.gameObject.activeInHierarchy).ToList()) routine.Stop();
    }

    private static void FastForwardDialogue()
    {
        LogSkip("Fast-forwarding dialogue");
        FastForwardRoutines<DialogueBox>();
        LogSkip(DialogueBox._instance.isDialogueRunning ? "Dialogue lines done, conversation still open" : "Dialogue ended");
    }

    // something a state waits on: a timed wait ends after Remaining seconds of Advance, an event wait when Resolve makes its event happen
    private abstract class StateWait(string name)
    {
        public readonly string Name = name;
    }

    private sealed class TimedWait(string name, float remaining, System.Action<float> advance) : StateWait(name)
    {
        public readonly float Remaining = remaining;
        public readonly System.Action<float> Advance = advance;
    }

    private sealed class EventWait(string name, System.Func<SkipResult> resolve) : StateWait(name)
    {
        public readonly System.Func<SkipResult> Resolve = resolve;
    }

    // timers compare with < or >=, so a step lands just past the end
    private const float StepEpsilon = 0.001f;

    // an event sent from inside another fsm's update only queues the switch for the receiver's next update, and further
    // events are dropped until then; a skip delivers events faster than updates run, so apply them before every step
    private static void ApplyPendingSwitches(Fsm current)
    {
        // sub fsms run by RunFSM have no component of their own
        foreach (Fsm fsm in PlayMakerFSM.FsmList.Where(c => c).Select(c => c.Fsm).Append(current).Where(f => f != null).Distinct().ToList())
        {
            if (!fsm.IsSwitchingState) continue;
            FsmState state = fsm.ActiveState;
            fsm.UpdateStateChanges();
            LogSkip($"{FsmName(fsm)}: applied pending switch{Transition(fsm, state)}");
        }
    }

    private static SkipResult SkipStep(Fsm fsm)
    {
        ApplyPendingSwitches(fsm);
        FsmState state = fsm.ActiveState;
        if (state == null) return SkipResult.Nothing;
        string name = state.Name;
        if (RedeliverEarlySends(fsm, state)) return SkipResult.Break;

        // a sequence only has entered the actions up to its current one
        var entered = (state.IsSequence ? state.Actions.Take(state.ActiveActionIndex + 1) : state.Actions)
            .Where(a => a.Enabled);
        List<StateWait> waits = [];
        List<System.Action<float>> sideEffects = [];
        List<string> unhandled = [];
        foreach (FsmStateAction action in entered)
        {
            if (!action.Finished && ActionWait(fsm, action, name) is { } wait)
            {
                waits.Add(wait);
            }
            else if (SideEffect(action) is { } advance)
            {
                sideEffects.Add(advance);
            }
            else if (!action.Finished)
            {
                unhandled.Add(action.GetType().Name);
            }
        }
        waits.AddRange(fsm.DelayedEvents
            .Where(e => !e.Finished)
            .Select(e => new TimedWait($"delayed {e.FsmEvent?.Name} → {DescribeTarget(e)}", e.Timer, dt => e.timer -= dt)));
        AddAnimatorClocks(fsm, entered, waits, sideEffects);
        waits.AddRange(TransitionWaits(fsm, state, name));

        List<TimedWait> timed = waits.OfType<TimedWait>().ToList();
        // timers that ran out act on their next update, so they can be slightly overdue
        float step = Mathf.Max(0f, timed.Select(w => w.Remaining).DefaultIfEmpty(float.PositiveInfinity).Min());
        if (!float.IsPositiveInfinity(step) && unhandled.Count == 0)
        {
            string due = string.Join(", ", timed.Where(w => w.Remaining <= step).Select(w => w.Name).Distinct());
            AdvanceState(fsm, state, step + StepEpsilon, timed, sideEffects);
            LogSkip($"{name}: advanced {step:0.###}s ({due}){Transition(fsm, state)}");
            return SkipResult.Done;
        }

        // an action finished from outside an update, e.g. by a message's callback, only ends the state on its next update
        if (timed.Count == 0 && unhandled.Count == 0 && !state.finished && entered.Any() && entered.All(a => a.Finished))
        {
            fsm.Update();
            LogSkip($"{name}: actions finished{Transition(fsm, state)}");
            if (fsm.ActiveState != state) return SkipResult.Done;
        }

        SkipResult result = SkipResult.Nothing;
        foreach (EventWait wait in waits.OfType<EventWait>())
        {
            SkipResult resolved = wait.Resolve();
            if (resolved is SkipResult.Done or SkipResult.Break) return resolved;
            if (resolved == SkipResult.Refused) result = resolved;
        }
        if (result != SkipResult.Nothing) return result;

        if (!float.IsPositiveInfinity(step))
        {
            LogSkip($"{name}: stuck, {string.Join(", ", timed.Select(w => w.Name))} blocked by unhandled {string.Join(", ", unhandled)}");
            return SkipResult.Refused;
        }
        if (timed.Count > 0)
        {
            LogSkip($"{name}: stuck, waiting on {string.Join(", ", timed.Select(w => w.Name))} which never end");
            return SkipResult.Refused;
        }
        if (unhandled.Count > 0) LogSkip($"{name}: unhandled {string.Join(", ", unhandled)}");
        return SkipResult.Nothing;
    }

    // moves every clock in the state forward, then runs a regular fsm update so the actions themselves react
    private static void AdvanceState(Fsm fsm, FsmState state, float dt, List<TimedWait> timed, List<System.Action<float>> sideEffects)
    {
        foreach (System.Action<float> advance in sideEffects) advance(dt);
        foreach (TimedWait wait in timed) wait.Advance(dt);
        AdvanceJitterChanges(dt);
        if (fsm.ActiveState != state) return;

        state.StateTime += dt;
        fsm.Update();
    }

    // jitters start and stop after a delay in game time, e.g. a choir, which a stop before its start would miss
    private static void AdvanceJitterChanges(float dt)
    {
        foreach (JitterSelf jitter in Object.FindObjectsByType<JitterSelf>(FindObjectsSortMode.None))
        {
            if (!jitter.isChangeQueued) continue;
            jitter.queueEndTime -= dt;
            if (jitter.queueEndTime <= Time.timeAsDouble) jitter.BatchedUpdate();
        }
    }

    private static StateWait ActionWait(Fsm fsm, FsmStateAction action, string name) => action switch
    {
        Wait w => new TimedWait("Wait", w.time.Value - w.timer, dt =>
        {
            if (w.realTime) w.startTime -= dt;
            else w.timer += dt;
        }),
        WaitRandom w => new TimedWait("WaitRandom", w.time - w.timer, dt =>
        {
            if (w.realTime) w.startTime -= dt;
            else w.timer += dt;
        }),
        SendEventToRegisterDelay s => new TimedWait($"send {s.EventName.Value}", s.timer, dt => s.timer -= dt),
        ActivateGameObjectDelay a => new TimedWait("ActivateGameObjectDelay", a.timer, dt => a.timer -= dt),
        AudioPlayerOneShotSingle a => new TimedWait("audio", a.delay.Value - a.timer, dt => a.timer += dt),
        AudioPlayerOneShot a => new TimedWait("audio", a.delay.Value - a.timer, dt => a.timer += dt),
        StartRoarEmitter r => new TimedWait("roar", r.delay.Value - r.timer, dt => r.timer += dt),
        StartRoarEmitterV2 r => new TimedWait("roar", r.delay.Value - r.timer, dt => r.timer += dt),
        CameraBlurPlaneFade b => new TimedWait("blur fade", b.Duration.Value - b.State.StateTime, _ => { }),
        PlayTimeline p => new TimedWait($"timeline {p.timeline.name}", (float)(p.timeline.duration - p.timeline.time), dt =>
        {
            p.timeline.time += dt;
            p.timeline.Evaluate();
        }),
        ListenForPromptContinue p => new EventWait("prompt", () =>
        {
            FsmState state = fsm.ActiveState;
            fsm.Event(p.ContinuePressed);
            LogSkip($"{name}: continued prompt{Transition(fsm, state)}");
            return SkipResult.Done;
        }),
        NextFrameEvent n => new EventWait("next frame", () =>
        {
            FsmState state = fsm.ActiveState;
            n.Finish();
            fsm.Event(n.sendEvent);
            LogSkip($"{name}: skipped a frame{Transition(fsm, state)}");
            return SkipResult.Done;
        }),
        RunFSMAction r when r.runFsm != null => SubFsmWait(fsm, r.runFsm, name),
        ListenForMenuActions l => PressWait(fsm, name, "menu cancel", l.cancelPressed),
        // its OnUpdate only sends when the delay crosses zero, which a step past it would skip
        CheckHeroPerformanceRegionBase c when c.delay > 0f => new TimedWait("performance reaction", c.delay, dt =>
        {
            c.delay -= dt;
            if (c.delay <= 0f) c.SendEvents(c.affectedState);
        }),
        SpawnSkillGetMsg => new EventWait("skill message", () => SkipUIMsg() ? SkipResult.Done : SkipResult.Nothing),
        SpawnPowerUpGetMsg => new EventWait("power-up message", () => SkipUIMsg() ? SkipResult.Done : SkipResult.Nothing),
        ListenForAnimationEvent l => AnimationEventWait(fsm.GetOwnerDefaultTarget(l.Target)),
        ListenForCast l => PressWait(fsm, name, "cast", l.wasPressed),
        ListenForJump l => PressWait(fsm, name, "jump", l.wasPressed),
        ListenForAttack l => PressWait(fsm, name, "attack", l.wasPressed),
        Tk2dWatchAnimationEvents a => AnimationWait(fsm, a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent != null),
        Tk2dWatchAnimationEventsV3 a => AnimationWait(fsm, a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent != null),
        Tk2dPlayAnimationWithEvents a => AnimationWait(fsm, a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent != null),
        Tk2dPlayAnimationWithEventsV3 a => AnimationWait(fsm, a.gameObject, a.animationTriggerEvent, a.animationCompleteEvent != null),
        Tk2dPlayAnimationWait a => AnimationWait(fsm, a.Target, null, true),
        WaitBool w => new TimedWait("WaitBool", w.time.Value - w.timer, dt =>
        {
            if (w.realTime) w.startTime -= dt;
            else w.timer += dt;
        }),
        EaseFsmAction e => EaseWait(e),
        FadeAudio f => FadeAudioWait(f),
        DoCameraShakeV4 c => new TimedWait("camera shake", c.delayLeft, dt =>
        {
            if (c.delayLeft > 0f && (c.delayLeft -= dt) <= 0f) c.DoShake();
        }),
        AnimatorPlayStateWait a => AnimatorStateWait(a),
        CheckYPosition c => HeightWait(fsm, c),
        _ => DelayedEventWait(action),
    };

    // RunFSM leaves the state once its sub fsm stops, which it notices on the next update
    private static StateWait SubFsmWait(Fsm fsm, Fsm sub, string name) => new EventWait($"sub fsm {sub.Name}", () =>
    {
        FsmState state = fsm.ActiveState;
        if (SkipChain(sub, $"{name}: running sub fsm {FsmName(sub)}", () => sub.Finished) == SkipResult.Break) return SkipResult.Break;
        if (!sub.Finished) return SkipResult.Refused;

        fsm.Update();
        LogSkip($"{name}: sub fsm finished{Transition(fsm, state)}");
        return SkipResult.Done;
    });

    private static StateWait PressWait(Fsm fsm, string name, string button, FsmEvent wasPressed) =>
        wasPressed == null || !IsButtonPrompt(fsm.ActiveState) ? null : new EventWait($"{button} press", () =>
        {
            FsmState state = fsm.ActiveState;
            fsm.Event(wasPressed);
            LogSkip($"{name}: pressed {button}{Transition(fsm, state)}");
            return SkipResult.Done;
        });

    // "press any of these to continue": all input listeners lead to one event, while controls map buttons to different ones
    private static bool IsButtonPrompt(FsmState state) => state.Actions
        .Where(a => a.Enabled && a.GetType().Name.StartsWith("ListenFor"))
        .SelectMany(a => FieldsOfType(a.GetType(), typeof(FsmEvent)).Select(f => (FsmEvent)f.GetValue(a)))
        .Where(e => !string.IsNullOrEmpty(e?.Name))
        .Distinct()
        .Count() == 1;

    private static readonly Dictionary<(System.Type, System.Type), FieldInfo[]> fieldsByType = [];

    private static FieldInfo[] FieldsOfType(System.Type type, System.Type fieldType)
    {
        if (!fieldsByType.TryGetValue((type, fieldType), out FieldInfo[] fields))
        {
            fieldsByType[(type, fieldType)] = fields = type
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => f.FieldType == fieldType)
                .ToArray();
        }
        return fields;
    }

    // an action whose pending work is a delayed event of its fsm, which is advanced as its own wait
    private static TimedWait DelayedEventWait(FsmStateAction action)
    {
        DelayedEvent pending = FieldsOfType(action.GetType(), typeof(DelayedEvent))
            .Select(f => (DelayedEvent)f.GetValue(action))
            .SingleOrDefault(e => e != null);
        if (pending == null) return null;
        // once sent, the action finishes on its next update
        return new TimedWait($"{action.GetType().Name} {pending.FsmEvent?.Name}", pending.Finished ? 0f : pending.Timer, _ => { });
    }

    private static IEnumerable<GameObject> ReferencedObjects(Fsm fsm, FsmStateAction action) =>
        FieldsOfType(action.GetType(), typeof(FsmGameObject)).Select(f => ((FsmGameObject)f.GetValue(action))?.Value)
            .Concat(FieldsOfType(action.GetType(), typeof(FsmOwnerDefault)).Select(f => fsm.GetOwnerDefaultTarget((FsmOwnerDefault)f.GetValue(action))));

    // animators the state works with keep running over the skipped time and fire their animation events
    private static void AddAnimatorClocks(Fsm fsm, IEnumerable<FsmStateAction> entered, List<StateWait> waits, List<System.Action<float>> sideEffects)
    {
        IEnumerable<Animator> animators = entered
            .SelectMany(a => ReferencedObjects(fsm, a))
            .Where(go => go)
            .Select(go => go.GetComponent<Animator>())
            .Where(a => a && a.isActiveAndEnabled)
            .Distinct();
        foreach (Animator animator in animators)
        {
            float remaining = NextAnimationEvent(animator, out string eventName);
            if (float.IsPositiveInfinity(remaining)) sideEffects.Add(dt => animator.Update(dt));
            else waits.Add(new TimedWait($"{animator.name} animation event {eventName}", remaining, dt => animator.Update(dt)));
        }
    }

    // fired by the target's animator, whose clock advances it
    private static TimedWait AnimationEventWait(GameObject target)
    {
        Animator animator = target ? target.GetComponent<Animator>() : null;
        if (!animator || !animator.isActiveAndEnabled) return null;

        // a state played this frame only becomes the current one with the animator's next update
        animator.Update(0f);
        float remaining = NextAnimationEvent(animator, out string eventName);
        return float.IsPositiveInfinity(remaining) ? null : new TimedWait($"{animator.name} animation event {eventName}", remaining, _ => { });
    }

    // seconds until the next animation event of a clip that plays once
    private static float NextAnimationEvent(Animator animator, out string eventName)
    {
        eventName = null;
        AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
        if (clips.Length == 0 || clips[0].clip.isLooping) return float.PositiveInfinity;

        AnimationClip clip = clips[0].clip;
        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        float time = info.normalizedTime * clip.length;
        AnimationEvent next = clip.events.Where(e => e.time > time).OrderBy(e => e.time).FirstOrDefault();
        if (next == null) return float.PositiveInfinity;

        eventName = next.functionName;
        return (next.time - time) / clip.length * info.length;
    }

    // resumes once game time passes the length of the animator state, which the action records on its second update
    private static TimedWait AnimatorStateWait(AnimatorPlayStateWait wait) =>
        wait.resumeTime is { } resume
            ? new TimedWait($"{wait.animator.name} state", (float)(resume - Time.timeAsDouble), dt => wait.resumeTime -= dt)
            : new TimedWait("AnimatorPlayStateWait starting", 0f, _ => { });

    private static string DescribeTarget(DelayedEvent delayed) => delayed.eventTarget is not { } target
        ? "self"
        : target.target switch
        {
            FsmEventTarget.EventTarget.GameObject or FsmEventTarget.EventTarget.GameObjectFSM =>
                delayed.fsm.GetOwnerDefaultTarget(target.gameObject) is { } go ? go.name : "nothing",
            FsmEventTarget.EventTarget.FSMComponent => target.fsmComponent ? FsmName(target.fsmComponent) : "nothing",
            _ => target.target.ToString(),
        };

    // the ease's delay, then its running time scaled by speed
    private static TimedWait EaseWait(EaseFsmAction ease)
    {
        string name = ease.GetType().Name;
        if (ease.realTime) return new TimedWait($"{name} in real time", float.PositiveInfinity, _ => { });

        float rate = ease.speed.IsNone ? 1f : ease.speed.Value;
        float delay = ease.start && !ease.isRunning ? Mathf.Max(0f, ease.delayTime) : 0f;
        // a finished ease still needs an update to finish the action
        float remaining = ease.finishAction ? 0f : delay + (ease.time.Value - ease.runningTime) / rate;
        return new TimedWait(name, remaining, dt =>
        {
            if (ease.start && !ease.isRunning)
            {
                ease.delayTime -= dt;
                if (ease.delayTime >= 0f) return;

                // what OnUpdate does once the delay is over, with the overshoot as running time
                dt = -ease.delayTime;
                ease.isRunning = true;
                ease.start = false;
            }
            if (ease.isRunning) ease.runningTime += dt * rate;
        });
    }

    // the volume moves linearly from start to end over time
    private static TimedWait FadeAudioWait(FadeAudio fade)
    {
        if (!fade.audio) return new TimedWait("FadeAudio without audio", float.PositiveInfinity, _ => { });

        float span = fade.endVolume.Value - fade.startVolume.Value;
        float remaining = Mathf.Approximately(span, 0f) ? 0f : (fade.endVolume.Value - fade.GetVolume()) / span * fade.time.Value;
        return new TimedWait("FadeAudio", remaining, dt => fade.SetVolume(fade.GetVolume() + span * dt / fade.time.Value));
    }

    // the object crossing the compared height at its current vertical speed, ignoring gravity and collisions
    private static TimedWait HeightWait(Fsm fsm, CheckYPosition check)
    {
        GameObject go = fsm.GetOwnerDefaultTarget(check.gameObject);
        string name = $"{(go ? go.name : "nothing")} reaching y";
        Rigidbody2D body = go ? go.GetComponent<Rigidbody2D>() : null;
        if (!body || check.space != Space.World || (!check.activeBool.IsNone && !check.activeBool.Value))
        {
            return new TimedWait(name, float.PositiveInfinity, _ => { });
        }

        float distance = check.compareTo.Value + check.compareToOffset.Value - go.transform.position.y;
        float speed = body.linearVelocity.y;
        FsmEvent crossing = speed < 0 ? check.lessThan : check.greaterThan;
        float remaining = distance * speed > 0 && crossing != null
            ? Mathf.Max(0f, (Mathf.Abs(distance) - check.tolerance.Value) / Mathf.Abs(speed))
            : float.PositiveInfinity;
        return new TimedWait(name, remaining, dt => go.transform.position += (Vector3)(body.linearVelocity * dt));
    }

    // the watched clip's next trigger frame, or its end
    private static TimedWait AnimationWait(Fsm fsm, FsmOwnerDefault target, FsmEvent triggerEvent, bool untilEnd)
    {
        tk2dSpriteAnimator animator = fsm.GetOwnerDefaultTarget(target)?.GetComponent<tk2dSpriteAnimator>();
        if (!animator || !animator.Playing) return new TimedWait("stopped animation", float.PositiveInfinity, _ => { });

        tk2dSpriteAnimationClip clip = animator.CurrentClip;
        int current = (int)animator.clipTime;
        int? frame = triggerEvent == null
            ? null
            : Enumerable.Range(current + 1, clip.frames.Length - 1 - current).Cast<int?>().FirstOrDefault(i => clip.frames[i.Value].triggerEvent);
        if (frame == null && untilEnd && clip.wrapMode == tk2dSpriteAnimationClip.WrapMode.Once)
        {
            frame = clip.frames.Length;
        }

        float remaining = frame == null ? float.PositiveInfinity : (frame.Value - animator.clipTime) / animator.clipFps;
        return new TimedWait($"animation {clip.name}", remaining, dt => animator.UpdateAnimation(dt));
    }

    // effects that keep running over the state's time without ever ending it
    private static System.Action<float> SideEffect(FsmStateAction action) => action switch
    {
        DecelerateXY d => dt => Decelerate(d, dt),
        // reapplied by the regular update, nothing builds up over time
        SetScale { everyFrame: true } or SetRotation { everyFrame: true } or AudioPlayInState or ObjectJitter => _ => { },
        // conditions on the current world, re-checked by the regular update
        CheckHeroPerformanceRegionBase or CheckAlertRangeByName => _ => { },
        ScreenFader f => dt => AdvanceScreenFade(f.State.StateTime, dt, f.duration.Value, f.endColour.Value),
        ScreenFaderAlpha f => dt => AdvanceScreenFade(f.State.StateTime, dt, f.Duration.Value, ScreenFaderUtils.GetColour() with { a = f.EndAlpha.Value }),
        _ => null,
    };

    // the per-physics-step deceleration compounded over dt, without the movement it would cause
    private static void Decelerate(DecelerateXY decelerate, float dt)
    {
        if (!decelerate.rb2d) return;
        float steps = dt / Time.fixedDeltaTime;
        Vector2 velocity = decelerate.rb2d.linearVelocity;
        if (!decelerate.decelerationX.IsNone) velocity.x *= Mathf.Pow(decelerate.decelerationX.Value, steps);
        if (!decelerate.decelerationY.IsNone) velocity.y *= Mathf.Pow(decelerate.decelerationY.Value, steps);
        decelerate.rb2d.linearVelocity = velocity;
    }

    // the fade started on state enter and eases linearly, so its colour after dt can be interpolated from now
    private static void AdvanceScreenFade(float now, float dt, float duration, Color target)
    {
        if (now >= duration) return;
        if (now + dt >= duration)
        {
            ScreenFaderUtils.SetColour(target);
            return;
        }

        Color atEnd = Color.Lerp(ScreenFaderUtils.GetColour(), target, dt / (duration - now));
        ScreenFaderUtils.Fade(atEnd, target, duration - now - dt);
    }

    // events from outside the fsm that the state transitions on
    private static IEnumerable<EventWait> TransitionWaits(Fsm fsm, FsmState state, string name)
    {
        HashSet<string> awaited = state.Transitions.Select(t => t.EventName).ToHashSet();
        foreach (string eventName in awaited.Where(eventSourceFastForwards.ContainsKey))
        {
            yield return new EventWait(eventName, () => FastForwardEventSource(fsm, state, eventName, name));
        }
        // a running dialogue ends the state once it is fast-forwarded, no other fsm has to send anything
        if (DialogueBox._instance is { isDialogueRunning: true } && state.Actions.Any(a => a is RunDialogueBase)) yield break;
        yield return new EventWait("register senders", () => FastForwardRegisterSenders(fsm, state, awaited, name));
    }

    // events sent by game systems outside the fsm, with a way to run that system up to sending it
    private static readonly Dictionary<string, System.Func<string>> eventSourceFastForwards = new()
    {
        ["REGENERATED SILK CHUNK"] = FastForwardSilkRegen,
        ["GATE LOCKS PRE COMPLETE"] = FastForwardGateLock,
        ["GATE LOCKS COMPLETE"] = FastForwardGateLock,
        ["CINEMATIC START"] = FastForwardCinematicStart,
    };

    private static string FastForwardCinematicStart()
    {
        if (!HasRoutines<CinematicPlayer>(IsTriggeredCinematic)) return "no cinematic is starting";
        FastForwardRoutines<CinematicPlayer>(IsTriggeredCinematic);
        return null;
    }

    private static SkipResult FastForwardEventSource(Fsm fsm, FsmState state, string eventName, string name)
    {
        string error = eventSourceFastForwards[eventName]();
        if (error == null && fsm.ActiveState == state && !fsm.IsSwitchingState) error = "the event did not leave the state";
        LogSkip(error == null
            ? $"{name}: fast-forwarded {eventName}{Transition(fsm, state)}"
            : $"{name}: stuck, cannot fast-forward {eventName}: {error}");
        return error == null ? SkipResult.Done : SkipResult.Refused;
    }

    // fsms currently waiting on register senders, never picked as a sender themselves
    private static readonly HashSet<Fsm> waitingOnSenders = [];

    // skips other fsms that broadcast an event this state waits for, until one of them makes it leave
    private static SkipResult FastForwardRegisterSenders(Fsm fsm, FsmState state, HashSet<string> awaited, string name)
    {
        List<(PlayMakerFSM sender, string[] events)> senders = Object.FindObjectsByType<PlayMakerFSM>(FindObjectsSortMode.None)
            .Where(s => s.Fsm != fsm && s.isActiveAndEnabled && !waitingOnSenders.Contains(s.Fsm))
            .Select(s => (s, SentEvents(s, fsm).Where(awaited.Contains).Distinct().ToArray()))
            .Where(s => s.Item2.Length > 0 && SendsUnprompted(s.Item1.Fsm, s.Item2, fsm))
            .ToList();
        if (senders.Count == 0) return SkipResult.Nothing;

        waitingOnSenders.Add(fsm);
        try
        {
            foreach ((PlayMakerFSM sender, string[] events) in senders)
            {
                string header = $"{name}: waiting on {string.Join(", ", events)}, skipping sender {FsmName(sender)}";
                if (SkipChain(sender.Fsm, header, () => fsm.ActiveState != state) == SkipResult.Break) return SkipResult.Break;
                if (fsm.ActiveState != state)
                {
                    LogSkip($"{name}: received from {FsmName(sender)}{Transition(fsm, state)}");
                    return SkipResult.Done;
                }
            }
        }
        finally
        {
            waitingOnSenders.Remove(fsm);
        }

        LogSkip($"{name}: stuck, none of the {senders.Count} senders made it leave");
        return SkipResult.Refused;
    }

    // states whose targeted sends were checked this request
    private static readonly HashSet<(Fsm, FsmState)> checkedSends = [];

    // a skip can send an event before its receiver got to the state that handles it, where it is dropped; without the
    // skip it would have arrived later, so run the receiver to that state and send it again; true on a break
    private static bool RedeliverEarlySends(Fsm sender, FsmState state)
    {
        if (!checkedSends.Add((sender, state))) return false;

        foreach (FsmStateAction action in state.Actions.Where(a => a.Enabled))
        {
            (FsmEventTarget target, string eventName) = action switch
            {
                SendEventByName send => (send.eventTarget, send.sendEvent.Value),
                SendEventByNameV2 send => (send.eventTarget, send.sendEvent.Value),
                _ => (null, null),
            };
            if (target == null || string.IsNullOrEmpty(eventName)) continue;

            foreach (Fsm receiver in TargetedFsms(sender, target))
            {
                if (receiver == sender || Handles(receiver, receiver.ActiveState, eventName)) continue;
                if (!ReachesHandlerUnprompted(receiver, eventName)) continue;

                if (SkipChain(receiver, $"{state.Name}: {FsmName(receiver)} wasn't ready for {eventName}",
                    () => Handles(receiver, receiver.ActiveState, eventName)) == SkipResult.Break) return true;
                if (!Handles(receiver, receiver.ActiveState, eventName)) continue;

                FsmState before = receiver.ActiveState;
                receiver.Event(FsmEvent.GetFsmEvent(eventName));
                LogSkip($"{FsmName(receiver)}: redelivered {eventName}{Transition(receiver, before)}");
            }
        }
        return false;
    }

    private static IEnumerable<Fsm> TargetedFsms(Fsm sender, FsmEventTarget target)
    {
        switch (target.target)
        {
            case FsmEventTarget.EventTarget.GameObject:
            case FsmEventTarget.EventTarget.GameObjectFSM:
                GameObject go = sender.GetOwnerDefaultTarget(target.gameObject);
                if (!go) return [];
                return PlayMakerFSM.FsmList
                    .Where(c => c && c.gameObject == go
                        && (target.target == FsmEventTarget.EventTarget.GameObject || c.FsmName == target.fsmName.Value))
                    .Select(c => c.Fsm)
                    .ToList();
            case FsmEventTarget.EventTarget.FSMComponent:
                return target.fsmComponent ? [target.fsmComponent.Fsm] : [];
            default:
                return [];
        }
    }

    private static bool Handles(Fsm fsm, FsmState state, string eventName) =>
        state != null && (state.Transitions.Any(t => t.EventName == eventName) || fsm.GlobalTransitions.Any(t => t.EventName == eventName));

    // only over FINISHED, i.e. states the fsm gets through on its own given time
    private static bool ReachesHandlerUnprompted(Fsm fsm, string eventName)
    {
        HashSet<FsmState> seen = [];
        for (FsmState state = fsm.ActiveState; state != null && seen.Add(state);)
        {
            if (Handles(fsm, state, eventName)) return true;
            FsmTransition finished = state.Transitions.FirstOrDefault(t => t.EventName == FsmEvent.Finished.Name);
            state = finished == null ? null : fsm.GetState(finished.ToState);
        }
        return false;
    }

    private static IEnumerable<string> SentEvents(PlayMakerFSM sender, Fsm receiver) =>
        sender.FsmStates.SelectMany(state => state.Actions).Select(a => SentEvent(sender.Fsm, a, receiver)).Where(e => e != null);

    // the event the action sends to everyone, or to the receiver in particular
    private static string SentEvent(Fsm sender, FsmStateAction action, Fsm receiver) => action switch
    {
        SendEventToRegister send => send.eventName.Value,
        SendEventToRegisterDelay send => send.EventName.Value,
        SendEventByName { eventTarget.target: FsmEventTarget.EventTarget.BroadcastAll } send => send.sendEvent.Value,
        SendEventByName send when TargetedFsms(sender, send.eventTarget).Contains(receiver) => send.sendEvent.Value,
        SendEventByNameV2 send when TargetedFsms(sender, send.eventTarget).Contains(receiver) => send.sendEvent.Value,
        _ => null,
    };

    // whether the fsm gets from its current state to sending one of the events without the player deciding something;
    // button prompts don't stop it since the skip presses them
    private static bool SendsUnprompted(Fsm fsm, string[] events, Fsm receiver)
    {
        HashSet<FsmState> seen = [];
        Queue<FsmState> queue = new();
        if (fsm.ActiveState != null) queue.Enqueue(fsm.ActiveState);
        while (queue.Count > 0)
        {
            FsmState state = queue.Dequeue();
            if (!seen.Add(state)) continue;
            if (state.Actions.Any(a => a.Enabled && events.Contains(SentEvent(fsm, a, receiver)))) return true;

            bool playerControlled = state.Actions.Any(a => a.Enabled && a.GetType().Name.StartsWith("ListenFor")) && !IsButtonPrompt(state);
            if (playerControlled) continue;
            foreach (FsmTransition transition in state.Transitions)
            {
                if (fsm.GetState(transition.ToState) is { } next) queue.Enqueue(next);
            }
        }
        return false;
    }

    // the gate lock's play routine, taken over from its coroutine and driven by skips from then on
    private static (BellShrineGateLock gateLock, IEnumerator routine) drivenGateLock;

    // runs BellShrineGateLock.PlayRoutine, the delays between its bells, up to the next event it sends
    private static string FastForwardGateLock()
    {
        BellShrineGateLock gateLock = Object.FindAnyObjectByType<BellShrineGateLock>();
        if (!gateLock) return "no gate lock in the scene";
        if (drivenGateLock.gateLock != gateLock)
        {
            if (gateLock.playRoutine == null) return "the gate lock isn't playing";
            gateLock.StopCoroutine(gateLock.playRoutine);
            drivenGateLock = (gateLock, gateLock.PlayRoutine());
        }

        Fsm eventFsm = gateLock.eventFsm.Fsm;
        FsmState before = eventFsm.ActiveState;
        for (int steps = 0; eventFsm.ActiveState == before && !eventFsm.IsSwitchingState; steps++)
        {
            if (steps == 1000) throw new System.InvalidOperationException($"{gateLock.name} did not send an event after {steps} steps");
            if (!drivenGateLock.routine.MoveNext())
            {
                drivenGateLock = default;
                return eventFsm.ActiveState == before && !eventFsm.IsSwitchingState ? "the gate lock stopped without completing" : null;
            }
        }
        return null;
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
    private static bool IsStunningRoar(PlayMakerFSM fsm) => fsm.isActiveAndEnabled && IsStunningRoar(fsm.Fsm.ActiveState);

    private static bool IsStunningRoar(FsmState state) =>
        state != null && state.Actions.Any(a => a.Enabled && a switch
        {
            StartRoarEmitter roar => roar.stunHero.Value,
            StartRoarEmitterV2 roar => roar.stunHero.Value,
            _ => false,
        });

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