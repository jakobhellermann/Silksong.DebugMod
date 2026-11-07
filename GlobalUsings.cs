global using DebugMod.VersionInterop;
#if SILKSONG
global using GameSceneManager = CustomSceneManager;
#else
global using GameSceneManager = global::SceneManager;
#endif
#if BEPINEX
global using ModLoaderBase = BepInEx.BaseUnityPlugin;
#endif
