using DebugMod.Helpers;
using System;

namespace DebugMod.Interop;

#nullable enable

internal static class LoadstoneInterop
{
    private static readonly Func<string, string, IDisposable>? scope;

    static LoadstoneInterop()
    {
        if (!InteropHelper.IsModInstalled(InteropHelper.LoadstoneModId, "0.1.0")) return;

        Type api = InteropHelper.GetPlugin(InteropHelper.LoadstoneModId).GetType().Assembly
            .GetType("LoadstoneProfiler.Source.LoadstoneApi", throwOnError: true);
        scope = (Func<string, string, IDisposable>)Delegate.CreateDelegate(
            typeof(Func<string, string, IDisposable>), api, nameof(Scope));
    }

    public static IDisposable? Scope(string category, string name) => scope?.Invoke(category, name);

    public static Sequence Phases(string category) => new(category);

    public sealed class Sequence(string category) : IDisposable
    {
        private IDisposable? current;

        public void Enter(string name)
        {
            current?.Dispose();
            current = Scope(category, name);
        }

        public void Dispose()
        {
            current?.Dispose();
            current = null;
        }
    }
}
