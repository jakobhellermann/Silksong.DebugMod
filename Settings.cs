using DebugMod.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DebugMod;

public partial class Settings
{
    public Dictionary<string, Binding> binds = new();

    private string lastLoadedPack = "";
    private string mainPanelCurrentTab;
    private int showHitBoxes;
    private bool showCursorWhileUnpaused;

    private bool mainPanelVisible = true;
    private bool enemiesPanelVisible = true;
    private bool consoleVisible = true;
    private bool infoPanelVisible = true;
    private bool saveStatePanelVisible = true;
    private bool saveStatePanelExpanded = false;

    private bool logUnityExceptions = true;

    public string LastLoadedPack
    {
        get => lastLoadedPack;
        set
        {
            lastLoadedPack = value;
            DebugMod.SaveSettings();
        }
    }

    public string MainPanelCurrentTab
    {
        get => mainPanelCurrentTab;
        set
        {
            mainPanelCurrentTab = value;
            DebugMod.SaveSettings();
        }
    }

    public int ShowHitBoxes
    {
        get => showHitBoxes;
        set
        {
            showHitBoxes = value;
            DebugMod.SaveSettings();
        }
    }

    public bool ShowCursorWhileUnpaused
    {
        get => showCursorWhileUnpaused;
        set
        {
            showCursorWhileUnpaused = value;
            DebugMod.SaveSettings();
        }
    }

    public bool MainPanelVisible
    {
        get => mainPanelVisible;
        set
        {
            mainPanelVisible = value;
            DebugMod.SaveSettings();
        }
    }

    public bool EnemiesPanelVisible
    {
        get => enemiesPanelVisible;
        set
        {
            enemiesPanelVisible = value;
            DebugMod.SaveSettings();
        }
    }

    public bool ConsoleVisible
    {
        get => consoleVisible;
        set
        {
            consoleVisible = value;
            DebugMod.SaveSettings();
        }
    }

    public bool InfoPanelVisible
    {
        get => infoPanelVisible;
        set
        {
            infoPanelVisible = value;
            DebugMod.SaveSettings();
        }
    }

    public bool SaveStatePanelVisible
    {
        get => saveStatePanelVisible;
        set
        {
            saveStatePanelVisible = value;
            DebugMod.SaveSettings();
        }
    }

    public bool SaveStatePanelExpanded
    {
        get => saveStatePanelExpanded;
        set
        {
            saveStatePanelExpanded = value;
            DebugMod.SaveSettings();
        }
    }

    public bool LogUnityExceptions
    {
        get => logUnityExceptions;
        set
        {
            logUnityExceptions = value;
            DebugMod.SaveSettings();
        }
    }
}