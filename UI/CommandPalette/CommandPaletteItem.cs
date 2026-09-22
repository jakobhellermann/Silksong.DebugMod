using System;
using System.Collections.Generic;

namespace DebugMod.UI.CommandPalette;

public abstract class CommandPaletteItem(Func<string> title, string detail = null)
{
    public Func<string> Title { get; } = title;
    public string Detail { get; } = detail;

    public sealed class ToggleItem(
        Func<string> title,
        Func<bool> isEnabled,
        Action toggle,
        string detail = null)
        : CommandPaletteItem(title, detail)
    {
        public ToggleItem(string title, Func<bool> isEnabled, Action toggle, string detail = null)
            : this(() => title, isEnabled, toggle, detail) { }

        public Func<bool> IsEnabled { get; } = isEnabled;
        public Action Toggle { get; } = toggle;
    }

    public sealed class ActionItem(Func<string> title, Action execute, string detail = null)
        : CommandPaletteItem(title, detail)
    {
        public ActionItem(string title, Action execute, string detail = null)
            : this(() => title, execute, detail) { }

        public Action Execute { get; } = execute;
    }

    public sealed class SubmenuItem(
        Func<string> title,
        Func<IEnumerable<CommandPaletteItem>> getChildren,
        string detail = null,
        bool searchChildren = true)
        : CommandPaletteItem(title, detail)
    {
        public SubmenuItem(string title, Func<IEnumerable<CommandPaletteItem>> getChildren, string detail = null, bool searchChildren = true)
            : this(() => title, getChildren, detail, searchChildren) { }

        public Func<IEnumerable<CommandPaletteItem>> GetChildren { get; } = getChildren;
        public bool SearchChildren { get; } = searchChildren;
    }
}
