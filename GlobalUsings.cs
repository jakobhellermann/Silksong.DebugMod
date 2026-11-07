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
#if MODDINGAPI
global using ModHooks = Modding.ModHooks;
#endif
