// ReSharper disable CheckNamespace
#nullable enable

#if !NETSTANDARD2_1
namespace System.Collections.Generic {
    // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.collectionextensions.getvalueordefault
    internal static class CollectionExtensionsPolyfill {
        public static TValue? GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key)
            where TKey : notnull
            => dictionary.TryGetValue(key, out var value) ? value : default;

        // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.collectionextensions.tryadd
        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
            where TKey : notnull {
            if (dictionary.ContainsKey(key)) return false;
            dictionary.Add(key, value);
            return true;
        }
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.collections.generic.keyvaluepair-2.deconstruct
    internal static class KeyValuePairPolyfill {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> pair, out TKey key, out TValue value) {
            key = pair.Key;
            value = pair.Value;
        }
    }
}

namespace System {
    // https://learn.microsoft.com/en-us/dotnet/api/system.index
    internal readonly struct Index {
        private readonly int value;

        public Index(int value, bool fromEnd = false) {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            this.value = fromEnd ? ~value : value;
        }

        public static Index Start => new(0);
        public static Index End => new(0, true);
        public int Value => value < 0 ? ~value : value;
        public bool IsFromEnd => value < 0;
        public int GetOffset(int length) => IsFromEnd ? length - ~value : value;
        public static implicit operator Index(int value) => new(value);
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.range
    internal readonly struct Range(Index start, Index end) {
        public Index Start { get; } = start;
        public Index End { get; } = end;

        public static Range StartAt(Index start) => new(start, Index.End);
        public static Range EndAt(Index end) => new(Index.Start, end);
        public static Range All => new(Index.Start, Index.End);

        public (int Offset, int Length) GetOffsetAndLength(int length) {
            int start = Start.GetOffset(length);
            int end = End.GetOffset(length);
            if ((uint)end > (uint)length || (uint)start > (uint)end) throw new ArgumentOutOfRangeException(nameof(length));
            return (start, end - start);
        }
    }

    internal static class ArrayPolyfill {
        extension(Array) {
            // https://learn.microsoft.com/en-us/dotnet/api/system.array.fill
            public static void Fill<T>(T[] array, T value) {
                for (int i = 0; i < array.Length; i++) array[i] = value;
            }
        }
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.hashcode
    internal struct HashCode {
        public static int Combine<T1, T2>(T1 value1, T2 value2)
            => unchecked((value1?.GetHashCode() ?? 0) * 31 + (value2?.GetHashCode() ?? 0));
    }

    internal static class MathPolyfill {
        extension(Math) {
            // https://learn.microsoft.com/en-us/dotnet/api/system.math.clamp
            public static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        }
    }

    internal static class StringPolyfill {
        // https://learn.microsoft.com/en-us/dotnet/api/system.string.split#system-string-split(system-char-system-stringsplitoptions)
        public static string[] Split(this string str, char separator, StringSplitOptions options)
            => str.Split([separator], options);

        // https://learn.microsoft.com/en-us/dotnet/api/system.string.contains#system-string-contains(system-string-system-stringcomparison)
        public static bool Contains(this string str, string value, StringComparison comparisonType)
            => str.IndexOf(value, comparisonType) >= 0;
    }
}
#endif

#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices {
    // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.isexternalinit
    internal static class IsExternalInit { }
}
#endif

// From 1.3.1.5 on the Modding API bakes these into Assembly-CSharp. Public like the BCL types, so tuples stay
// usable in non-private signatures.
#if HK1221
namespace System {
    // https://learn.microsoft.com/en-us/dotnet/api/system.valuetuple-2
    public struct ValueTuple<T1, T2>(T1 item1, T2 item2) {
        public T1 Item1 = item1;
        public T2 Item2 = item2;
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.valuetuple-3
    public struct ValueTuple<T1, T2, T3>(T1 item1, T2 item2, T3 item3) {
        public T1 Item1 = item1;
        public T2 Item2 = item2;
        public T3 Item3 = item3;
    }

    // https://learn.microsoft.com/en-us/dotnet/api/system.valuetuple-4
    public struct ValueTuple<T1, T2, T3, T4>(T1 item1, T2 item2, T3 item3, T4 item4) {
        public T1 Item1 = item1;
        public T2 Item2 = item2;
        public T3 Item3 = item3;
        public T4 Item4 = item4;
    }
}

namespace System.Runtime.CompilerServices {
    // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.tupleelementnamesattribute
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Property |
                    AttributeTargets.ReturnValue | AttributeTargets.Class | AttributeTargets.Struct |
                    AttributeTargets.Event)]
    public sealed class TupleElementNamesAttribute(string[] transformNames) : Attribute {
        public string[] TransformNames { get; } = transformNames;
    }
}

#endif

// TryGetComponent arrived in Unity 2019.2; 1.5.78 (2020.2) and newer ship it.
#if HK1221 || HK1315 || HK1432
namespace UnityEngine {
    // https://docs.unity3d.com/ScriptReference/Component.TryGetComponent.html
    internal static class ComponentPolyfill {
        public static bool TryGetComponent<T>(this Component self, out T component) where T : Component {
            component = self.GetComponent<T>();
            return component != null;
        }
    }
}
#endif

#if HK1221 || HK1315 || HK1432 || HK1578
namespace UnityEngine {
    // https://docs.unity3d.com/ScriptReference/FindObjectsSortMode.html
    public enum FindObjectsSortMode {
        None,
    }

    internal static class ObjectPolyfill {
        extension(Object) {
            // https://docs.unity3d.com/ScriptReference/Object.FindObjectsByType.html
            public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object
                => Object.FindObjectsOfType<T>();

            // https://docs.unity3d.com/ScriptReference/Object.FindFirstObjectByType.html
            public static T FindFirstObjectByType<T>() where T : Object
                => Object.FindObjectOfType<T>();
        }
    }
}
#endif

#if HK1578
namespace UnityEngine.UI {
    internal static class InputFieldPolyfill {
        extension(InputField inputField) {
            // https://docs.unity3d.com/Packages/com.unity.ugui@2.0/api/UnityEngine.UI.InputField.html#UnityEngine_UI_InputField_onSubmit
            public SubmitEventPolyfill onSubmit => new(inputField);
        }
    }

    internal readonly struct SubmitEventPolyfill(InputField inputField) {
        public void AddListener(Events.UnityAction<string> call) => inputField.onEndEdit.AddListener(text => {
            // onEndEdit also fires on escape and deselect
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) call(text);
        });
    }
}
#endif
