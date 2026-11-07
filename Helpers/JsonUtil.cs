using System;
using System.IO;
#if HK1221_OR_OLDER
using LitJson;
#else
using Newtonsoft.Json;
#endif

namespace DebugMod.Helpers;

// Newtonsoft.Json always references System.Data, which 1.2.2.1 doesn't ship
internal static class JsonUtil
{
    public static T Deserialize<T>(string embeddedResourcePath)
    {
        using StreamReader sr = new(typeof(JsonUtil).Assembly.GetManifestResourceStream(embeddedResourcePath) ?? throw new ArgumentNullException($"Embedded resource {embeddedResourcePath} missing"));
#if HK1221_OR_OLDER
        return JsonMapper.ToObject<T>(sr);
#else
        using JsonTextReader jtr = new(sr);
        return _js.Deserialize<T>(jtr);
#endif
    }

#if HK1221_OR_OLDER
    public static T FromJson<T>(string json) => JsonMapper.ToObject<T>(json);

    public static string ToJson(object value)
    {
        JsonWriter writer = new() { PrettyPrint = true };
        JsonMapper.ToJson(value, writer);
        return writer.ToString();
    }

    static JsonUtil()
    {
        Binding.RegisterLitJson();
    }
#else
    private static readonly JsonSerializer _js;

    public static T FromJson<T>(string json) => JsonConvert.DeserializeObject<T>(json);

    public static string ToJson(object value) => JsonConvert.SerializeObject(value, Formatting.Indented);

    static JsonUtil()
    {
        _js = new JsonSerializer
        {
            DefaultValueHandling = DefaultValueHandling.Ignore,
            Formatting = Formatting.Indented,
            TypeNameHandling = TypeNameHandling.Auto,
        };
    }
#endif
}
