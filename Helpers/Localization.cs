using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using TeamCherry.Localization;

namespace DebugMod.Helpers;

internal static class Localization
{
    private static readonly List<string> sheets = [$"Mods.{DebugMod.Id}"];
    private static Dictionary<string, string> fallbackSheet;
    private static bool warnedEntryMissing;

    internal static Dictionary<string, string> FallbackSheet
    {
        get
        {
            if (fallbackSheet == null)
            {
                LoadFallbackSheet();
            }

            return fallbackSheet ?? [];
        }
    }

    private static void LoadFallbackSheet()
    {
        // When loaded with `ScriptEngine`, Info.Location is only available after Awake and `Assembly.Location` is empty
        string location = DebugMod.instance.Info.Location;
        if (string.IsNullOrEmpty(location))
        {
            return;
        }

        try
        {
            string path = Path.Combine(Path.GetDirectoryName(location), "languages", "en.json");
            Dictionary<string, object> dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(path));

            fallbackSheet = [];
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                fallbackSheet.Add(pair.Key, (string)pair.Value);
            }
        }
        catch (Exception e)
        {
            DebugMod.LogError($"Could not load fallback sheet: {e}");
            fallbackSheet = [];
        }
    }

    internal static string Get(string key)
    {
        if (TryGetFromSheets(key, out string value)) return value;

        if (!warnedEntryMissing)
        {
            DebugMod.LogWarn("Entry not found in language sheet (is Silksong.I18N installed?)");
            warnedEntryMissing = true;
        }

        if (FallbackSheet.TryGetValue(key, out value))
        {
            return value;
        }

        DebugMod.LogError($"'{key}' is not a valid key in the language sheet.");
        return key;
    }

    /// <summary>
    /// Like <see cref="Get"/>, but passes text that is not a known localization key through unchanged and
    /// never warns — for callers that can't tell whether a string is a key (palette titles are either
    /// keys or raw data like scene names).
    /// </summary>
    internal static string GetOrSelf(string key)
    {
        if (string.IsNullOrEmpty(key)) return key;
        if (TryGetFromSheets(key, out string value)) return value;
        return FallbackSheet.TryGetValue(key, out value) ? value : key;
    }

    // Silent reimplementation of Language.Get to avoid I18N warning us of extensions missing keys
    private static bool TryGetFromSheets(string key, out string value)
    {
        foreach (string sheetName in sheets)
        {
            if (Language._currentEntrySheets == null || !Language._currentEntrySheets.ContainsKey(sheetName)) continue;
            if (!Language._currentEntrySheets.TryGetValue(sheetName, out Dictionary<string, string> sheet)) continue;
            if (sheet.TryGetValue(key, out value)) return true;
        }

        value = null;
        return false;
    }

    internal static void AddSheet(string sheet)
    {
        sheets.Add(sheet);
    }
}