using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SptBattlePass.Client.Models;
using SptBattlePass.Client.Services;
using UnityEngine;

namespace SptBattlePass.Client.UI;

public sealed class BattlePassPanel
{
    private enum View
    {
        Challenges,
        Track,
        Exchange,
        Season,
        Settings
    }

    private bool _visible;
    private View _view;
    private readonly Vector2[] _viewScroll = new Vector2[5];
    private float _layoutWidth = 900f;
    private int _lastTickets = -1;
    private float _ticketFlashUntil;
    private BattlePassStatusDto _status;
    private string _error;
    private bool _loading = true;
    private bool _buying;
    private string _shopNotice;
    private string _shopQuery = "";
    private string _shopCategory = "";
    private bool _shopAffordableOnly;
    private GUIStyle _title;
    private GUIStyle _header;
    private GUIStyle _body;
    private GUIStyle _small;
    private GUIStyle _button;
    private GUIStyle _tabOn;
    private GUIStyle _tabOff;
    private GUIStyle _chipOn;
    private GUIStyle _chipOff;
    private GUIStyle _btnBuy;
    private GUIStyle _btnClose;
    private GUIStyle _stateComplete;
    private GUIStyle _stateProgress;
    private GUIStyle _stateIdle;
    private GUIStyle _cardStyle;
    private GUIStyle _need;
    private GUIStyle _notice;
    private GUIStyle _field;
    private GUIStyle _ticket;
    private GUIStyle _ticketUnit;
    private GUIStyle _accentComplete;
    private GUIStyle _accentProgress;
    private GUIStyle _accentIdle;
    private Texture2D _panelBg;
    private Texture2D _cardBg;
    private Texture2D _accent;
    private Texture2D _rowBg;
    private Texture2D _iconBg;
    private Texture2D _barBg;
    private Texture2D _barFill;
    private Texture2D _barComplete;
    private Texture2D _barIdle;

    public bool IsVisible => _visible;

    public void Toggle()
    {
        if (_visible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        _visible = true;
        _shopNotice = null;
        SoundUtil.Play("ButtonClick", "TabButton");
        Plugin.Instance.RefreshStatus();
    }

    public void Hide()
    {
        _visible = false;
        SoundUtil.Play("MenuEscape", "ButtonClick");
    }

    public void SetStatus(BattlePassStatusDto status)
    {
        _status = status;
        _loading = false;
        _error = null;
        PrefetchShopIcons();
        PrefetchTrackIcons();
    }

    public void SetError(string message)
    {
        _error = message;
        _loading = false;
    }

    public void SetLoading()
    {
        _loading = true;
        _error = null;
    }

    public void SetBuying(bool buying)
    {
        _buying = buying;
    }

    public void SetShopNotice(string message)
    {
        _shopNotice = message;
    }

    public void Draw()
    {
        if (!_visible)
        {
            return;
        }

        EnsureStyles();
        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        float width = Mathf.Min(Screen.width - 80f, 1040f * scale);
        float height = Mathf.Min(Screen.height - 80f, 760f * scale);
        var window = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        _layoutWidth = window.width - 48f;

        Event ev = Event.current;
        if (ev != null
            && (ev.type == EventType.MouseDown
                || ev.type == EventType.MouseUp
                || ev.type == EventType.ScrollWheel))
        {
            if (!window.Contains(ev.mousePosition))
            {
                if (ev.type == EventType.MouseDown && ev.button == 0)
                {
                    Hide();
                }

                ev.Use();
                if (!_visible)
                {
                    return;
                }
            }
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), TarkovUi.OverlayTex);
        TarkovUi.Frame(window);

        GUILayout.BeginArea(new Rect(window.x + 24f, window.y + 18f, window.width - 48f, window.height - 36f));
        DrawHeader();

        string season = _status?.SeasonName ?? "Loading season...";
        GUILayout.Label(season, _header);
        if (_status != null)
        {
            DrawSeasonStrip(_status);
            DrawPremiumBanner(_status);
        }

        if (_status != null && _status.LastCrateTickets > 0)
        {
            GUILayout.Label($"Unspent tickets from {_status.LastCrateSeasonId} were mailed as a consolation crate.", _small);
        }

        if (!string.IsNullOrEmpty(_shopNotice))
        {
            GUILayout.Label(_shopNotice, _notice);
        }
        GUILayout.Space(10f);

        GUILayout.BeginHorizontal();
        int challengeDone = CountCompleted(_status?.Challenges?.Daily)
                            + CountCompleted(_status?.Challenges?.Weekly)
                            + CountCompleted(_status?.Challenges?.Monthly);
        int challengeTotal = (_status?.Challenges?.Daily?.Count ?? 0)
                             + (_status?.Challenges?.Weekly?.Count ?? 0)
                             + (_status?.Challenges?.Monthly?.Count ?? 0);
        DrawViewButton(challengeTotal > 0 ? $"CHALLENGES  {challengeDone}/{challengeTotal}" : "CHALLENGES", View.Challenges);
        GUILayout.Space(6f);
        string trackLabel = _status != null && _status.TrackMaxLevel > 0
            ? $"TRACK  {_status.TrackLevel}/{_status.TrackMaxLevel}"
            : "TRACK";
        DrawViewButton(trackLabel, View.Track);
        GUILayout.Space(6f);
        DrawViewButton("EXCHANGE", View.Exchange);
        GUILayout.Space(6f);
        DrawViewButton("SEASON", View.Season);
        GUILayout.Space(6f);
        DrawViewButton("SETTINGS", View.Settings);
        GUILayout.EndHorizontal();
        GUILayout.Space(12f);

        if (_loading)
        {
            GUILayout.Label("Fetching battle pass...", _body);
        }
        else if (!string.IsNullOrEmpty(_error))
        {
            GUILayout.Label(_error, _body);
        }
        else if (_status == null)
        {
            GUILayout.Label("No battle pass data.", _body);
        }
        else
        {
            if (_view == View.Exchange && _status.Shop != null && _status.Shop.Count > 0)
            {
                DrawShopFilters(_status.Shop);
            }

            int scrollIndex = (int)_view;
            _viewScroll[scrollIndex] = GUILayout.BeginScrollView(_viewScroll[scrollIndex]);
            if (_view == View.Challenges)
            {
                DrawChallengeGroup("DAILY", _status.Challenges?.Daily, "daily", _status.XpDaily);
                DrawChallengeGroup("WEEKLY", _status.Challenges?.Weekly, "weekly", _status.XpWeekly);
                DrawChallengeGroup("MONTHLY", _status.Challenges?.Monthly, null, _status.XpMonthly);
            }
            else if (_view == View.Track)
            {
                DrawTrack(_status);
            }
            else if (_view == View.Exchange)
            {
                DrawShop(_status.Shop);
            }
            else if (_view == View.Season)
            {
                DrawSeason(_status);
            }
            else
            {
                DrawSettings();
            }

            GUILayout.EndScrollView();
        }

        GUILayout.EndArea();
    }

    private void DrawHeader()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("BATTLE PASS", _title);
        GUILayout.FlexibleSpace();
        if (_status != null)
        {
            DrawTicketChip(_status.Tickets);
            GUILayout.Space(12f);
        }

        if (_status != null && _status.Debug)
        {
            GUI.enabled = !_buying;
            int grant = _status.GrantAmount > 0 ? _status.GrantAmount : 10;
            if (GUILayout.Button($"GRANT {grant}", _button, GUILayout.Width(110f), GUILayout.Height(28f)))
            {
                Plugin.Instance.GrantTickets();
            }

            GUI.enabled = true;
            GUILayout.Space(8f);
        }

        if (GUILayout.Button("CLOSE", _btnClose, GUILayout.Width(90f), GUILayout.Height(28f)))
        {
            Hide();
        }

        GUILayout.EndHorizontal();
    }

    private void DrawTicketChip(int tickets)
    {
        if (_lastTickets < 0)
        {
            _lastTickets = tickets;
        }
        else if (tickets != _lastTickets)
        {
            if (tickets > _lastTickets)
            {
                _ticketFlashUntil = Time.unscaledTime + 0.7f;
            }

            _lastTickets = tickets;
        }

        float flash = Mathf.Clamp01((_ticketFlashUntil - Time.unscaledTime) / 0.7f);
        Color previous = GUI.contentColor;
        GUI.contentColor = Color.Lerp(new Color(0.92f, 0.86f, 0.62f), Color.white, flash);
        GUILayout.Label(tickets.ToString("N0"), _ticket, GUILayout.Height(28f));
        GUI.contentColor = previous;
        GUILayout.Space(6f);
        GUILayout.Label("TICKETS", _ticketUnit, GUILayout.Height(28f));
    }

    private void DrawViewButton(string label, View view)
    {
        GUIStyle style = _view == view ? _tabOn : _tabOff;
        if (GUILayout.Button(label, style, GUILayout.Height(30f), GUILayout.ExpandWidth(true)))
        {
            if (_view != view)
            {
                SoundUtil.Play("ButtonClick", "TabButton");
            }

            _view = view;
        }

        if (_view == view && Event.current.type == EventType.Repaint)
        {
            Rect rect = GUILayoutUtility.GetLastRect();
            GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2f, rect.width, 2f), TarkovUi.AmberTex);
        }
    }

    private void DrawSeasonStrip(BattlePassStatusDto status)
    {
        int monthlyDone = CountCompleted(status.Challenges?.Monthly);
        int monthlyTotal = Math.Max(1, status.Challenges?.Monthly?.Count ?? 0);
        DrawProgress(monthlyDone, monthlyTotal, monthlyDone >= monthlyTotal ? "complete" : monthlyDone > 0 ? "in_progress" : "not_started");
        GUILayout.Label(
            $"{status.DaysRemaining} days left  ·  {status.ChallengesCompletedSeason} challenges  ·  {status.TicketsEarnedSeason} earned  ·  {status.TicketsSpentSeason} spent",
            _small);
    }

    private void DrawPremiumBanner(BattlePassStatusDto status)
    {
        GUILayout.Space(8f);
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        if (status.Premium)
        {
            GUILayout.Label("PREMIUM UNLOCKED", _header);
            GUILayout.Label("Extra TRACK rewards are live this month.", _small);
        }
        else
        {
            GUILayout.Label("PREMIUM TRACK", _header);
            string cost = status.PremiumCost > 0 ? status.PremiumCost.ToString("N0") : "0";
            GUILayout.Label($"Unlock the paid lane on TRACK. {cost} RUB from stash, once this month.", _small);
        }

        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        if (status.Premium)
        {
            GUILayout.Label("ACTIVE", _stateComplete, GUILayout.Height(32f));
        }
        else
        {
            GUI.enabled = !_buying;
            string cost = status.PremiumCost.ToString("N0");
            if (GUILayout.Button($"BUY PREMIUM  {cost} RUB", _btnBuy, GUILayout.Width(240f), GUILayout.Height(32f)))
            {
                Plugin.Instance.UnlockPremium(false);
            }

            GUI.enabled = true;
            if (status.Debug)
            {
                GUILayout.Space(8f);
                GUI.enabled = !_buying;
                if (GUILayout.Button("UNLOCK (DEBUG)", _button, GUILayout.Width(150f), GUILayout.Height(32f)))
                {
                    Plugin.Instance.UnlockPremium(true);
                }

                GUI.enabled = true;
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawTrack(BattlePassStatusDto status)
    {
        int max = Math.Max(0, status.TrackMaxLevel);
        int level = Math.Max(0, status.TrackLevel);
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label(max > 0 ? $"LEVEL {level} / {max}" : "LEVEL 0", _header);
        if (status.TrackXpForLevel > 0)
        {
            GUILayout.Label($"{status.TrackXpIntoLevel} / {status.TrackXpForLevel} pass XP  ·  {status.TrackXp} total", _small);
        }
        else if (max > 0 && level >= max)
        {
            GUILayout.Label($"Track complete  ·  {status.TrackXp} pass XP", _small);
        }
        else
        {
            GUILayout.Label($"{status.TrackXp} pass XP", _small);
        }

        if (!status.Premium)
        {
            GUILayout.Label("Premium is locked. Use BUY PREMIUM at the top of this panel.", _need);
        }
        GUILayout.Space(8f);
        int barCurrent = status.TrackXpForLevel > 0 ? status.TrackXpIntoLevel : 1;
        int barMax = status.TrackXpForLevel > 0 ? status.TrackXpForLevel : 1;
        string barState = status.TrackXpForLevel <= 0 && level >= max && max > 0
            ? "complete"
            : status.TrackXpIntoLevel > 0 || level > 0
                ? "in_progress"
                : "not_started";
        DrawProgress(barCurrent, barMax, barState);
        GUILayout.EndVertical();
        GUILayout.Space(10f);

        List<TrackTierStatusDto> tiers = status.Track;
        if (tiers == null || tiers.Count == 0)
        {
            GUILayout.Label("No track rewards this season.", _body);
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.Space(36f);
        GUILayout.Label("FREE", _small, GUILayout.Width(280f));
        GUILayout.FlexibleSpace();
        GUILayout.Label("PREMIUM", _small, GUILayout.Width(280f));
        GUILayout.EndHorizontal();
        GUILayout.Space(4f);

        foreach (TrackTierStatusDto tier in tiers)
        {
            DrawTrackRow(tier);
        }

        GUILayout.Space(8f);
        GUILayout.Label("Pass XP comes from completing challenges. Item rewards mail to SYSTEM. Premium is a one-time rouble unlock this month.", _small);
    }

    private void DrawTrackRow(TrackTierStatusDto tier)
    {
        bool reached = tier.Reached;
        Color previous = GUI.color;
        if (reached && (tier.Free == null || tier.Free.Claimed) && (tier.Premium == null || !tier.Premium.Locked))
        {
            GUI.color = new Color(1f, 1f, 1f, 0.55f);
        }

        GUILayout.BeginHorizontal(_cardStyle);
        GUILayout.BeginVertical(GUILayout.Width(28f));
        GUILayout.Space(10f);
        GUILayout.Label(tier.Level.ToString(), reached ? _header : _small);
        GUILayout.EndVertical();
        GUILayout.Space(8f);
        DrawTrackReward(tier.Free, 280f);
        GUILayout.FlexibleSpace();
        DrawTrackReward(tier.Premium, 280f);
        GUILayout.EndHorizontal();
        GUI.color = previous;
        GUILayout.Space(4f);
    }

    private void DrawTrackReward(TrackRewardStatusDto reward, float width)
    {
        GUILayout.BeginHorizontal(GUILayout.Width(width));
        if (reward == null)
        {
            GUILayout.Label("—", _small);
            GUILayout.EndHorizontal();
            return;
        }

        Color tint = reward.Locked
            ? new Color(1f, 1f, 1f, 0.28f)
            : reward.Claimed
                ? new Color(1f, 1f, 1f, 0.55f)
                : Color.white;
        if (!string.IsNullOrEmpty(reward.Tpl))
        {
            DrawItemIcon(reward.Tpl, 40f, tint);
        }
        else
        {
            Rect area = GUILayoutUtility.GetRect(40f, 40f, GUILayout.Width(40f), GUILayout.Height(40f));
            GUI.DrawTexture(area, _iconBg);
            GUI.Label(area, reward.Tickets > 0 ? $"+{reward.Tickets}" : "•", _small);
        }

        GUILayout.Space(8f);
        GUILayout.BeginVertical();
        GUILayout.Label(reward.Name ?? "Reward", _body);
        string meta = reward.Locked
            ? "Locked"
            : reward.Claimed
                ? "Claimed"
                : reward.Tickets > 0 && string.IsNullOrEmpty(reward.Tpl)
                    ? "Tickets"
                    : reward.Preset
                        ? "Default build"
                        : reward.Count > 1
                            ? $"{reward.Count}x"
                            : "Ready";
        GUILayout.Label(meta, reward.Locked ? _need : _small);
        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }

    private void DrawSeason(BattlePassStatusDto status)
    {
        int monthlyDone = CountCompleted(status.Challenges?.Monthly);
        int monthlyTotal = status.Challenges?.Monthly?.Count ?? 0;

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("THIS MONTH", _header);
        GUILayout.Label($"{monthlyDone} / {Math.Max(1, monthlyTotal)} monthly challenges", _body);
        DrawProgress(
            monthlyDone,
            Math.Max(1, monthlyTotal),
            monthlyDone >= monthlyTotal && monthlyTotal > 0 ? "complete" : monthlyDone > 0 ? "in_progress" : "not_started");
        GUILayout.Space(10f);
        GUILayout.BeginHorizontal();
        DrawMetric("EARNED", status.TicketsEarnedSeason.ToString());
        GUILayout.Space(8f);
        DrawMetric("SPENT", status.TicketsSpentSeason.ToString());
        GUILayout.Space(8f);
        DrawMetric("ON HAND", status.Tickets.ToString());
        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
        DrawStatRow("Challenges completed", status.ChallengesCompletedSeason.ToString());
        DrawStatRow("Daily", status.DailyCompletedSeason.ToString());
        DrawStatRow("Weekly", status.WeeklyCompletedSeason.ToString());
        DrawStatRow("Monthly", status.MonthlyCompletedSeason.ToString());
        if (status.XpEarnedSeason > 0)
        {
            DrawStatRow("XP from challenges", status.XpEarnedSeason.ToString("N0"));
        }

        if (status.TrackMaxLevel > 0)
        {
            DrawStatRow("Track level", $"{status.TrackLevel} / {status.TrackMaxLevel}");
            DrawStatRow("Pass XP", status.TrackXp.ToString());
            DrawStatRow("Premium", status.Premium ? "Unlocked" : "Locked");
        }

        GUILayout.EndVertical();
        GUILayout.Space(10f);

        if (!string.IsNullOrEmpty(status.LastSeasonId) || status.LastSeasonChallengesCompleted > 0 || status.LastCrateTickets > 0)
        {
            GUILayout.BeginVertical(_cardStyle);
            string lastName = string.IsNullOrEmpty(status.LastSeasonName) ? status.LastSeasonId : status.LastSeasonName;
            GUILayout.Label("LAST SEASON", _header);
            if (!string.IsNullOrEmpty(lastName))
            {
                GUILayout.Label(lastName, _body);
            }

            DrawStatRow("Challenges completed", status.LastSeasonChallengesCompleted.ToString());
            DrawStatRow("Tickets earned", status.LastSeasonTicketsEarned.ToString());
            if (status.LastCrateTickets > 0)
            {
                DrawStatRow("Leftover crate", $"{status.LastCrateTickets} tickets mailed");
            }

            GUILayout.EndVertical();
            GUILayout.Space(10f);
        }

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("LIFETIME", _header);
        DrawStatRow("Challenges completed", status.LifetimeChallengesCompleted.ToString());
        DrawStatRow("Tickets earned", status.LifetimeTicketsEarned.ToString());
        DrawStatRow("Tickets spent", status.LifetimeTicketsSpent.ToString());
        if (status.LifetimeXpEarned > 0)
        {
            DrawStatRow("XP from challenges", status.LifetimeXpEarned.ToString("N0"));
        }

        GUILayout.EndVertical();
        GUILayout.Space(8f);
        GUILayout.Label("Season totals start from this update. Older completions are not backfilled.", _small);
    }

    private void DrawSettings()
    {
        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("IN RAID", _header);
        DrawToggle(
            "Challenge widget",
            "Top-right live progress during a raid.",
            BattlePassSettings.WidgetEnabled);
        GUI.enabled = BattlePassSettings.Widget;
        DrawToggle(
            "Auto-show widget",
            "Slide in at raid start and when a challenge moves.",
            BattlePassSettings.WidgetAutoShow);
        GUI.enabled = true;
        string key = BattlePassSettings.WidgetKey != null ? BattlePassSettings.WidgetKey.Value.ToString() : "F8";
        DrawStatRow("Toggle key", key);
        GUILayout.Label("Rebind InRaidWidgetKey in the BepInEx config if you want a different key.", _small);
        DrawToggle(
            "Complete toasts",
            "Bottom-right popup when a challenge hits its target.",
            BattlePassSettings.ToastsEnabled);
        GUILayout.EndVertical();
        GUILayout.Space(10f);

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("MENU", _header);
        DrawToggle(
            "Raid results card",
            "Battle pass summary on the left of the results screen.",
            BattlePassSettings.RaidSummaryEnabled);
        DrawToggle(
            "UI sounds",
            "Vanilla Tarkov clicks, quest complete, and buy sounds.",
            BattlePassSettings.SoundsEnabled);
        GUILayout.EndVertical();
        GUILayout.Space(10f);

        GUILayout.BeginVertical(_cardStyle);
        GUILayout.Label("COOP", _header);
        GUILayout.Label(FikaCompat.StatusLine, _body);
        GUILayout.Space(4f);
        GUILayout.Label("Each player has their own challenges and tickets. Only your kills count. Teammates are not PMC / scav challenge kills.", _small);
        GUILayout.EndVertical();
        GUILayout.Space(8f);
        GUILayout.Label("These settings save in the BepInEx config and persist between sessions.", _small);
    }

    private void DrawToggle(string label, string hint, BepInEx.Configuration.ConfigEntry<bool> entry)
    {
        if (entry == null)
        {
            return;
        }

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical();
        GUILayout.Label(label, _body);
        GUILayout.Label(hint, _small);
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(entry.Value ? "ON" : "OFF", entry.Value ? _btnBuy : _button, GUILayout.Width(72f), GUILayout.Height(32f)))
        {
            entry.Value = !entry.Value;
            SoundUtil.Play("ButtonClick", "TabButton");
            Plugin.Instance.ApplyHudSettings();
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(8f);
    }

    private void DrawMetric(string label, string value)
    {
        GUILayout.BeginVertical(_cardStyle, GUILayout.ExpandWidth(true));
        GUILayout.Label(label, _small);
        GUILayout.Label(value, _ticket);
        GUILayout.EndVertical();
    }

    private void DrawStatRow(string label, string value)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, _small);
        GUILayout.FlexibleSpace();
        GUILayout.Label(value, _body);
        GUILayout.EndHorizontal();
    }

    private static int CountCompleted(List<BattlePassChallengeDto> challenges)
    {
        if (challenges == null)
        {
            return 0;
        }

        int count = 0;
        foreach (BattlePassChallengeDto challenge in challenges)
        {
            if (challenge != null && challenge.Completed)
            {
                count++;
            }
        }

        return count;
    }

    private void DrawChallengeGroup(string title, List<BattlePassChallengeDto> challenges, string rerollBucket, int xp)
    {
        int done = CountCompleted(challenges);
        int total = challenges?.Count ?? 0;
        GUILayout.BeginHorizontal();
        GUILayout.Label(total > 0 ? $"{title}  {done} / {total}" : title, _header);
        GUILayout.FlexibleSpace();
        DrawRerollButton(rerollBucket, challenges);
        string expiry = FormatExpiry(challenges);
        if (!string.IsNullOrEmpty(expiry))
        {
            GUILayout.Label(expiry, _small);
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(4f);
        if (challenges == null || challenges.Count == 0)
        {
            GUILayout.Label("No challenges in this set.", _small);
            GUILayout.Space(10f);
            return;
        }

        foreach (BattlePassChallengeDto challenge in challenges
                     .OrderBy(item => BattlePassSettings.IsPinned(item) ? 0 : 1)
                     .ThenBy(item => StateRank(ResolveState(item)))
                     .ThenBy(item => item.Name ?? ""))
        {
            string state = ResolveState(challenge);
            Color previousColor = GUI.color;
            if (state == "complete")
            {
                GUI.color = new Color(1f, 1f, 1f, 0.55f);
            }

            GUILayout.BeginHorizontal(_cardStyle);
            GUILayout.Box(GUIContent.none, AccentBox(state), GUILayout.Width(3f), GUILayout.ExpandHeight(true));
            GUILayout.Space(8f);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            GUILayout.Label(challenge.Name, _body);
            GUILayout.FlexibleSpace();
            bool pinned = BattlePassSettings.IsPinned(challenge);
            if (GUILayout.Button(pinned ? "PINNED" : "PIN", pinned ? _tabOn : _button, GUILayout.Width(72f), GUILayout.Height(22f)))
            {
                BattlePassSettings.TogglePin(challenge);
                SoundUtil.Play("ButtonClick", "TabButton");
            }

            GUILayout.Space(6f);
            GUILayout.Label(StateLabel(state), StateStyle(state));
            GUILayout.EndHorizontal();
            if (state != "complete" && !string.IsNullOrEmpty(challenge.Description))
            {
                GUILayout.Label(challenge.Description, _small);
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label($"{challenge.Progress} / {challenge.Target}", _small);
            GUILayout.FlexibleSpace();
            if (challenge.Type == "HandOver" && state != "complete")
            {
                GUI.enabled = !_buying;
                if (GUILayout.Button("TURN IN", _btnBuy, GUILayout.Width(88f), GUILayout.Height(22f)))
                {
                    Plugin.Instance.Handover(challenge.InstanceId);
                }

                GUI.enabled = true;
                GUILayout.Space(8f);
            }

            string reward = $"+{challenge.TicketReward} tickets";
            if (xp > 0)
            {
                reward += $"  ·  +{xp} XP";
            }

            GUILayout.Label(reward, _small);
            GUILayout.EndHorizontal();
            DrawProgress(challenge.Progress, challenge.Target, state);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUI.color = previousColor;
            GUILayout.Space(6f);
        }

        GUILayout.Space(8f);
    }

    private void DrawRerollButton(string bucket, List<BattlePassChallengeDto> challenges)
    {
        if (string.IsNullOrEmpty(bucket) || _status == null)
        {
            return;
        }

        int cost = bucket == "weekly" ? _status.WeeklyRerollCost : _status.DailyRerollCost;
        int left = bucket == "weekly" ? _status.WeeklyRerollsLeft : _status.DailyRerollsLeft;
        if (cost <= 0 || left <= 0)
        {
            return;
        }

        bool anyComplete = challenges != null && challenges.Any(challenge => challenge.Completed);
        bool canAfford = _status.Tickets >= cost;
        bool canReroll = !_buying && !anyComplete && canAfford;
        GUI.enabled = canReroll;
        if (GUILayout.Button($"REROLL {cost}  ({left} left)", _button, GUILayout.Height(24f), GUILayout.Width(150f)))
        {
            Plugin.Instance.Reroll(bucket);
        }

        GUI.enabled = true;
        GUILayout.Space(8f);
    }

    private static string ResolveState(BattlePassChallengeDto challenge)
    {
        if (!string.IsNullOrEmpty(challenge.State))
        {
            return challenge.State;
        }

        if (challenge.Completed || (challenge.Target > 0 && challenge.Progress >= challenge.Target))
        {
            return "complete";
        }

        return challenge.Progress > 0 ? "in_progress" : "not_started";
    }

    private static int StateRank(string state)
    {
        return state switch
        {
            "in_progress" => 0,
            "complete" => 2,
            _ => 1
        };
    }

    private static string StateLabel(string state)
    {
        return state switch
        {
            "complete" => "COMPLETE",
            "in_progress" => "IN PROGRESS",
            _ => "NOT STARTED"
        };
    }

    private GUIStyle StateStyle(string state)
    {
        return state switch
        {
            "complete" => _stateComplete,
            "in_progress" => _stateProgress,
            _ => _stateIdle
        };
    }

    private GUIStyle AccentBox(string state)
    {
        return state switch
        {
            "complete" => _accentComplete,
            "in_progress" => _accentProgress,
            _ => _accentIdle
        };
    }

    private static string FormatExpiry(List<BattlePassChallengeDto> challenges)
    {
        string expiresAt = challenges?.FirstOrDefault(challenge => !string.IsNullOrEmpty(challenge.ExpiresAt))?.ExpiresAt;
        if (string.IsNullOrEmpty(expiresAt)
            || !DateTime.TryParse(
                expiresAt,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime when))
        {
            return null;
        }

        TimeSpan left = when - DateTime.UtcNow;
        if (left.TotalSeconds <= 0)
        {
            return "expired";
        }

        if (left.TotalDays >= 2)
        {
            return $"{Math.Floor(left.TotalDays)}d left";
        }

        if (left.TotalHours >= 1)
        {
            return $"{Math.Floor(left.TotalHours)}h left";
        }

        return $"{Math.Max(1, Math.Floor(left.TotalMinutes))}m left";
    }

    private void DrawProgress(int progress, int target, string state)
    {
        float fraction = target <= 0 ? 0f : Mathf.Clamp01(progress / (float)target);
        Texture2D fill = state switch
        {
            "complete" => _barComplete,
            "in_progress" => _barFill,
            _ => _barIdle
        };
        Rect bar = GUILayoutUtility.GetRect(1f, 4f, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(bar, _barBg);
        if (fraction > 0f)
        {
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), fill);
        }
    }

    private void DrawShop(List<BattlePassShopOfferDto> shop)
    {
        if (shop == null || shop.Count == 0)
        {
            GUILayout.Label("The exchange is empty.", _body);
            return;
        }

        int tickets = _status?.Tickets ?? 0;
        List<BattlePassShopOfferDto> filtered = shop.Where(offer => OfferMatches(offer, tickets)).ToList();
        if (filtered.Count == 0)
        {
            GUILayout.Label("No offers match that search.", _body);
            GUILayout.Space(8f);
            if (GUILayout.Button("CLEAR FILTERS", _button, GUILayout.Width(140f), GUILayout.Height(28f)))
            {
                ClearShopFilters();
                SoundUtil.Play("ButtonClick", "TabButton");
            }

            return;
        }

        if (filtered.Count != shop.Count)
        {
            GUILayout.Label($"{filtered.Count} of {shop.Count} offers", _small);
            GUILayout.Space(6f);
        }

        foreach (var group in filtered
                     .GroupBy(offer => string.IsNullOrEmpty(offer.Category) ? "other" : offer.Category)
                     .OrderBy(group => CategoryOrder(group.Key)))
        {
            GUILayout.Label(CategoryLabel(group.Key), _header);
            GUILayout.Space(4f);
            List<BattlePassShopOfferDto> offers = group.OrderBy(item => item.Price).ThenBy(item => item.Name).ToList();
            float column = Mathf.Max(260f, Mathf.Floor((_layoutWidth - 24f) * 0.5f));
            for (int i = 0; i < offers.Count; i += 2)
            {
                GUILayout.BeginHorizontal();
                DrawShopOffer(offers[i], tickets, column);
                GUILayout.Space(8f);
                if (i + 1 < offers.Count)
                {
                    DrawShopOffer(offers[i + 1], tickets, column);
                }
                else
                {
                    GUILayout.Space(column);
                }

                GUILayout.EndHorizontal();
                GUILayout.Space(6f);
            }

            GUILayout.Space(8f);
        }

        GUILayout.Space(8f);
        GUILayout.Label("Purchases are sent to Messages (SYSTEM). Collect them into stash from there.", _small);
    }

    private void DrawShopOffer(BattlePassShopOfferDto offer, int tickets, float width)
    {
        bool soldOut = offer.StockRemaining != null && offer.StockRemaining <= 0;
        bool canAfford = tickets >= offer.Price;
        bool canBuy = !_buying && !soldOut && canAfford;
        string stock = offer.StockRemaining == null ? "unlimited" : $"{offer.StockRemaining} left";
        Color iconTint = soldOut ? new Color(1f, 1f, 1f, 0.35f) : Color.white;

        GUILayout.BeginVertical(_cardStyle, GUILayout.Width(width));
        GUILayout.BeginHorizontal();
        DrawItemIcon(offer.Tpl, 44f, iconTint);
        GUILayout.Space(8f);
        GUILayout.BeginVertical();
        GUILayout.Label(offer.Name, _body);
        string meta = offer.Preset
            ? "Default build  ·  assembled"
            : $"{offer.Count}x  ·  {stock}";
        GUILayout.Label(meta, _small);
        if (soldOut)
        {
            GUILayout.Label("Sold out this season.", _need);
        }
        else if (!canAfford)
        {
            GUILayout.Label($"{offer.Price - tickets} short", _need);
        }

        GUILayout.EndVertical();
        GUILayout.EndHorizontal();
        GUILayout.Space(6f);
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{offer.Price} tickets", _header);
        GUILayout.FlexibleSpace();
        GUI.enabled = canBuy;
        string buyLabel = soldOut ? "SOLD OUT" : "BUY";
        if (GUILayout.Button(buyLabel, soldOut ? _button : _btnBuy, GUILayout.Width(78f), GUILayout.Height(28f)))
        {
            Plugin.Instance.BuyOffer(offer.Id);
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void DrawShopFilters(List<BattlePassShopOfferDto> shop)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Search", _small, GUILayout.Width(52f));
        string nextQuery = GUILayout.TextField(_shopQuery ?? "", _field, GUILayout.Height(26f), GUILayout.ExpandWidth(true));
        if (nextQuery != _shopQuery)
        {
            _shopQuery = nextQuery;
            _viewScroll[(int)View.Exchange] = Vector2.zero;
        }

        bool filtering = ShopFiltersActive();
        GUI.enabled = filtering;
        if (GUILayout.Button("CLEAR", _button, GUILayout.Width(70f), GUILayout.Height(26f)))
        {
            ClearShopFilters();
            SoundUtil.Play("ButtonClick", "TabButton");
        }

        GUI.enabled = true;
        DrawShopChip("CAN AFFORD", _shopAffordableOnly, () =>
        {
            _shopAffordableOnly = !_shopAffordableOnly;
            _viewScroll[(int)View.Exchange] = Vector2.zero;
        });
        GUILayout.EndHorizontal();
        GUILayout.Space(6f);

        DrawWrappedCategoryChips(shop);
        GUILayout.Space(10f);
    }

    private void DrawWrappedCategoryChips(List<BattlePassShopOfferDto> shop)
    {
        var chips = new List<(string Id, string Label)> { ("", "ALL") };
        foreach (string category in shop
                     .Select(offer => string.IsNullOrEmpty(offer.Category) ? "other" : offer.Category)
                     .Distinct()
                     .OrderBy(CategoryOrder))
        {
            chips.Add((category, CategoryLabel(category)));
        }

        GUILayout.BeginHorizontal();
        float used = 0f;
        foreach ((string id, string label) in chips)
        {
            float width = Mathf.Clamp(_chipOff.CalcSize(new GUIContent(label)).x + 22f, 56f, 160f);
            if (used > 0f && used + width > _layoutWidth)
            {
                GUILayout.EndHorizontal();
                GUILayout.Space(4f);
                GUILayout.BeginHorizontal();
                used = 0f;
            }

            bool selected = string.IsNullOrEmpty(id)
                ? string.IsNullOrEmpty(_shopCategory)
                : string.Equals(_shopCategory, id, StringComparison.OrdinalIgnoreCase);
            DrawShopChip(label, selected, () =>
            {
                _shopCategory = string.IsNullOrEmpty(id)
                    ? ""
                    : string.Equals(_shopCategory, id, StringComparison.OrdinalIgnoreCase) ? "" : id;
                _viewScroll[(int)View.Exchange] = Vector2.zero;
            }, width);
            used += width + 4f;
        }

        GUILayout.EndHorizontal();
    }

    private void DrawShopChip(string label, bool selected, Action onClick, float width = 0f)
    {
        GUIStyle style = selected ? _chipOn : _chipOff;
        bool clicked = width > 0f
            ? GUILayout.Button(label, style, GUILayout.Height(24f), GUILayout.Width(width))
            : GUILayout.Button(label, style, GUILayout.Height(24f));
        if (clicked)
        {
            onClick();
            SoundUtil.Play("ButtonClick", "TabButton");
        }

        GUILayout.Space(4f);
    }

    private bool OfferMatches(BattlePassShopOfferDto offer, int tickets)
    {
        if (offer == null)
        {
            return false;
        }

        string category = string.IsNullOrEmpty(offer.Category) ? "other" : offer.Category;
        if (!string.IsNullOrEmpty(_shopCategory) && !string.Equals(category, _shopCategory, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        bool soldOut = offer.StockRemaining != null && offer.StockRemaining <= 0;
        if (_shopAffordableOnly && (soldOut || tickets < offer.Price))
        {
            return false;
        }

        string query = (_shopQuery ?? "").Trim();
        if (query.Length == 0)
        {
            return true;
        }

        return (offer.Name ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
               || CategoryLabel(category).IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
               || (offer.Id ?? "").IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0
               || (offer.Preset && "preset".IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private bool ShopFiltersActive()
    {
        return !string.IsNullOrWhiteSpace(_shopQuery)
               || !string.IsNullOrEmpty(_shopCategory)
               || _shopAffordableOnly;
    }

    private void ClearShopFilters()
    {
        _shopQuery = "";
        _shopCategory = "";
        _shopAffordableOnly = false;
        _viewScroll[(int)View.Exchange] = Vector2.zero;
        GUI.FocusControl(null);
    }

    private void DrawItemIcon(string tpl, float size, Color tint)
    {
        Rect area = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));
        GUI.DrawTexture(area, _iconBg);
        BattlePassItemIcons.Request(tpl);
        Sprite sprite = BattlePassItemIcons.Get(tpl);
        if (sprite != null)
        {
            BattlePassItemIcons.Draw(area, sprite, tint);
            return;
        }

        if (!BattlePassItemIcons.IsLoading(tpl) || Event.current.type != EventType.Repaint)
        {
            return;
        }

        float pulse = 0.22f + 0.28f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
        Color previous = GUI.color;
        GUI.color = new Color(0.85f, 0.65f, 0.15f, pulse);
        GUI.DrawTexture(new Rect(area.x + 4f, area.y + 4f, area.width - 8f, area.height - 8f), _accent);
        GUI.color = previous;
    }

    private void PrefetchShopIcons()
    {
        if (_status?.Shop == null)
        {
            return;
        }

        foreach (BattlePassShopOfferDto offer in _status.Shop)
        {
            BattlePassItemIcons.Request(offer.Tpl);
        }
    }

    private void PrefetchTrackIcons()
    {
        if (_status?.Track == null)
        {
            return;
        }

        foreach (TrackTierStatusDto tier in _status.Track)
        {
            if (!string.IsNullOrEmpty(tier.Free?.Tpl))
            {
                BattlePassItemIcons.Request(tier.Free.Tpl);
            }

            if (!string.IsNullOrEmpty(tier.Premium?.Tpl))
            {
                BattlePassItemIcons.Request(tier.Premium.Tpl);
            }
        }
    }

    private static string CategoryLabel(string category)
    {
        return category switch
        {
            "ammo" => "AMMO",
            "medical" => "MEDICAL",
            "provisions" => "PROVISIONS",
            "hideout" => "HIDEOUT",
            "barter" => "BARTER",
            "cases" => "CASES",
            "weapons" => "WEAPONS",
            "keys" => "KEYS",
            "rare" => "RARE",
            _ => category.ToUpperInvariant()
        };
    }

    private static int CategoryOrder(string category)
    {
        return category switch
        {
            "ammo" => 0,
            "medical" => 1,
            "provisions" => 2,
            "hideout" => 3,
            "barter" => 4,
            "cases" => 5,
            "weapons" => 6,
            "keys" => 7,
            "rare" => 8,
            _ => 9
        };
    }

    private void EnsureStyles()
    {
        TarkovUi.Ensure();
        if (_title != null)
        {
            return;
        }

        _panelBg = TarkovUi.OverlayTex;
        _cardBg = TarkovUi.PanelTex;
        _accent = TarkovUi.AmberTex;
        _rowBg = TarkovUi.ItemTex;
        _iconBg = TarkovUi.IconTex;
        _barBg = TarkovUi.BarBgTex;
        _barFill = TarkovUi.AmberTex;
        _barComplete = TarkovUi.GreenTex;
        _barIdle = TarkovUi.IdleBarTex;

        _cardStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = _rowBg },
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(10, 10, 8, 8),
            margin = new RectOffset(0, 0, 0, 0)
        };
        _accentComplete = new GUIStyle(GUIStyle.none) { normal = { background = _barComplete } };
        _accentProgress = new GUIStyle(GUIStyle.none) { normal = { background = _barFill } };
        _accentIdle = new GUIStyle(GUIStyle.none) { normal = { background = _barIdle } };

        _title = TarkovUi.Label(22, TarkovUi.Amber, FontStyle.Bold);
        _header = TarkovUi.Label(13, Color.white, FontStyle.Bold);
        _body = TarkovUi.Label(13, TarkovUi.Text, FontStyle.Normal, TextAnchor.UpperLeft, true);
        _small = TarkovUi.Label(11, TarkovUi.Grey);
        _button = TarkovUi.Button(11, TarkovUi.Grey, TarkovUi.Btn, TarkovUi.Amber, TarkovUi.BtnHover);
        _tabOn = TarkovUi.Button(11, TarkovUi.Amber, TarkovUi.TabOn, TarkovUi.Amber, TarkovUi.BtnHover);
        _tabOff = TarkovUi.Button(11, TarkovUi.Grey, TarkovUi.Btn, TarkovUi.Amber, TarkovUi.BtnHover);
        _chipOn = TarkovUi.Button(10, TarkovUi.Amber, TarkovUi.TabOn, Color.white, TarkovUi.BtnHover);
        _chipOff = TarkovUi.Button(10, TarkovUi.Grey, TarkovUi.Btn, TarkovUi.Amber, TarkovUi.BtnHover);
        _btnBuy = TarkovUi.Button(11, TarkovUi.Amber, TarkovUi.TabOn, Color.white, TarkovUi.BtnHover);
        _btnClose = TarkovUi.Button(11, TarkovUi.Grey, TarkovUi.Btn, Color.white, TarkovUi.CloseHover);
        _stateComplete = TarkovUi.Label(11, TarkovUi.Green, FontStyle.Bold);
        _stateProgress = TarkovUi.Label(11, TarkovUi.Amber, FontStyle.Bold);
        _stateIdle = TarkovUi.Label(11, TarkovUi.Dim, FontStyle.Bold);
        _need = TarkovUi.Label(11, TarkovUi.Red, FontStyle.Bold);
        _notice = TarkovUi.Label(13, TarkovUi.Amber, FontStyle.Bold, TextAnchor.UpperLeft, true);
        _ticket = TarkovUi.Label(22, TarkovUi.Amber, FontStyle.Bold, TextAnchor.MiddleRight);
        _ticketUnit = TarkovUi.Label(11, TarkovUi.Dim, FontStyle.Bold, TextAnchor.MiddleLeft);
        _field = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            font = TarkovUi.Font,
            border = new RectOffset(0, 0, 0, 0),
            padding = new RectOffset(8, 8, 4, 4)
        };
        _field.normal.textColor = TarkovUi.Text;
        _field.normal.background = TarkovUi.Tex(new Color(0.09f, 0.09f, 0.095f, 1f));
        _field.focused.textColor = Color.white;
        _field.focused.background = TarkovUi.Tex(new Color(0.12f, 0.11f, 0.08f, 1f));
    }
}
