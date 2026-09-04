using UnityEngine;

namespace SptBattlePass.Client.UI;

internal static class TabIcon
{
    private static Sprite _sprite;

    public static Sprite Get()
    {
        if (_sprite != null)
        {
            return _sprite;
        }

        Texture2D texture = DrawGlyph();
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.hideFlags = HideFlags.HideAndDontSave;
        _sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        _sprite.hideFlags = HideFlags.HideAndDontSave;
        return _sprite;
    }

    private static Texture2D DrawGlyph()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 ink = new Color32(255, 255, 255, 255);
        Color32[] pixels = new Color32[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = clear;
        }

        FillRoundRect(pixels, size, 4, 16, 60, 48, 5, ink);
        CutCircle(pixels, size, 16, 32, 6, clear);
        CutCircle(pixels, size, 16, 32, 4, clear);
        for (int y = 20; y <= 44; y++)
        {
            if ((y % 3) != 0)
            {
                Plot(pixels, size, 26, y, clear);
                Plot(pixels, size, 27, y, clear);
            }
        }

        CutRect(pixels, size, 50, 22, 58, 42, clear);
        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    private static void FillRoundRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, int radius, Color32 color)
    {
        int r2 = radius * radius;
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                int cx = x < x0 + radius ? x0 + radius : x > x1 - radius ? x1 - radius : x;
                int cy = y < y0 + radius ? y0 + radius : y > y1 - radius ? y1 - radius : y;
                bool corner = (x < x0 + radius || x > x1 - radius) && (y < y0 + radius || y > y1 - radius);
                if (corner)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy > r2)
                    {
                        continue;
                    }
                }

                Plot(pixels, size, x, y, color);
            }
        }
    }

    private static void CutCircle(Color32[] pixels, int size, int cx, int cy, int radius, Color32 color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= r2)
                {
                    Plot(pixels, size, x, y, color);
                }
            }
        }
    }

    private static void CutRect(Color32[] pixels, int size, int x0, int y0, int x1, int y1, Color32 color)
    {
        for (int y = y0; y <= y1; y++)
        {
            for (int x = x0; x <= x1; x++)
            {
                Plot(pixels, size, x, y, color);
            }
        }
    }

    private static void Plot(Color32[] pixels, int size, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= size || y >= size)
        {
            return;
        }

        pixels[y * size + x] = color;
    }
}
