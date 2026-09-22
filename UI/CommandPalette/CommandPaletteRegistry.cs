using System;
using System.Collections.Generic;

namespace DebugMod.UI.CommandPalette;

public sealed class CommandPaletteRegistry
{
    private readonly List<CommandPaletteItem> rootItems = [];

    public IEnumerable<CommandPaletteItem> RootItems => rootItems;

    public void Register(CommandPaletteItem item) => rootItems.Add(item);

    public void RegisterAction(string title, Action execute, string detail = null)
        => RegisterAction(() => title, execute, detail);

    public void RegisterAction(Func<string> title, Action execute, string detail = null)
        => Register(new CommandPaletteItem.ActionItem(title, execute, detail));

    public void RegisterToggle(string title, Func<bool> isEnabled, Action toggle, string detail = null)
        => RegisterToggle(() => title, isEnabled, toggle, detail);

    public void RegisterToggle(Func<string> title, Func<bool> isEnabled, Action toggle, string detail = null)
        => Register(new CommandPaletteItem.ToggleItem(title, isEnabled, toggle, detail));

    public void RegisterSubmenu(string title, Func<IEnumerable<CommandPaletteItem>> getChildren, string detail = null)
        => RegisterSubmenu(() => title, getChildren, detail);

    public void RegisterSubmenu(Func<string> title, Func<IEnumerable<CommandPaletteItem>> getChildren, string detail = null)
        => Register(new CommandPaletteItem.SubmenuItem(title, getChildren, detail));
}
