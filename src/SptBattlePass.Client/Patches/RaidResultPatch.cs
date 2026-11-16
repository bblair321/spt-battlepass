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
    private static void PatchPostfix(object[] __args, ExitStatus __3)
    {
        try
        {
            if (!FikaCompat.ShouldRunClient)
            {
                return;
            }

            if (__args != null)
            {
                foreach (object arg in __args)
                {
                    if (arg is string text
                        && text.Length > 1
                        && text.Length < 80
                        && text.IndexOf('/') < 0
                        && text.IndexOf('\\') < 0)
                    {
                        RaidProgress.SetExtract(text);
                    }
                }
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
