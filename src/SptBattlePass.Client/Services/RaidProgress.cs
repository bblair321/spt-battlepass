using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using SptBattlePass.Client.Models;

namespace SptBattlePass.Client.Services;

public static class RaidProgress
{
    private static readonly Dictionary<string, int> WeaponAll = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> WeaponScavs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> WeaponPmcs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> WeaponHeadshots = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    public static bool Active { get; private set; }
    public static string RaidId { get; private set; } = "";
    public static string Location { get; private set; } = "";
    public static bool IsScavRaid { get; private set; }
    public static bool IsNight { get; private set; }
    public static int ScavKills { get; private set; }
    public static int PmcKills { get; private set; }
    public static int BossKills { get; private set; }
    public static int RaiderKills { get; private set; }
    public static int RogueKills { get; private set; }
    public static int CultistKills { get; private set; }
    public static int Headshots { get; private set; }
    public static int PmcHeadshots { get; private set; }
    public static int MeleeKills { get; private set; }
    public static int GrenadeKills { get; private set; }

    public static void Start(string location, bool isScavRaid, bool isNight)
    {
        Active = true;
        RaidId = Guid.NewGuid().ToString("N");
        Location = location ?? "";
        IsScavRaid = isScavRaid;
        IsNight = isNight;
        ScavKills = 0;
        PmcKills = 0;
        BossKills = 0;
        RaiderKills = 0;
        RogueKills = 0;
        CultistKills = 0;
        Headshots = 0;
        PmcHeadshots = 0;
        MeleeKills = 0;
        GrenadeKills = 0;
        WeaponAll.Clear();
        WeaponScavs.Clear();
        WeaponPmcs.Clear();
        WeaponHeadshots.Clear();
    }

    public static void RegisterKill(
        bool isScav,
        bool isPmc,
        bool isBoss,
        bool isRaider,
        bool isRogue,
        bool isCultist,
        bool isHeadshot,
        string weaponClass,
        bool isMelee,
        bool isGrenade)
    {
        if (!Active)
        {
            return;
        }

        if (isBoss)
        {
            BossKills++;
        }
        else if (isRaider)
        {
            RaiderKills++;
        }
        else if (isRogue)
        {
            RogueKills++;
        }
        else if (isPmc)
        {
            PmcKills++;
        }
        else if (isScav)
        {
            ScavKills++;
        }

        if (isCultist)
        {
            CultistKills++;
        }

        if (isMelee)
        {
            MeleeKills++;
        }

        if (isGrenade)
        {
            GrenadeKills++;
        }

        if (isHeadshot)
        {
            Headshots++;
            if (isPmc)
            {
                PmcHeadshots++;
            }
        }

        string weapon = isGrenade ? "" : NormalizeWeapon(weaponClass);
        if (isMelee && weapon.Length == 0)
        {
            weapon = "melee";
        }

        if (weapon.Length == 0)
        {
            return;
        }

        Add(WeaponAll, weapon);
        if (isScav)
        {
            Add(WeaponScavs, weapon);
        }

        if (isPmc)
        {
            Add(WeaponPmcs, weapon);
        }

        if (isHeadshot)
        {
            Add(WeaponHeadshots, weapon);
        }
    }

    public static Dictionary<string, int> CopyWeaponKills() => Copy(WeaponAll);

    public static Dictionary<string, int> CopyWeaponScavKills() => Copy(WeaponScavs);

    public static Dictionary<string, int> CopyWeaponPmcKills() => Copy(WeaponPmcs);

    public static Dictionary<string, int> CopyWeaponHeadshots() => Copy(WeaponHeadshots);

    public static Dictionary<string, int> CopyFirItems() => CountFirItems();

    public static void End()
    {
        Active = false;
    }

    public static int DeltaFor(BattlePassChallengeDto challenge)
    {
        if (!Active || challenge == null || !TimeMatches(challenge.TimeOfDay))
        {
            return 0;
        }

        bool onMap = LocationMatches(Location, challenge.Map);
        return challenge.Type switch
        {
            "KillScavs" => ScavKills,
            "KillPmcs" => PmcKills,
            "KillBosses" => BossKills,
            "KillRaiders" => RaiderKills,
            "KillRogues" => RogueKills,
            "KillCultists" => CultistKills,
            "Headshots" => Headshots,
            "HeadshotPmcs" => PmcHeadshots,
            "KillMelee" => MeleeKills,
            "KillGrenade" => GrenadeKills,
            "KillScavsMap" => onMap ? ScavKills : 0,
            "KillPmcsMap" => onMap ? PmcKills : 0,
            "HeadshotsMap" => onMap ? Headshots : 0,
            "KillWeapon" => Count(WeaponAll, challenge.Weapon),
            "KillScavsWeapon" => Count(WeaponScavs, challenge.Weapon),
            "KillPmcsWeapon" => Count(WeaponPmcs, challenge.Weapon),
            "HeadshotWeapon" => Count(WeaponHeadshots, challenge.Weapon),
            "FindInRaid" => Count(CountFirItems(), challenge.Tpl),
            _ => 0
        };
    }

    public static bool CanProgress(BattlePassChallengeDto challenge)
    {
        if (!Active || challenge == null || challenge.Completed)
        {
            return false;
        }

        if (!TimeMatches(challenge.TimeOfDay))
        {
            return false;
        }

        return challenge.Type switch
        {
            "KillScavs" or "KillPmcs" or "KillBosses" or "KillRaiders" or "KillRogues"
                or "KillCultists" or "Headshots" or "HeadshotPmcs"
                or "Survive" or "SurviveCount"
                or "KillWeapon" or "KillScavsWeapon" or "KillPmcsWeapon"
                or "KillMelee" or "KillGrenade" or "HeadshotWeapon"
                or "FindInRaid" => true,
            "SurvivePmc" => !IsScavRaid,
            "SurviveDay" => !IsNight,
            "SurviveNight" => IsNight,
            "SurviveScav" => IsScavRaid,
            "ExtractMap" or "KillScavsMap" or "KillPmcsMap" or "HeadshotsMap" =>
                LocationMatches(Location, challenge.Map),
            _ => false
        };
    }

    public static bool LocationMatches(string raidLocation, string requiredMap)
    {
        if (string.IsNullOrWhiteSpace(requiredMap))
        {
            return false;
        }

        return NormalizeLocation(raidLocation) == NormalizeLocation(requiredMap);
    }

    public static bool IsNightNow()
    {
        try
        {
            if (TOD_Sky.Instantiated)
            {
                return TOD_Sky.Instance.IsNight;
            }
        }
        catch
        {
        }

        return false;
    }

    private static bool TimeMatches(string timeOfDay)
    {
        if (string.IsNullOrWhiteSpace(timeOfDay))
        {
            return true;
        }

        bool wantNight = timeOfDay.Equals("night", StringComparison.OrdinalIgnoreCase);
        return wantNight == IsNight;
    }

    private static Dictionary<string, int> CountFirItems()
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (!Active)
        {
            return counts;
        }

        try
        {
            GameWorld world = Singleton<GameWorld>.Instance;
            Player player = world != null ? world.MainPlayer : null;
            Inventory inventory = player?.InventoryController?.Inventory;
            if (inventory == null)
            {
                return counts;
            }

            foreach (Item item in inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (item == null || !item.SpawnedInSession)
                {
                    continue;
                }

                string tpl = item.StringTemplateId;
                if (string.IsNullOrWhiteSpace(tpl))
                {
                    continue;
                }

                int stack = item.StackObjectsCount > 0 ? item.StackObjectsCount : 1;
                counts.TryGetValue(tpl, out int current);
                counts[tpl] = current + stack;
            }
        }
        catch
        {
        }

        return counts;
    }

    private static void Add(Dictionary<string, int> counts, string weapon)
    {
        counts.TryGetValue(weapon, out int current);
        counts[weapon] = current + 1;
    }

    private static int Count(Dictionary<string, int> counts, string weapon)
    {
        string key = NormalizeWeapon(weapon);
        if (key.Length == 0)
        {
            return 0;
        }

        counts.TryGetValue(key, out int value);
        return value;
    }

    private static Dictionary<string, int> Copy(Dictionary<string, int> source)
    {
        return new Dictionary<string, int>(source, StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeWeapon(string weapon)
    {
        return (weapon ?? "").Trim().ToLowerInvariant();
    }

    private static string NormalizeLocation(string location)
    {
        string value = (location ?? "").Trim().ToLowerInvariant();
        if (value.StartsWith("factory4", StringComparison.Ordinal))
        {
            return "factory4";
        }

        if (value.StartsWith("sandbox", StringComparison.Ordinal))
        {
            return "sandbox";
        }

        return value;
    }
}
