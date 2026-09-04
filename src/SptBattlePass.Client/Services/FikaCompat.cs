using System;
using System.Collections;
using System.Reflection;
using BepInEx.Bootstrap;
using Comfort.Common;
using EFT;

namespace SptBattlePass.Client.Services;

internal static class FikaCompat
{
    public const string PluginGuid = "com.fika.core";

    private static bool _probed;
    private static bool _loaded;
    private static Func<bool> _headlessGetter;
    private static Func<bool> _hostGetter;
    private static MethodInfo _tryGetCoopHandler;
    private static PropertyInfo _humanPlayers;

    public static bool IsFikaLoaded
    {
        get
        {
            Probe();
            return _loaded;
        }
    }

    public static bool IsHeadless
    {
        get
        {
            Probe();
            return _headlessGetter?.Invoke() ?? false;
        }
    }

    public static bool IsRaidHost
    {
        get
        {
            Probe();
            return _hostGetter?.Invoke() ?? false;
        }
    }

    public static bool ShouldRunClient => !IsHeadless;

    public static string StatusLine
    {
        get
        {
            if (!IsFikaLoaded)
            {
                return "Fika is not loaded. This profile talks to your local SPT server.";
            }

            if (IsHeadless)
            {
                return "This is a Fika headless host. Battle Pass UI and raid reports are off here so they do not write the dedicated profile.";
            }

            if (IsRaidHost)
            {
                return "Fika host. Your kills count on your profile. Teammates need this client plugin too; the SPT host needs the server mod.";
            }

            return "Fika client. Your kills count on your profile. The SPT host must have the Battle Pass server mod.";
        }
    }

    public static void LogState(string context)
    {
        Probe();
        if (Plugin.Log == null)
        {
            return;
        }

        if (!_loaded)
        {
            Plugin.Log.LogInfo($"[BattlePass] Fika ({context}): not loaded (solo SPT).");
            return;
        }

        Plugin.Log.LogInfo(
            $"[BattlePass] Fika ({context}): loaded=true headless={IsHeadless} host={IsRaidHost} runClient={ShouldRunClient}");
    }

    public static bool IsLocalKiller(IPlayer aggressor)
    {
        if (aggressor == null)
        {
            return false;
        }

        try
        {
            if (aggressor.IsYourPlayer)
            {
                return true;
            }
        }
        catch
        {
        }

        try
        {
            GameWorld world = Singleton<GameWorld>.Instance;
            Player local = world != null ? world.MainPlayer : null;
            if (local != null && !string.IsNullOrEmpty(local.ProfileId) && aggressor.ProfileId == local.ProfileId)
            {
                return true;
            }
        }
        catch
        {
        }

        return false;
    }

    public static bool IsHumanTeammate(Player victim)
    {
        if (victim == null || !IsFikaLoaded)
        {
            return false;
        }

        try
        {
            if (victim.IsYourPlayer)
            {
                return true;
            }
        }
        catch
        {
        }

        object handler = GetCoopHandler();
        if (handler == null || _humanPlayers == null)
        {
            return false;
        }

        try
        {
            if (!(_humanPlayers.GetValue(handler) is IEnumerable humans))
            {
                return false;
            }

            string victimId = victim.ProfileId;
            foreach (object human in humans)
            {
                if (human == null)
                {
                    continue;
                }

                string id = ProfileIdOf(human);
                if (!string.IsNullOrEmpty(id) && id == victimId)
                {
                    return true;
                }
            }
        }
        catch
        {
        }

        return false;
    }

    private static string ProfileIdOf(object player)
    {
        try
        {
            PropertyInfo property = player.GetType().GetProperty("ProfileId", BindingFlags.Public | BindingFlags.Instance);
            object value = property?.GetValue(player);
            return value?.ToString();
        }
        catch
        {
            return null;
        }
    }

    private static void Probe()
    {
        if (_probed)
        {
            return;
        }

        _probed = true;
        _loaded = Chainloader.PluginInfos != null && Chainloader.PluginInfos.ContainsKey(PluginGuid);
        if (!_loaded)
        {
            return;
        }

        try
        {
            Type backend = FindType("FikaBackendUtils", "Fika.Core.Main.Utils.FikaBackendUtils", "Fika.Core.Utils.FikaBackendUtils");
            if (backend == null)
            {
                Plugin.Log?.LogWarning("[BattlePass] Fika is loaded but FikaBackendUtils was not found; treating this client as a normal player.");
                return;
            }

            _headlessGetter = BoolMember(backend, "IsHeadless", "IsHeadlessGame", "IsDedicatedGame");
            _hostGetter = BoolMember(backend, "IsServer", "IsHost");
            Plugin.Log?.LogInfo(
                $"[BattlePass] Fika detected on '{backend.FullName}' (headless={_headlessGetter != null}, host={_hostGetter != null}).");

            Type coop = FindType("CoopHandler", "Fika.Core.Main.Components.CoopHandler");
            if (coop != null)
            {
                _tryGetCoopHandler = coop.GetMethod("TryGetCoopHandler", BindingFlags.Public | BindingFlags.Static);
                _humanPlayers = coop.GetProperty("HumanPlayers", BindingFlags.Public | BindingFlags.Instance);
            }
        }
        catch (Exception exception)
        {
            Plugin.Log?.LogWarning("[BattlePass] Fika reflection init failed: " + exception.Message);
        }
    }

    private static object GetCoopHandler()
    {
        Probe();
        if (_tryGetCoopHandler == null)
        {
            return null;
        }

        try
        {
            object[] args = { null };
            object ok = _tryGetCoopHandler.Invoke(null, args);
            if (ok is true)
            {
                return args[0];
            }
        }
        catch
        {
        }

        return null;
    }

    private static Func<bool> BoolMember(Type type, params string[] names)
    {
        foreach (string name in names)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.PropertyType == typeof(bool) && property.GetGetMethod(true) != null)
            {
                return () =>
                {
                    try
                    {
                        return (bool)property.GetValue(null);
                    }
                    catch
                    {
                        return false;
                    }
                };
            }

            FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(bool))
            {
                return () =>
                {
                    try
                    {
                        return (bool)field.GetValue(null);
                    }
                    catch
                    {
                        return false;
                    }
                };
            }
        }

        return null;
    }

    private static Type FindType(string simpleName, params string[] fullNames)
    {
        Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (string fullName in fullNames)
        {
            foreach (Assembly assembly in assemblies)
            {
                if (!assembly.GetName().Name.StartsWith("Fika", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                Type type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }
        }

        foreach (Assembly assembly in assemblies)
        {
            if (!assembly.GetName().Name.StartsWith("Fika", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                foreach (Type type in assembly.GetTypes())
                {
                    if (type.Name == simpleName)
                    {
                        return type;
                    }
                }
            }
            catch
            {
            }
        }

        return null;
    }
}
