using Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace DebugMod;

public abstract class ModLoaderBase : Mod
{
    private MonoBehaviour coroutineHost;

    internal ModLogger Logger { get; }
    internal ModInfo Info => new(AssemblyLocation());

    protected ModLoaderBase()
    {
        Logger = new ModLogger(this);
    }

    // HACK: remove this
    private string AssemblyLocation()
    {
        string location = GetType().Assembly.Location;
        if (!string.IsNullOrEmpty(location))
        {
            return location;
        }

        // Hot reloaded assemblies are loaded from memory
        FieldInfo field = typeof(ModLoader).GetField("ModInstancesByAssembly", BindingFlags.NonPublic | BindingFlags.Static)
            ?? throw new MissingFieldException(nameof(ModLoader), "ModInstancesByAssembly");
        var modsByAssembly = (Dictionary<string, List<ModLoader.ModInstance>>)field.GetValue(null);
        foreach ((string path, List<ModLoader.ModInstance> mods) in modsByAssembly)
        {
            if (mods.Any(mod => mod.Mod == this))
            {
                return path;
            }
        }

        throw new InvalidOperationException($"Could not find the assembly location of {GetType().Name}");
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        if (coroutineHost == null)
        {
            GameObject go = new(GetType().Name);
            UnityEngine.Object.DontDestroyOnLoad(go);
            coroutineHost = go.AddComponent<CoroutineHost>();
        }

        return coroutineHost.StartCoroutine(routine);
    }

    protected void DestroyCoroutineHost()
    {
        if (coroutineHost != null)
        {
            UnityEngine.Object.Destroy(coroutineHost.gameObject);
            coroutineHost = null;
        }
    }

    private class CoroutineHost : MonoBehaviour;

    internal record ModInfo(string Location);

    internal class ModLogger(Loggable loggable)
    {
        public void LogDebug(string message) => loggable.LogDebug(message);
        public void LogInfo(string message) => loggable.Log(message);
        public void LogWarning(string message) => loggable.LogWarn(message);
        public void LogError(string message) => loggable.LogError(message);
    }
}
