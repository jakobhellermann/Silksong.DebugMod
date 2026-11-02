using DebugMod.Hitbox;
using DebugMod.UI.Canvas;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace DebugMod.UI;

public class HitboxTooltip : CanvasPanel
{
    private const int MaxLines = 12;

    public static HitboxTooltip Instance { get; private set; }

    private readonly CanvasImage background;
    private readonly CanvasText text;

    public static void BuildPanel()
    {
        Instance = new HitboxTooltip();
        Instance.Build();
    }

    public HitboxTooltip() : base(nameof(HitboxTooltip))
    {
        Size = new Vector2(1, 1);
        OnUpdate += DoUpdate;

        background = Add(new CanvasImage("Background"));
        background.Size = Size;
        background.SetImage(UICommon.dialogBG);
        background.AddBorder();

        text = Add(new CanvasText("Text"));
        text.Overflow = HorizontalWrapMode.Overflow;
        text.LocalPosition = new Vector2(UICommon.Margin, UICommon.Margin);
    }

    private void DoUpdate()
    {
        var lines = HitboxRender.Hovered.Take(MaxLines).Select(h => $"[{h.Type.Name}] {GetPath(h.Collider.transform)}").ToList();
        if (HitboxRender.Hovered.Count > MaxLines) lines.Add($"+{HitboxRender.Hovered.Count - MaxLines} more");
        text.Text = string.Join("\n", lines);

        Text uiText = text.GameObject.GetComponent<Text>();
        Vector2 textSize = new(Mathf.Ceil(uiText.preferredWidth), Mathf.Ceil(uiText.preferredHeight));
        text.Size = textSize;
        Size = textSize + Vector2.one * UICommon.Margin * 2;
        background.Size = Size;

        Vector2 mouse = new(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        Vector2 offset = Vector2.one * UICommon.ScaleHeight(16);
        Vector2 pos = mouse + offset;
        if (pos.x + Size.x > Screen.width) pos.x = mouse.x - offset.x - Size.x;
        if (pos.y + Size.y > Screen.height) pos.y = mouse.y - offset.y - Size.y;
        LocalPosition = new Vector2(Mathf.Max(0, pos.x), Mathf.Max(0, pos.y));
    }

    private static string GetPath(Transform t)
    {
        string path = t.name;
        for (Transform parent = t.parent; parent; parent = parent.parent)
        {
            path = $"{parent.name}/{path}";
        }
        return path;
    }
}
