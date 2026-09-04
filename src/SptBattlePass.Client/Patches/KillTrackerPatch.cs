using System;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using SptBattlePass.Client.Services;

namespace SptBattlePass.Client.Patches;

public sealed class KillTrackerPatch : ModulePatch
{
    private static FieldInfo _weaponField;

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(Player), "OnBeenKilledByAggressor");
    }

    [PatchPostfix]
    private static void PatchPostfix(
        Player __instance,
        IPlayer aggressor,
        EBodyPart bodyPart,
        EDamageType lethalDamageType,
        object[] __args)
    {
        try
        {
            if (!RaidProgress.Active || __instance == null || __instance.IsYourPlayer)
            {
                return;
            }

            if (!FikaCompat.ShouldRunClient)
            {
                return;
            }

            if (!FikaCompat.IsLocalKiller(aggressor))
            {
                return;
            }

            if (FikaCompat.IsHumanTeammate(__instance))
            {
                return;
            }

            string role = "";
            try
            {
                role = __instance.Profile?.Info?.Settings?.Role.ToString() ?? "";
            }
            catch
            {
                role = "";
            }

            bool isBoss = role.StartsWith("boss", StringComparison.OrdinalIgnoreCase);
            bool isRaider =
                role.IndexOf("pmcbot", StringComparison.OrdinalIgnoreCase) >= 0
                || role.IndexOf("raider", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isRogue =
                role.IndexOf("exusec", StringComparison.OrdinalIgnoreCase) >= 0
                || role.IndexOf("rogue", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isCultist =
                role.IndexOf("sectant", StringComparison.OrdinalIgnoreCase) >= 0
                || role.IndexOf("sectact", StringComparison.OrdinalIgnoreCase) >= 0
                || role.IndexOf("cultist", StringComparison.OrdinalIgnoreCase) >= 0;
            bool isPmc = (__instance.Side == EPlayerSide.Usec || __instance.Side == EPlayerSide.Bear)
                         && !isBoss
                         && !isRaider
                         && !isRogue;
            bool isScav = __instance.Side == EPlayerSide.Savage
                          && !isBoss
                          && !isRaider
                          && !isRogue
                          && !isCultist;
            bool isHeadshot = bodyPart == EBodyPart.Head;
            bool isMelee = lethalDamageType == EDamageType.Melee;
            bool isGrenade = lethalDamageType == EDamageType.GrenadeFragment
                             || lethalDamageType == EDamageType.Explosion;
            object damageInfo = __args != null && __args.Length > 1 ? __args[1] : null;
            string weaponClass = isGrenade ? "" : WeaponClassOf(aggressor, damageInfo);

            RaidProgress.RegisterKill(
                isScav,
                isPmc,
                isBoss,
                isRaider,
                isRogue,
                isCultist,
                isHeadshot,
                weaponClass,
                isMelee,
                isGrenade);
            if (FikaCompat.IsFikaLoaded)
            {
                Plugin.Log.LogInfo(
                    $"[BattlePass] Fika kill victim={role} scav={isScav} pmc={isPmc} boss={isBoss} hs={isHeadshot} melee={isMelee} nade={isGrenade} weap={weaponClass}");
            }

            Plugin.OnRaidKill();
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError($"[BattlePass] Kill tracking failed: {exception.Message}");
        }
    }

    private static string WeaponClassOf(IPlayer aggressor, object damageInfo)
    {
        try
        {
            Item weapon = WeaponFromDamage(damageInfo);
            if (weapon is Weapon fromDamage && fromDamage.WeapClass != null)
            {
                return fromDamage.WeapClass.ToString();
            }
        }
        catch
        {
        }

        try
        {
            if (aggressor is Player player)
            {
                Item item = player.HandsController?.Item;
                if (item is Weapon gun && gun.WeapClass != null)
                {
                    return gun.WeapClass.ToString();
                }
            }
        }
        catch
        {
        }

        return "";
    }

    private static Item WeaponFromDamage(object damageInfo)
    {
        if (damageInfo == null)
        {
            return null;
        }

        if (_weaponField == null)
        {
            _weaponField = damageInfo.GetType().GetField(
                "Weapon",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        return _weaponField?.GetValue(damageInfo) as Item;
    }
}
