using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using EFT.UI;
using SPT.Reflection.Patching;
using SptBattlePass.Client.Services;
using UnityEngine;

namespace SptBattlePass.Client.Patches;

public sealed class InventoryScreenPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        MethodBase method = typeof(InventoryScreen)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(candidate => candidate.Name == "Show" && candidate.GetParameters().Length > 1);
        if (method == null)
        {
            throw new InvalidOperationException("InventoryScreen.Show overload was not found.");
        }

        return method;
    }

    [PatchPostfix]
    private static void PatchPostfix()
    {
        if (Plugin.Instance == null || !FikaCompat.ShouldRunClient)
        {
            return;
        }

        Plugin.Instance.StartCoroutine(InjectNextFrame());
    }

    private static IEnumerator InjectNextFrame()
    {
        yield return null;
        if (!FikaCompat.ShouldRunClient)
        {
            yield break;
        }

        try
        {
            UI.BattlePassTabInjector.TryInject();
            Plugin.Instance.PrefetchStatus();
        }
        catch (Exception exception)
        {
            Plugin.Log.LogError($"[BattlePass] Tab inject failed: {exception}");
        }
    }
}
