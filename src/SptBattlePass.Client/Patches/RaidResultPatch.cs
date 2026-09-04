using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.UI.SessionEnd;
using SPT.Reflection.Patching;
using SptBattlePass.Client.Services;

namespace SptBattlePass.Client.Patches;

public sealed class RaidResultPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        MethodBase method = typeof(SessionResultExitStatus)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(candidate => candidate.Name == "Show" && candidate.GetParameters().Length == 7);
        if (method == null)
        {
            throw new InvalidOperationException("SessionResultExitStatus.Show overload was not found.");
        }

        return method;
    }

    [PatchPostfix]
    private static void PatchPostfix(ExitStatus __3)
    {
        try
        {
            if (!FikaCompat.ShouldRunClient)
            {
                return;
            }

            bool survived = __3 == ExitStatus.Survived || __3 == ExitStatus.Runner;
            Plugin.ReportRaidResult(survived);
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError($"[BattlePass] Raid result report failed: {exception.Message}");
        }
    }
}
