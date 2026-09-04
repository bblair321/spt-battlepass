using System;
using System.Collections.Generic;
using System.Reflection;
using EFT;

namespace SptBattlePass.Client.Services;

internal static class SoundUtil
{
    private static MethodInfo _play;
    private static PropertyInfo _instance;
    private static Type _enumType;
    private static bool _ready;
    private static bool _ok;
    private static readonly Dictionary<string, object> _resolved = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

    public static void Init()
    {
        if (_ready)
        {
            return;
        }

        _ready = true;
        try
        {
            Assembly assembly = typeof(GameWorld).Assembly;
            Type type = assembly.GetType("GUISounds") ?? assembly.GetType("EFT.UI.GUISounds");
            if (type == null)
            {
                return;
            }

            _play = type.GetMethod("PlayUISound", BindingFlags.Instance | BindingFlags.Public);
            if (_play == null)
            {
                return;
            }

            _instance = type.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public);
            if (_instance == null)
            {
                return;
            }

            _enumType = _play.GetParameters()[0].ParameterType;
            _ok = true;
        }
        catch (Exception exception)
        {
            Plugin.Log?.LogDebug($"[BattlePass] SoundUtil init failed: {exception.Message}");
        }
    }

    public static void Play(params string[] names)
    {
        if (!_ok || !BattlePassSettings.Sounds || names == null || names.Length == 0)
        {
            return;
        }

        try
        {
            object value = _instance.GetValue(null);
            if (value == null)
            {
                return;
            }

            foreach (string name in names)
            {
                object sound = Resolve(name);
                if (sound == null)
                {
                    continue;
                }

                try
                {
                    _play.Invoke(value, new object[] { sound });
                    return;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private static object Resolve(string name)
    {
        if (_resolved.TryGetValue(name, out object cached))
        {
            return cached;
        }

        object parsed = null;
        try
        {
            parsed = Enum.Parse(_enumType, name, true);
        }
        catch
        {
        }

        _resolved[name] = parsed;
        return parsed;
    }
}
