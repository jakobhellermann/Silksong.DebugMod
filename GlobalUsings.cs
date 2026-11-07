global using DebugMod.VersionInterop;
#if SILKSONG
global using GameSceneManager = CustomSceneManager;
#else
global using GameSceneManager = global::SceneManager;
#endif
#if HK1578_OR_OLDER
global using LanguageCode = Language.LanguageCode;
#endif
#if BEPINEX
global using ModLoaderBase = BepInEx.BaseUnityPlugin;
#endif
#if MODDINGAPI && !HK1221_OR_OLDER
global using ModHooks = Modding.ModHooks;
#endif
#if !HK1221_OR_OLDER
global using SceneLoadInfo = GameManager.SceneLoadInfo;
#endif
#if HK1221_OR_OLDER
global using HollowKnightInputModule = InControl.InControlInputModule;
global using Profiler = DebugMod.Helpers.Profiler;
#endif
