using BepInEx.Configuration;
using DebugMod.UI;
using System.Linq;
using UnityEngine;

namespace DebugMod;

public partial class Settings
{
    private static ConfigEntry<float> noclipSpeedModifier;
    private static ConfigEntry<bool> altInfoPanel;
    private static ConfigEntry<bool> expandedInfoPanel;

    private static ConfigEntry<bool> disableBossHpBars;
    
    private static ConfigEntry<bool> safeSavestateLoading;

    public float NoClipSpeedModifier
    {
        get => noclipSpeedModifier.Value;
        set
        {
            noclipSpeedModifier.Value = value;
            DebugMod.SaveSettings();
        }
    }

    public bool AltInfoPanel
    {
        get => altInfoPanel.Value;
        set
        {
            altInfoPanel.Value = value;
            DebugMod.SaveSettings();
        }
    }

    public bool ExpandedInfoPanel
    {
        get => expandedInfoPanel.Value;
        set
        {
            expandedInfoPanel.Value = value;
            DebugMod.SaveSettings();
        }
    }

    public bool DisableBossHpBars
    {
        get => disableBossHpBars.Value;
        set
        {
            disableBossHpBars.Value = value;
            DebugMod.SaveSettings();
        }
    }

    public bool SafeSaveStateLoading
    {
        get => safeSavestateLoading.Value;
        set
        {
            safeSavestateLoading.Value = value;
            DebugMod.SaveSettings();
        }
    }

    internal void InitMenu(ConfigFile config)
    {
        // We store all the settings ourselves
        config.SaveOnConfigSet = false;

        AddConfigEntryKeybind(config, "MODUI_TOGGLEALLUI", "Toggle All UI Keybind",
            new Binding(KeyCode.F2), "Press this key to toggle DebugMod's UI.");
        AddConfigEntryKeybind(config, "MODUI_TOGGLECOMMANDPALETTE", "Toggle Command Palette Keybind",
            new Binding(Modifier.Ctrl, KeyCode.Space), "Press this key to toggle the command palette.");

        noclipSpeedModifier = config.Bind(
            "General",
            "Noclip Speed Multiplier",
            1f,
            "You can also hold shift in noclip to get an additional 2x multiplier."
        );

        altInfoPanel = config.Bind(
            "General",
            "Alternate Info Panel Style",
            false,
            "Adds some decoration to the info panel."
        );
        altInfoPanel.SettingChanged += (_, _) =>
        {
            InfoPanel.Instance.Destroy();
            InfoPanel.BuildPanel();
        };

        expandedInfoPanel = config.Bind(
            "General",
            "Expanded Info Panel",
            false,
            "Shows additional niche info on the info panel."
        );
        expandedInfoPanel.SettingChanged += (_, _) =>
        {
            InfoPanel.Instance.Destroy();
            InfoPanel.BuildPanel();
        };

        disableBossHpBars = config.Bind(
            "Visual",
            "Disable Boss HP Bars",
            false,
            "Make bosses use the default HP bar."
        );
        disableBossHpBars.SettingChanged += (_, _) => {
        
        };

        safeSavestateLoading = config.Bind(
            "Savestates",
            "Safe Savestate Loading",
            false,
            "Fixes some obscure issues when using savestates, but makes loading take longer."
        );
    }

    private static void AddConfigEntryKeybind(ConfigFile config, string bindName, string displayName, Binding defaultBinding, string description)
    {
        // KeyboardShortcut instead of Binding, so that ConfigManager knows how to handle it
        ConfigEntry<KeyboardShortcut> entry = config.Bind("General", displayName, ToShortcut(defaultBinding), description);
        entry.SettingChanged += (_, _) =>
        {
            DebugMod.UpdateBind(bindName, ToBinding(entry.Value));
        };
        DebugMod.bindUpdated += (name, binding) =>
        {
            if (name == bindName && ToBinding(entry.Value) != binding)
            {
                entry.Value = ToShortcut(binding);
            }
        };
    }

    private static Binding? ToBinding(KeyboardShortcut shortcut)
    {
        if (shortcut.MainKey == KeyCode.None) return null;

        Modifier modifiers = Modifier.None;
        foreach (KeyCode key in shortcut.Modifiers)
        {
            modifiers |= Modifiers.FromKeyCode(key);
        }
        return new Binding(modifiers, shortcut.MainKey);
    }

    private static KeyboardShortcut ToShortcut(Binding? binding)
    {
        if (binding is null || binding.Value.Key == KeyCode.None) return KeyboardShortcut.Empty;

        KeyCode[] modifiers = [..binding.Value.Modifiers.Active().Select(modifier => modifier.ToKeyCode())];
        return new KeyboardShortcut(binding.Value.Key, modifiers);
    }
}
