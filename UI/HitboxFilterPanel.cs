using DebugMod.Hitbox;
using DebugMod.UI.Canvas;
using UnityEngine;

namespace DebugMod.UI;

public class HitboxFilterPanel : CanvasPanel
{
    public static int PanelWidth => UICommon.ScaleWidth(200);

    public static HitboxFilterPanel Instance { get; private set; }

    public static void BuildPanel()
    {
        Instance = new HitboxFilterPanel();
        Instance.Build();
    }

    public HitboxFilterPanel() : base(nameof(HitboxFilterPanel))
    {
        LocalPosition = new Vector2(Screen.width - UICommon.ScreenMargin * 2 - UICommon.RightSideWidth - PanelWidth, UICommon.ScreenMargin);
        Size = new Vector2(PanelWidth, 0);

        UICommon.AddBackground(this);

        using PanelBuilder builder = new(this);
        builder.DynamicLength = true;
        builder.OuterPadding = ContentMargin(UICommon.Margin);
        builder.InnerPadding = UICommon.Margin;

        foreach (HitboxRender.HitboxType type in HitboxRender.HitboxType.All)
        {
            using PanelBuilder row = new(builder.AppendFixed(new CanvasPanel(type.Name), UICommon.ControlHeight));
            row.Horizontal = true;
            row.InnerPadding = UICommon.Margin;

            CanvasImage swatch = row.AppendSquare(new CanvasImage("Swatch"));
            swatch.SetImage(Texture2D.whiteTexture);
            swatch.Color = type.Color;

            CanvasButton toggle = row.AppendFlex(new CanvasButton("Toggle"));
            toggle.Text.Text = type.Name;
            toggle.Text.Alignment = TextAnchor.MiddleLeft;
            toggle.OnUpdate += () => toggle.Toggled = type.Visible;
            toggle.OnClicked += () =>
            {
                if (!DebugMod.settings.HiddenHitboxTypes.Remove(type.Name))
                {
                    DebugMod.settings.HiddenHitboxTypes.Add(type.Name);
                }
                DebugMod.SaveSettings();
            };
        }
    }
}
