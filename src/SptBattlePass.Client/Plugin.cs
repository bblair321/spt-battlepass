using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EFT.UI.SessionEnd;
using SptBattlePass.Client.Models;
using SptBattlePass.Client.Patches;
using SptBattlePass.Client.Services;
using SptBattlePass.Client.UI;
using SPT.Reflection.Patching;
using UnityEngine;
using UnityEngine.UI;

namespace SptBattlePass.Client;

[BepInPlugin("com.bblai.battlepass", "SPT Battle Pass", "0.2.1")]
[BepInDependency(FikaCompat.PluginGuid, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    private readonly List<GraphicRaycaster> _disabledRaycasters = new();
    private BattlePassPanel _panel;
    private RaidSummaryOverlay _raidSummary;
    private InRaidWidget _raidWidget;
    private ChallengeToastOverlay _toasts;
    private BattlePassStatusDto _status;
    private bool _panelWasVisible;

    public static Plugin Instance { get; private set; }
    internal static ManualLogSource Log { get; private set; }

    public static void TogglePanel()
    {
        if (Instance == null || !FikaCompat.ShouldRunClient)
        {
            return;
        }

        Instance._panel.Toggle();
    }

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        _panel = new BattlePassPanel();
        _raidSummary = new RaidSummaryOverlay();
        _raidWidget = new InRaidWidget();
        _toasts = new ChallengeToastOverlay();
        BattlePassSettings.Bind(Config);
        new InventoryScreenPatch().Enable();
        new RaidStartPatch().Enable();
        new KillTrackerPatch().Enable();
        new RaidResultPatch().Enable();
        SoundUtil.Init();
        Log.LogInfo("SPT Battle Pass client loaded. Open Character / inventory and click BATTLE PASS.");
        if (FikaCompat.IsHeadless)
        {
            Log.LogInfo("[BattlePass] Fika headless detected — UI, tab, and raid reports are disabled on this instance.");
        }
    }

    private void Start()
    {
        FikaCompat.LogState("start");
    }

    public static void ReportRaidResult(bool survived)
    {
        if (Instance == null || !FikaCompat.ShouldRunClient)
        {
            return;
        }

        Instance.StartCoroutine(Instance.ReportRaidResultCoroutine(survived));
    }

    private IEnumerator ReportRaidResultCoroutine(bool survived)
    {
        if (!FikaCompat.ShouldRunClient || !RaidProgress.Active)
        {
            yield break;
        }

        RaidResultDto result = new RaidResultDto
        {
            RaidId = RaidProgress.RaidId,
            Survived = survived,
            Location = RaidProgress.Location,
            IsScavRaid = RaidProgress.IsScavRaid,
            ScavKills = RaidProgress.ScavKills,
            PmcKills = RaidProgress.PmcKills,
            BossKills = RaidProgress.BossKills,
            RaiderKills = RaidProgress.RaiderKills,
            RogueKills = RaidProgress.RogueKills,
            CultistKills = RaidProgress.CultistKills,
            Headshots = RaidProgress.Headshots,
            PmcHeadshots = RaidProgress.PmcHeadshots,
            MeleeKills = RaidProgress.MeleeKills,
            GrenadeKills = RaidProgress.GrenadeKills,
            IsNight = RaidProgress.IsNight,
            WeaponKills = RaidProgress.CopyWeaponKills(),
            WeaponScavKills = RaidProgress.CopyWeaponScavKills(),
            WeaponPmcKills = RaidProgress.CopyWeaponPmcKills(),
            WeaponHeadshots = RaidProgress.CopyWeaponHeadshots(),
            FirItems = RaidProgress.CopyFirItems()
        };
        RaidProgress.End();
        _raidWidget.Hide();
        _toasts.Hide();

        Log.LogInfo($"[BattlePass] Reporting raid survived={survived} loc={result.Location} scavRaid={result.IsScavRaid} night={result.IsNight} scav={result.ScavKills} pmc={result.PmcKills} boss={result.BossKills} raider={result.RaiderKills} rogue={result.RogueKills} cultist={result.CultistKills} hs={result.Headshots} pmcHs={result.PmcHeadshots} melee={result.MeleeKills} nade={result.GrenadeKills}");
        Task<RaidEndResultDto> task = BattlePassApi.ReportRaidAsync(result);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted || task.Result == null)
        {
            Log.LogError($"[BattlePass] Raid report failed: {task.Exception?.GetBaseException().Message ?? "empty raidend payload"}");
            yield break;
        }

        RaidEndResultDto raidEnd = task.Result;
        if (raidEnd.Status != null)
        {
            ApplyStatus(raidEnd.Status);
        }

        if (raidEnd.Duplicate || !HasRaidFeedback(raidEnd))
        {
            yield break;
        }

        yield return new WaitForSecondsRealtime(0.45f);
        if (FindObjectOfType<SessionResultExitStatus>() == null)
        {
            yield break;
        }

        if (!BattlePassSettings.RaidSummary)
        {
            yield break;
        }

        _raidSummary.Show(raidEnd);
        bool completed = raidEnd.MonthlyBonus > 0 || raidEnd.MonthlyBonusXp > 0;
        if (raidEnd.Updates != null)
        {
            foreach (RaidChallengeUpdateDto update in raidEnd.Updates)
            {
                if (update.Completed)
                {
                    completed = true;
                    break;
                }
            }
        }

        if (completed)
        {
            SoundUtil.Play("AchievementCompleted", "QuestCompleted", "QuestFinished", "ButtonClick");
        }
        else
        {
            SoundUtil.Play("QuestUpdated", "TaskMarkerFound", "ButtonClick");
        }

        Log.LogInfo($"[BattlePass] Raid feedback tickets=+{raidEnd.TicketsEarned} xp=+{raidEnd.XpEarned} updates={raidEnd.Updates?.Count ?? 0}");
    }

    private static bool HasRaidFeedback(RaidEndResultDto raidEnd)
    {
        if (raidEnd.TicketsEarned > 0 || raidEnd.MonthlyBonus > 0 || raidEnd.XpEarned > 0)
        {
            return true;
        }

        return raidEnd.Updates != null && raidEnd.Updates.Count > 0;
    }

    public static void OnRaidStarted()
    {
        if (Instance == null || !FikaCompat.ShouldRunClient)
        {
            return;
        }

        Instance.PrefetchStatus();
        Instance._toasts.Reset();
        if (BattlePassSettings.Widget && BattlePassSettings.AutoShowWidget)
        {
            Instance._raidWidget.ShowAuto();
        }
    }

    public static void OnRaidKill()
    {
        if (!FikaCompat.ShouldRunClient)
        {
            return;
        }

        if (BattlePassSettings.Widget)
        {
            Instance?._raidWidget.NotifyProgress();
        }

        if (BattlePassSettings.Toasts && Instance?._status != null)
        {
            Instance._toasts.CheckCompletions(Instance._status);
        }
    }

    private void OnGUI()
    {
        if (!FikaCompat.ShouldRunClient)
        {
            return;
        }

        if (RaidProgress.Active && !_panel.IsVisible)
        {
            if (BattlePassSettings.Widget)
            {
                _raidWidget.Draw();
            }

            if (BattlePassSettings.Toasts)
            {
                _toasts.Draw();
            }
        }

        if (BattlePassSettings.RaidSummary)
        {
            _raidSummary.Draw();
        }

        _panel.Draw();
    }

    private void Update()
    {
        if (!FikaCompat.ShouldRunClient)
        {
            return;
        }

        if (BattlePassSettings.Widget
            && RaidProgress.Active
            && !_panel.IsVisible
            && BattlePassSettings.WidgetKey != null
            && Input.GetKeyDown(BattlePassSettings.WidgetKey.Value))
        {
            _raidWidget.Toggle();
            SoundUtil.Play("ButtonClick", "TabButton");
        }

        if (_raidSummary.IsVisible && FindObjectOfType<SessionResultExitStatus>() == null)
        {
            _raidSummary.Hide();
        }

        if (_panel.IsVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            _panel.Hide();
        }

        if (_panel.IsVisible != _panelWasVisible)
        {
            if (_panel.IsVisible)
            {
                DisableMenuRaycasters();
            }
            else
            {
                RestoreMenuRaycasters();
            }

            _panelWasVisible = _panel.IsVisible;
        }
    }

    public void RefreshStatus()
    {
        StartCoroutine(FetchStatus(true));
    }

    public void PrefetchStatus()
    {
        StartCoroutine(FetchStatus(false));
    }

    private IEnumerator FetchStatus(bool showLoading)
    {
        if (!FikaCompat.ShouldRunClient)
        {
            yield break;
        }

        if (showLoading)
        {
            _panel.SetLoading();
        }

        Task<BattlePassStatusDto> task = BattlePassApi.FetchStatusAsync();
        while (!task.IsCompleted)
        {
            yield return null;
        }

        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty status payload";
            Log.LogError($"[BattlePass] Status fetch failed: {message}");
            if (showLoading)
            {
                _panel.SetError("Could not load battle pass: " + message);
            }

            yield break;
        }

        ApplyStatus(task.Result);
    }

    private void ApplyStatus(BattlePassStatusDto status)
    {
        _status = status;
        _panel.SetStatus(status);
        _raidWidget.SetStatus(status);
        if (RaidProgress.Active && BattlePassSettings.Widget && BattlePassSettings.AutoShowWidget)
        {
            _raidWidget.ShowAuto();
        }
    }

    public void ApplyHudSettings()
    {
        if (!BattlePassSettings.Widget)
        {
            _raidWidget.Hide();
        }

        if (!BattlePassSettings.Toasts)
        {
            _toasts.Hide();
        }

        if (!BattlePassSettings.RaidSummary)
        {
            _raidSummary.Hide();
        }
    }

    public void BuyOffer(string offerId)
    {
        StartCoroutine(BuyOfferCoroutine(offerId));
    }

    private IEnumerator BuyOfferCoroutine(string offerId)
    {
        _panel.SetBuying(true);
        Task<BuyResultDto> task = BattlePassApi.BuyAsync(offerId);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        _panel.SetBuying(false);
        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty buy payload";
            Log.LogError($"[BattlePass] Buy failed: {message}");
            _panel.SetShopNotice("Purchase failed: " + message);
            yield break;
        }

        BuyResultDto result = task.Result;
        if (result.Status != null)
        {
            ApplyStatus(result.Status);
        }

        if (!result.Ok)
        {
            SoundUtil.Play("ErrorMessage", "ButtonClick");
            _panel.SetShopNotice(BuyErrorText(result.Error));
            yield break;
        }

        SoundUtil.Play("QuestTurnedIn", "InsuranceItemReturnedToStash", "Ready");
        string name = string.IsNullOrEmpty(result.OfferName) ? "Item" : result.OfferName;
        _panel.SetShopNotice($"{name} sent to Messages. Collect it from the SYSTEM chat.");
    }

    public void GrantTickets()
    {
        StartCoroutine(GrantTicketsCoroutine());
    }

    private IEnumerator GrantTicketsCoroutine()
    {
        _panel.SetBuying(true);
        Task<GrantResultDto> task = BattlePassApi.GrantAsync();
        while (!task.IsCompleted)
        {
            yield return null;
        }

        _panel.SetBuying(false);
        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty grant payload";
            Log.LogError($"[BattlePass] Grant failed: {message}");
            _panel.SetShopNotice("Grant failed: " + message);
            yield break;
        }

        GrantResultDto result = task.Result;
        if (result.Status != null)
        {
            ApplyStatus(result.Status);
        }

        if (!result.Ok)
        {
            SoundUtil.Play("ErrorMessage", "ButtonClick");
            _panel.SetShopNotice(result.Error == "disabled" ? "Debug grants are turned off in config.json." : "Grant failed.");
            yield break;
        }

        SoundUtil.Play("InsuranceItemReturnedToStash", "ButtonClick", "Ready");
        _panel.SetShopNotice($"Granted {result.Amount} tickets.");
    }

    public void Reroll(string bucket)
    {
        StartCoroutine(RerollCoroutine(bucket));
    }

    public void Handover(string instanceId)
    {
        StartCoroutine(HandoverCoroutine(instanceId));
    }

    public void UnlockPremium(bool debug)
    {
        StartCoroutine(UnlockPremiumCoroutine(debug));
    }

    private IEnumerator UnlockPremiumCoroutine(bool debug)
    {
        _panel.SetBuying(true);
        Task<PremiumResultDto> task = BattlePassApi.UnlockPremiumAsync(debug);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        _panel.SetBuying(false);
        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty premium payload";
            Log.LogError($"[BattlePass] Premium unlock failed: {message}");
            _panel.SetShopNotice("Premium unlock failed: " + message);
            yield break;
        }

        PremiumResultDto result = task.Result;
        if (result.Status != null)
        {
            ApplyStatus(result.Status);
        }

        if (!result.Ok)
        {
            SoundUtil.Play("ErrorMessage", "ButtonClick");
            _panel.SetShopNotice(PremiumErrorText(result.Error));
            yield break;
        }

        StashSync.Apply(result.StashChanges);
        SoundUtil.Play("QuestTurnedIn", "InsuranceItemReturnedToStash", "Ready");
        _panel.SetShopNotice("Premium unlocked. Rewards for levels you already reached were mailed.");
    }

    private IEnumerator HandoverCoroutine(string instanceId)
    {
        _panel.SetBuying(true);
        Task<HandoverResultDto> task = BattlePassApi.HandoverAsync(instanceId);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        _panel.SetBuying(false);
        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty handover payload";
            Log.LogError($"[BattlePass] Handover failed: {message}");
            _panel.SetShopNotice("Turn-in failed: " + message);
            yield break;
        }

        HandoverResultDto result = task.Result;
        if (result.Status != null)
        {
            ApplyStatus(result.Status);
        }

        if (!result.Ok)
        {
            SoundUtil.Play("ErrorMessage", "ButtonClick");
            _panel.SetShopNotice(HandoverErrorText(result.Error));
            yield break;
        }

        StashSync.Apply(result.StashChanges);
        SoundUtil.Play("QuestTurnedIn", "InsuranceItemReturnedToStash", "Ready");
        _panel.SetShopNotice($"Turned in {result.TurnedIn}.");
    }

    private IEnumerator RerollCoroutine(string bucket)
    {
        _panel.SetBuying(true);
        Task<RerollResultDto> task = BattlePassApi.RerollAsync(bucket);
        while (!task.IsCompleted)
        {
            yield return null;
        }

        _panel.SetBuying(false);
        if (task.IsFaulted || task.Result == null)
        {
            string message = task.Exception?.GetBaseException().Message ?? "Empty reroll payload";
            Log.LogError($"[BattlePass] Reroll failed: {message}");
            _panel.SetShopNotice("Reroll failed: " + message);
            yield break;
        }

        RerollResultDto result = task.Result;
        if (result.Status != null)
        {
            ApplyStatus(result.Status);
        }

        if (!result.Ok)
        {
            SoundUtil.Play("ErrorMessage", "ButtonClick");
            _panel.SetShopNotice(RerollErrorText(result.Error));
            yield break;
        }

        SoundUtil.Play("QuestUpdated", "TaskMarkerFound", "ButtonClick");
        string label = result.Bucket == "weekly" ? "Weekly" : "Daily";
        _panel.SetShopNotice($"{label} challenges rerolled.");
    }

    private static string RerollErrorText(string error)
    {
        return error switch
        {
            "insufficient_tickets" => "Not enough tickets to reroll.",
            "already_complete" => "Can't reroll after a challenge in that set is complete.",
            "max_rerolls" => "No rerolls left for this period.",
            "disabled" => "Rerolls are turned off in config.json.",
            "unknown_bucket" => "That set can't be rerolled.",
            _ => "Reroll failed."
        };
    }

    private static string PremiumErrorText(string error)
    {
        return error switch
        {
            "insufficient_roubles" => "Not enough roubles in stash.",
            "already_premium" => "Premium is already unlocked this month.",
            "disabled" => "Debug unlock is turned off in config.json.",
            _ => "Premium unlock failed."
        };
    }

    private static string HandoverErrorText(string error)
    {
        return error switch
        {
            "insufficient_items" => "Not enough of that item in stash.",
            "unknown_challenge" => "That challenge is no longer active.",
            "already_complete" => "Already turned in.",
            "wrong_type" => "That challenge is not a turn-in.",
            _ => "Turn-in failed."
        };
    }

    private static string BuyErrorText(string error)
    {
        return error switch
        {
            "insufficient_tickets" => "Not enough tickets.",
            "out_of_stock" => "Sold out this season.",
            "unknown_offer" => "That offer is no longer available.",
            "invalid_item" => "Could not create that item.",
            _ => "Purchase failed."
        };
    }

    private void DisableMenuRaycasters()
    {
        _disabledRaycasters.Clear();
        foreach (GraphicRaycaster raycaster in FindObjectsOfType<GraphicRaycaster>())
        {
            if (raycaster != null && raycaster.enabled)
            {
                raycaster.enabled = false;
                _disabledRaycasters.Add(raycaster);
            }
        }
    }

    private void RestoreMenuRaycasters()
    {
        foreach (GraphicRaycaster raycaster in _disabledRaycasters)
        {
            if (raycaster != null)
            {
                raycaster.enabled = true;
            }
        }

        _disabledRaycasters.Clear();
    }

    private void OnDestroy()
    {
        RestoreMenuRaycasters();
    }
}
