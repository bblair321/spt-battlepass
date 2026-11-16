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
    private static readonly Dictionary<string, int> NamedBosses = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string> ParentAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["meds"] = "543be5664bdc2dd4348b4569",
        ["medkit"] = "5448f39d4bdc2d0a728b4568",
        ["medikit"] = "5448f39d4bdc2d0a728b4568",
        ["drugs"] = "5448f3a14bdc2d27728b4569",
        ["barter"] = "5448eb774bdc2d0a728b4567",
        ["electronics"] = "57864a66245977548f04a81f",
        ["building"] = "57864ada245977548638de91",
        ["info"] = "5448ecbe4bdc2d60728b4568"
    };

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
    public static string ExtractName { get; private set; } = "";

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
        NamedBosses.Clear();
        ExtractName = "";
    }

    public static void SetExtract(string extract)
    {
        ExtractName = extract ?? "";
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
        bool isGrenade,
        string role = "")
    {
        if (!Active)
        {
            return;
        }

        if (isBoss)
        {
            BossKills++;
            if (!string.IsNullOrWhiteSpace(role))
            {
                Add(NamedBosses, role.Trim());
            }
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

    public static Dictionary<string, int> CopyNamedBosses() => Copy(NamedBosses);

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
            "KillBoss" => NamedBossDelta(challenge.Boss),
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
            "FindInRaidCategory" => CountFirByParent(challenge.Parent),
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
            "KillScavs" or "KillPmcs" or "KillBosses" or "KillBoss" or "KillRaiders" or "KillRogues"
                or "KillCultists" or "Headshots" or "HeadshotPmcs"
                or "Survive" or "SurviveCount"
                or "KillWeapon" or "KillScavsWeapon" or "KillPmcsWeapon"
                or "KillMelee" or "KillGrenade" or "HeadshotWeapon"
                or "FindInRaid" or "FindInRaidCategory" or "ExtractNamed" => true,
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

    private static int NamedBossDelta(string required)
    {
        if (string.IsNullOrWhiteSpace(required))
        {
            return 0;
        }

        int total = 0;
        foreach (KeyValuePair<string, int> pair in NamedBosses)
        {
            if (BossMatches(pair.Key, required))
            {
                total += pair.Value;
            }
        }

        return total;
    }

    private static bool BossMatches(string role, string required)
    {
        string have = NormalizeBoss(role);
        string want = NormalizeBoss(required);
        return have.Length > 0 && want.Length > 0 && (have == want || have.IndexOf(want, StringComparison.Ordinal) >= 0 || want.IndexOf(have, StringComparison.Ordinal) >= 0);
    }

    private static string NormalizeBoss(string role)
    {
        string value = (role ?? "").Trim().ToLowerInvariant();
        if (value.StartsWith("boss", StringComparison.Ordinal))
        {
            value = value.Substring(4);
        }

        return value.Trim();
    }

    private static int CountFirByParent(string parent)
    {
        string want = ResolveParent(parent);
        if (!Active || want.Length == 0)
        {
            return 0;
        }

        int total = 0;
        try
        {
            GameWorld world = Singleton<GameWorld>.Instance;
            Player player = world != null ? world.MainPlayer : null;
            Inventory inventory = player?.InventoryController?.Inventory;
            if (inventory == null)
            {
                return 0;
            }

            foreach (Item item in inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (item == null || !item.SpawnedInSession)
                {
                    continue;
                }

                if (!ItemMatchesParent(item, want))
                {
                    continue;
                }

                total += item.StackObjectsCount > 0 ? item.StackObjectsCount : 1;
            }
        }
        catch
        {
        }

        return total;
    }

    private static string ResolveParent(string value)
    {
        string key = (value ?? "").Trim();
        if (key.Length == 0)
        {
            return "";
        }

        return ParentAliases.TryGetValue(key, out string tpl) ? tpl : key;
    }

    private static bool ItemMatchesParent(Item item, string want)
    {
        string tpl = item.StringTemplateId;
        if (string.Equals(tpl, want, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        try
        {
            object template = item.GetType().GetProperty("Template")?.GetValue(item);
            object parent = template == null
                ? null
                : template.GetType().GetProperty("_parent")?.GetValue(template)
                  ?? template.GetType().GetProperty("Parent")?.GetValue(template)
                  ?? template.GetType().GetField("_parent", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)?.GetValue(template);
            string parentId = parent?.ToString() ?? "";
            return parentId.Equals(want, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
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
