using System.Collections.Generic;
using UnityEngine;

namespace SptBattlePass.Client.UI;

internal static class TarkovUi
{
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.62f);
    public static readonly Color Panel = new Color(0.066f, 0.066f, 0.068f, 0.97f);
    public static readonly Color Amber = new Color(0.804f, 0.522f, 0.196f, 1f);
    public static readonly Color Text = new Color(0.865f, 0.865f, 0.875f, 1f);
    public static readonly Color Grey = new Color(0.64f, 0.64f, 0.655f, 1f);
    public static readonly Color Dim = new Color(0.48f, 0.48f, 0.495f, 1f);
    public static readonly Color Green = new Color(0.42f, 0.64f, 0.38f, 1f);
    public static readonly Color Red = new Color(0.78f, 0.29f, 0.235f, 1f);
    public static readonly Color Item = new Color(0.11f, 0.11f, 0.116f, 1f);
    public static readonly Color IconWell = new Color(0.152f, 0.152f, 0.16f, 1f);
    public static readonly Color Sep = new Color(0.195f, 0.195f, 0.205f, 1f);
    public static readonly Color BarBg = new Color(0.135f, 0.135f, 0.142f, 1f);
    public static readonly Color Btn = new Color(0.118f, 0.118f, 0.124f, 1f);
    public static readonly Color BtnHover = new Color(0.2f, 0.15f, 0.06f, 1f);
    public static readonly Color TabOn = new Color(0.145f, 0.125f, 0.085f, 1f);
    public static readonly Color CloseHover = new Color(0.35f, 0.16f, 0.12f, 1f);

    public static Font Font { get; private set; }
    public static Texture2D White { get; private set; }
    public static Texture2D AmberTex { get; private set; }
    public static Texture2D PanelTex { get; private set; }
    public static Texture2D OverlayTex { get; private set; }
    public static Texture2D ItemTex { get; private set; }
    public static Texture2D IconTex { get; private set; }
    public static Texture2D BarBgTex { get; private set; }
    public static Texture2D GreenTex { get; private set; }
    public static Texture2D SepTex { get; private set; }
    public static Texture2D IdleBarTex { get; private set; }

    private static readonly Dictionary<int, Texture2D> Cache = new Dictionary<int, Texture2D>();
    private static bool _ready;

    public static void Ensure()
    {
        if (!_ready)
        {
            White = Tex(Color.white);
            AmberTex = Tex(Amber);
            PanelTex = Tex(Panel);
            OverlayTex = Tex(Overlay);
            ItemTex = Tex(Item);
            IconTex = Tex(IconWell);
            BarBgTex = Tex(BarBg);
            GreenTex = Tex(Green);
            SepTex = Tex(Sep);
            IdleBarTex = Tex(new Color(0.22f, 0.22f, 0.23f, 1f));
            _ready = true;
        }

        if (Font != null)
        {
            return;
        }

        Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
        foreach (Font font in fonts)
        {
            if (font == null)
            {
                continue;
            }

            string name = font.name ?? "";
            if (name.IndexOf("Bender", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("consortium", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                Font = font;
                return;
            }
        }

        Font = Font.CreateDynamicFontFromOSFont(new[] { "Bender", "Bahnschrift", "Segoe UI" }, 16);
    }

    public static Texture2D Tex(Color color)
    {
        var raw = (Color32)color;
        int key = raw.r | (raw.g << 8) | (raw.b << 16) | (raw.a << 24);
        if (Cache.TryGetValue(key, out Texture2D existing) && existing != null)
        {
            return existing;
        }

        var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave
        };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        Cache[key] = texture;
        return texture;
    }

    public static void Outline(Rect rect, Color color, float thickness = 1f)
    {
        Texture2D tex = Tex(color);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), tex);
        GUI.DrawTexture(new Rect(rect.x, rect.y, thickness, rect.height), tex);
        GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), tex);
    }

    public static void Frame(Rect window)
    {
        GUI.DrawTexture(window, PanelTex);
        GUI.DrawTexture(new Rect(window.x, window.y, window.width, 3f), AmberTex);
        Outline(window, new Color(Amber.r, Amber.g, Amber.b, 0.8f), 1f);
    }

    public static GUIStyle Label(int size, Color color, FontStyle style = FontStyle.Normal, TextAnchor align = TextAnchor.UpperLeft, bool wrap = false)
    {
        var gui = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = style,
            alignment = align,
            wordWrap = wrap,
            clipping = TextClipping.Clip,
            font = Font
        };
        gui.normal.textColor = color;
        return gui;
    }

    public static GUIStyle Button(int size, Color text, Color bg, Color hoverText, Color hoverBg)
    {
        var gui = new GUIStyle(GUI.skin.button)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            font = Font,
            border = new RectOffset(0, 0, 0, 0),
            margin = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(8, 8, 4, 4)
        };
        gui.normal.textColor = text;
        gui.normal.background = Tex(bg);
        gui.hover.textColor = hoverText;
        gui.hover.background = Tex(hoverBg);
        gui.active.textColor = Color.white;
        gui.active.background = Tex(hoverBg);
        gui.focused.textColor = text;
        gui.focused.background = Tex(bg);
        gui.onNormal = gui.normal;
        gui.onHover = gui.hover;
        gui.onActive = gui.active;
        return gui;
    }
}
