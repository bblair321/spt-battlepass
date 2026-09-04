using System;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SptBattlePass.Client.Services;

namespace SptBattlePass.Client.Patches;

public sealed class RaidStartPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(GameWorld), "OnGameStarted");
    }

    [PatchPostfix]
    private static void PatchPostfix(GameWorld __instance)
    {
        try
        {
            FikaCompat.LogState("raid start");
            if (!FikaCompat.ShouldRunClient)
            {
                Plugin.Log.LogInfo("[BattlePass] Skipping raid tracking on Fika headless.");
                return;
            }

            bool isScavRaid = false;
            Player mainPlayer = __instance != null ? __instance.MainPlayer : null;
            if (mainPlayer != null)
            {
                isScavRaid = mainPlayer.Side == EPlayerSide.Savage;
            }

            string location = __instance != null ? __instance.LocationId : "";
            bool isNight = RaidProgress.IsNightNow();
            RaidProgress.Start(location ?? "", isScavRaid, isNight);
            Plugin.Log.LogInfo($"[BattlePass] raid start loc={location} scav={isScavRaid} night={isNight} id={RaidProgress.RaidId}");
            Plugin.OnRaidStarted();
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError($"[BattlePass] Raid start tracking failed: {exception.Message}");
        }
    }
}
