#if HK1221_OR_OLDER
// TODO(port/1.2.2.1): replacement for EventRegister, check that the receiving FSMs react to broadcasts
// ReSharper disable once CheckNamespace
internal static class EventRegister
{
    public static void SendEvent(string eventName) => PlayMakerFSM.BroadcastEvent(eventName);
}
#endif
