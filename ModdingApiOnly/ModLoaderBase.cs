using Modding;
using System.Collections;
using UnityEngine;

namespace DebugMod;

public abstract class ModLoaderBase : Mod
{
    private MonoBehaviour coroutineHost;

    internal ModLogger Logger { get; }
    internal ModInfo Info { get; }

    protected ModLoaderBase()
    {
        Logger = new ModLogger(this);
        Info = new ModInfo(GetType().Assembly.Location);
    }

    public Coroutine StartCoroutine(IEnumerator routine)
    {
        if (coroutineHost == null)
        {
            GameObject go = new(GetType().Name);
            Object.DontDestroyOnLoad(go);
            coroutineHost = go.AddComponent<CoroutineHost>();
        }

        return coroutineHost.StartCoroutine(routine);
    }

    protected void DestroyCoroutineHost()
    {
        if (coroutineHost != null)
        {
            Object.Destroy(coroutineHost.gameObject);
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
