using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.UI.DragAndDrop;
using UnityEngine;

namespace SptBattlePass.Client.Services;

internal static class BattlePassItemIcons
{
    private const int MaxAttempts = 8;
    private const float RetryDelay = 2f;

    private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
    private static readonly HashSet<string> Queued = new HashSet<string>();
    private static readonly Queue<string> Pending = new Queue<string>();
    private static readonly Dictionary<string, Miss> Misses = new Dictionary<string, Miss>();
    private static MethodInfo _createItem;
    private static PropertyInfo _singletonInstance;
    private static bool _pumping;

    private struct Miss
    {
        public int Attempts;
        public float LastTry;
        public bool Permanent;
    }

    public static Sprite Get(string templateId)
    {
        if (string.IsNullOrEmpty(templateId))
        {
            return null;
        }

        if (!Cache.TryGetValue(templateId, out Sprite sprite))
        {
            return null;
        }

        if (sprite != null)
        {
            return sprite;
        }

        Cache.Remove(templateId);
        return null;
    }

    public static bool IsLoading(string templateId)
    {
        return !string.IsNullOrEmpty(templateId) && Queued.Contains(templateId);
    }

    public static void Request(string templateId)
    {
        if (string.IsNullOrEmpty(templateId) || Queued.Contains(templateId) || Get(templateId) != null)
        {
            return;
        }

        if (Misses.TryGetValue(templateId, out Miss miss)
            && (miss.Permanent || miss.Attempts >= MaxAttempts || Time.unscaledTime - miss.LastTry < RetryDelay))
        {
            return;
        }

        if (Plugin.Instance == null)
        {
            return;
        }

        Queued.Add(templateId);
        Pending.Enqueue(templateId);
        if (!_pumping)
        {
            Plugin.Instance.StartCoroutine(Pump());
        }
    }

    public static void Draw(Rect area, Sprite sprite, Color tint)
    {
        if (sprite == null || sprite.texture == null)
        {
            return;
        }

        Texture2D texture = sprite.texture;
        Rect textureRect = sprite.textureRect;
        var uv = new Rect(
            textureRect.x / texture.width,
            textureRect.y / texture.height,
            textureRect.width / texture.width,
            textureRect.height / texture.height);
        float aspect = textureRect.height > 0f ? textureRect.width / textureRect.height : 1f;
        float width = area.width;
        float height = area.height;
        if (width / height > aspect)
        {
            width = height * aspect;
        }
        else
        {
            height = width / aspect;
        }

        var dest = new Rect(
            area.x + (area.width - width) * 0.5f,
            area.y + (area.height - height) * 0.5f,
            width,
            height);
        Color previous = GUI.color;
        GUI.color = tint;
        GUI.DrawTextureWithTexCoords(dest, texture, uv);
        GUI.color = previous;
    }

    private static IEnumerator Pump()
    {
        _pumping = true;
        while (Pending.Count > 0)
        {
            string templateId = Pending.Dequeue();
            yield return Load(templateId);
            Queued.Remove(templateId);
        }

        _pumping = false;
    }

    private static IEnumerator Load(string templateId)
    {
        Task<Sprite> task = null;
        try
        {
            object factory = ItemFactoryInstance();
            if (factory == null)
            {
                yield break;
            }

            Item item = CreateDummyItem(factory, templateId);
            if (item == null)
            {
                NoteMiss(templateId, false);
                yield break;
            }

            task = ItemViewFactory.GetItemSpriteAsync(item, 1);
        }
        catch (Exception exception)
        {
            bool missing = exception.Message != null
                           && exception.Message.IndexOf("Cannot find template", StringComparison.OrdinalIgnoreCase) >= 0;
            Plugin.Log.LogWarning($"[BattlePass] Icon load failed for {templateId}: {exception.Message}");
            NoteMiss(templateId, missing);
            yield break;
        }

        while (task != null && !task.IsCompleted)
        {
            yield return null;
        }

        Sprite sprite = null;
        try
        {
            sprite = task?.Result;
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning($"[BattlePass] Icon result failed for {templateId}: {exception.Message}");
        }

        if (sprite != null && sprite.texture != null)
        {
            Cache[templateId] = sprite;
            Misses.Remove(templateId);
            yield break;
        }

        Plugin.Log.LogWarning($"[BattlePass] Icon empty for {templateId}");
        NoteMiss(templateId, false);
    }

    private static object ItemFactoryInstance()
    {
        ResolveFactory();
        if (_singletonInstance == null)
        {
            return null;
        }

        return _singletonInstance.GetValue(null);
    }

    private static Item CreateDummyItem(object factory, string templateId)
    {
        if (factory == null || _createItem == null)
        {
            return null;
        }

        string id = MongoID.Generate(false);
        return _createItem.Invoke(factory, new object[] { id, templateId, null }) as Item;
    }

    private static void ResolveFactory()
    {
        if (_createItem != null)
        {
            return;
        }

        Type[] types;
        try
        {
            types = typeof(Item).Assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        if (types == null)
        {
            return;
        }

        foreach (Type type in types)
        {
            if (type == null)
            {
                continue;
            }

            MethodInfo create = null;
            try
            {
                foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (method.Name != "CreateItem" || method.ReturnType != typeof(Item))
                    {
                        continue;
                    }

                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 3
                        && parameters[0].ParameterType == typeof(string)
                        && parameters[1].ParameterType == typeof(string))
                    {
                        create = method;
                        break;
                    }
                }
            }
            catch
            {
                continue;
            }

            if (create == null)
            {
                continue;
            }

            Type singletonType = typeof(Singleton<>).MakeGenericType(type);
            PropertyInfo instance = singletonType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instance == null)
            {
                continue;
            }

            _createItem = create;
            _singletonInstance = instance;
            return;
        }
    }

    private static void NoteMiss(string templateId, bool permanent)
    {
        Misses.TryGetValue(templateId, out Miss miss);
        miss.Attempts++;
        miss.LastTry = Time.unscaledTime;
        miss.Permanent = permanent || miss.Permanent;
        Misses[templateId] = miss;
    }
}
