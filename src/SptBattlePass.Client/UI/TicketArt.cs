using System;
using System.IO;
using UnityEngine;

namespace SptBattlePass.Client.UI;

internal static class TicketArt
{
    private static Texture2D _texture;
    private static bool _photo;

    public static void Draw(Rect area)
    {
        Texture2D texture = Get();
        if (texture == null || Event.current.type != EventType.Repaint)
        {
            return;
        }

        if (_photo)
        {
            GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
            return;
        }

        Color previous = GUI.color;
        GUI.color = previous * TarkovUi.Amber;
        GUI.DrawTexture(area, texture, ScaleMode.ScaleToFit, true);
        GUI.color = previous;
    }

    public static void DrawLayout(float height)
    {
        Texture2D texture = Get();
        float aspect = texture != null && texture.height > 0
            ? texture.width / (float)texture.height
            : 2.1f;
        aspect = Mathf.Clamp(aspect, 1.6f, 2.6f);
        float width = height * aspect;
        Rect area = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
        Draw(area);
    }

    private static Texture2D Get()
    {
        if (_texture != null)
        {
            return _texture;
        }

        _texture = LoadPng();
        if (_texture != null)
        {
            _photo = true;
            return _texture;
        }

        _photo = false;
        _texture = TabIcon.GetTexture();
        return _texture;
    }

    private static Texture2D LoadPng()
    {
        try
        {
            string folder = Path.GetDirectoryName(typeof(TicketArt).Assembly.Location);
            if (string.IsNullOrEmpty(folder))
            {
                return null;
            }

            string path = Path.Combine(folder, "ticket-icon.png");
            if (!File.Exists(path))
            {
                return null;
            }

            byte[] bytes = File.ReadAllBytes(path);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };
            if (!ImageConversion.LoadImage(texture, bytes, false))
            {
                UnityEngine.Object.Destroy(texture);
                return null;
            }

            PunchCheckerboard(texture);
            Texture2D cropped = CropTransparent(texture);
            if (cropped != texture)
            {
                UnityEngine.Object.Destroy(texture);
            }

            cropped.wrapMode = TextureWrapMode.Clamp;
            cropped.filterMode = FilterMode.Bilinear;
            cropped.hideFlags = HideFlags.HideAndDontSave;
            return cropped;
        }
        catch
        {
            return null;
        }
    }

    private static void PunchCheckerboard(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();
        bool changed = false;
        for (int i = 0; i < pixels.Length; i++)
        {
            Color32 pixel = pixels[i];
            int chroma = Math.Max(pixel.r, Math.Max(pixel.g, pixel.b)) - Math.Min(pixel.r, Math.Min(pixel.g, pixel.b));
            int warmth = pixel.r - pixel.b;
            byte alpha;
            if (warmth >= 32 && chroma >= 22)
            {
                alpha = pixel.a;
            }
            else if (warmth <= 12 || chroma <= 10)
            {
                alpha = 0;
            }
            else
            {
                int fringe = (warmth - 12) * 255 / 20;
                if (chroma < 16)
                {
                    fringe = fringe * chroma / 16;
                }

                alpha = (byte)Math.Max(0, Math.Min(pixel.a, fringe));
            }

            if (alpha != pixel.a)
            {
                pixel.a = alpha;
                if (alpha < 8)
                {
                    pixel.r = 0;
                    pixel.g = 0;
                    pixel.b = 0;
                    pixel.a = 0;
                }

                pixels[i] = pixel;
                changed = true;
            }
        }

        if (!changed)
        {
            return;
        }

        source.SetPixels32(pixels);
        source.Apply(false, false);
    }

    private static Texture2D CropTransparent(Texture2D source)
    {
        Color32[] pixels = source.GetPixels32();
        int width = source.width;
        int height = source.height;
        int minX = width;
        int minY = height;
        int maxX = -1;
        int maxY = -1;
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                if (pixels[row + x].a < 12)
                {
                    continue;
                }

                if (x < minX)
                {
                    minX = x;
                }

                if (y < minY)
                {
                    minY = y;
                }

                if (x > maxX)
                {
                    maxX = x;
                }

                if (y > maxY)
                {
                    maxY = y;
                }
            }
        }

        if (maxX < minX)
        {
            return source;
        }

        int pad = Math.Max(4, (maxX - minX) / 24);
        minX = Math.Max(0, minX - pad);
        minY = Math.Max(0, minY - pad);
        maxX = Math.Min(width - 1, maxX + pad);
        maxY = Math.Min(height - 1, maxY + pad);
        int cropW = maxX - minX + 1;
        int cropH = maxY - minY + 1;
        if (cropW >= width - 4 && cropH >= height - 4)
        {
            return source;
        }

        var cropped = new Texture2D(cropW, cropH, TextureFormat.RGBA32, false);
        cropped.SetPixels(source.GetPixels(minX, minY, cropW, cropH));
        cropped.Apply(false, true);
        return cropped;
    }
}
