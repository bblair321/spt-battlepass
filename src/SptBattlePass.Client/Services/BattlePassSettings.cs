using System;
using BepInEx.Configuration;
using SptBattlePass.Client.Models;
using UnityEngine;

namespace SptBattlePass.Client.Services;

internal static class BattlePassSettings
{
    public static ConfigEntry<bool> WidgetEnabled { get; private set; }
    public static ConfigEntry<bool> WidgetAutoShow { get; private set; }
    public static ConfigEntry<bool> ToastsEnabled { get; private set; }
    public static ConfigEntry<bool> SoundsEnabled { get; private set; }
    public static ConfigEntry<bool> RaidSummaryEnabled { get; private set; }
    public static ConfigEntry<KeyCode> WidgetKey { get; private set; }

    public static ConfigEntry<string> PinnedChallenge { get; private set; }

    public static bool Widget => WidgetEnabled == null || WidgetEnabled.Value;
    public static bool AutoShowWidget => WidgetAutoShow == null || WidgetAutoShow.Value;
    public static bool Toasts => ToastsEnabled == null || ToastsEnabled.Value;
    public static bool Sounds => SoundsEnabled == null || SoundsEnabled.Value;
    public static bool RaidSummary => RaidSummaryEnabled == null || RaidSummaryEnabled.Value;

    public static void Bind(ConfigFile config)
    {
        WidgetEnabled = config.Bind("UI", "InRaidWidget", true, "Show the in-raid challenge widget.");
        WidgetAutoShow = config.Bind("UI", "InRaidWidgetAutoShow", true, "Slide the widget in at raid start and when a challenge moves.");
        WidgetKey = config.Bind("UI", "InRaidWidgetKey", KeyCode.F8, "In-raid: show or hide the battle pass challenge widget.");
        ToastsEnabled = config.Bind("UI", "ChallengeToasts", true, "Show challenge-complete toasts in raid.");
        SoundsEnabled = config.Bind("UI", "UiSounds", true, "Play Tarkov UI sounds for battle pass actions.");
        RaidSummaryEnabled = config.Bind("UI", "RaidSummary", true, "Show the battle pass card on the raid results screen.");
        PinnedChallenge = config.Bind("UI", "PinnedChallenge", "", "Instance id of the challenge shown first on the in-raid widget.");
    }

    public static string ChallengeId(BattlePassChallengeDto challenge)
    {
        if (challenge == null)
        {
            return "";
        }

        return string.IsNullOrEmpty(challenge.InstanceId)
            ? (challenge.TemplateId ?? "") + ":" + (challenge.Name ?? "")
            : challenge.InstanceId;
    }

    public static bool IsPinned(BattlePassChallengeDto challenge)
    {
        string id = ChallengeId(challenge);
        return !string.IsNullOrEmpty(id)
               && PinnedChallenge != null
               && string.Equals(PinnedChallenge.Value, id, StringComparison.Ordinal);
    }

    public static void TogglePin(BattlePassChallengeDto challenge)
    {
        if (PinnedChallenge == null || challenge == null)
        {
            return;
        }

        string id = ChallengeId(challenge);
        PinnedChallenge.Value = IsPinned(challenge) ? "" : id;
    }
}
