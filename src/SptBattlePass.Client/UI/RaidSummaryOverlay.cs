using System.Collections.Generic;
using SptBattlePass.Client.Models;
using UnityEngine;

namespace SptBattlePass.Client.UI;

public sealed class RaidSummaryOverlay
{
    private RaidEndResultDto _result;
    private bool _visible;
    private Vector2 _scroll;
    private GUIStyle _title;
    private GUIStyle _tickets;
    private GUIStyle _name;
    private GUIStyle _small;
    private GUIStyle _complete;
    private GUIStyle _progress;
    private Texture2D _cardBg;
    private Texture2D _accent;
    private Texture2D _barBg;
    private Texture2D _barFill;
    private Texture2D _barComplete;

    public bool IsVisible => _visible;

    public void Show(RaidEndResultDto result)
    {
        _result = result;
        _visible = result != null;
        _scroll = Vector2.zero;
    }

    public void Hide()
    {
        _visible = false;
        _result = null;
    }

    public void Draw()
    {
        if (!_visible || _result == null)
        {
            return;
        }

        EnsureStyles();
        float scale = Mathf.Max(0.75f, Screen.height / 1080f);
        float width = 360f * scale;
        int rows = (_result.Updates?.Count ?? 0)
            + (_result.MonthlyBonus > 0 || _result.MonthlyBonusXp > 0 ? 1 : 0);
        float height = Mathf.Min(
            Screen.height * 0.62f,
            (92f + Mathf.Max(1, rows) * 58f) * scale);
        var window = new Rect(28f * scale, (Screen.height - height) * 0.42f, width, height);

        TarkovUi.Frame(window);

        GUILayout.BeginArea(new Rect(window.x + 16f, window.y + 12f, window.width - 28f, window.height - 24f));
        GUILayout.BeginHorizontal();
        GUILayout.Label("BATTLE PASS", _title);
        GUILayout.FlexibleSpace();
        if (_result.TicketsEarned > 0)
        {
            GUILayout.Label($"+{_result.TicketsEarned} tickets", _tickets);
        }

        if (_result.XpEarned > 0)
        {
            GUILayout.Label($"+{_result.XpEarned} XP", _tickets);
        }

        GUILayout.EndHorizontal();
        GUILayout.Space(8f);

        List<RaidChallengeUpdateDto> updates = _result.Updates;
        if ((updates == null || updates.Count == 0) && _result.MonthlyBonus <= 0 && _result.MonthlyBonusXp <= 0)
        {
            GUILayout.Label("No challenge progress this raid.", _small);
            GUILayout.EndArea();
            return;
        }

        _scroll = GUILayout.BeginScrollView(_scroll);
        if (updates != null)
        {
            foreach (RaidChallengeUpdateDto update in updates)
            {
                DrawUpdate(update);
            }
        }

        if (_result.MonthlyBonus > 0 || _result.MonthlyBonusXp > 0)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("All monthly challenges complete", _name);
            GUILayout.FlexibleSpace();
            GUILayout.Label("COMPLETE", _complete);
            GUILayout.EndHorizontal();
            var bonusParts = new List<string>();
            if (_result.MonthlyBonus > 0)
            {
                bonusParts.Add($"+{_result.MonthlyBonus} tickets");
            }

            if (_result.MonthlyBonusXp > 0)
            {
                bonusParts.Add($"+{_result.MonthlyBonusXp} XP");
            }

            GUILayout.Label(string.Join("  ", bonusParts.ToArray()), _tickets);
            GUILayout.EndVertical();
            GUILayout.Space(6f);
        }

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    private void DrawUpdate(RaidChallengeUpdateDto update)
    {
        bool complete = update.Completed;
        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.BeginHorizontal();
        GUILayout.Label(string.IsNullOrEmpty(update.Name) ? "Challenge" : update.Name, _name);
        GUILayout.FlexibleSpace();
        GUILayout.Label(complete ? "COMPLETE" : CategoryLabel(update.Category), complete ? _complete : _progress);
        GUILayout.EndHorizontal();

        string progress = $"{update.PreviousProgress} → {update.Progress} / {update.Target}";
        if (update.TicketsEarned > 0)
        {
            progress += $"   +{update.TicketsEarned} ticket{(update.TicketsEarned == 1 ? "" : "s")}";
        }

        if (update.XpEarned > 0)
        {
            progress += $"   +{update.XpEarned} XP";
        }

        GUILayout.Label(progress, _small);
        DrawProgress(update.Progress, update.Target, complete);
        GUILayout.EndVertical();
        GUILayout.Space(6f);
    }

    private static string CategoryLabel(string category)
    {
        return category switch
        {
            "daily" => "DAILY",
            "weekly" => "WEEKLY",
            "monthly" => "MONTHLY",
            _ => (category ?? "").ToUpperInvariant()
        };
    }

    private void DrawProgress(int progress, int target, bool complete)
    {
        float fraction = target <= 0 ? 0f : Mathf.Clamp01(progress / (float)target);
        Rect bar = GUILayoutUtility.GetRect(1f, 7f, GUILayout.ExpandWidth(true));
        GUI.DrawTexture(bar, _barBg);
        if (fraction > 0f)
        {
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), complete ? _barComplete : _barFill);
        }
    }

    private void EnsureStyles()
    {
        if (_title != null)
        {
            return;
        }

        TarkovUi.Ensure();
        _cardBg = TarkovUi.PanelTex;
        _accent = TarkovUi.AmberTex;
        _barBg = TarkovUi.BarBgTex;
        _barFill = TarkovUi.AmberTex;
        _barComplete = TarkovUi.GreenTex;

        _title = TarkovUi.Label(16, TarkovUi.Amber, FontStyle.Bold);
        _tickets = TarkovUi.Label(15, TarkovUi.Amber, FontStyle.Bold);
        _name = TarkovUi.Label(13, TarkovUi.Text, FontStyle.Bold, TextAnchor.UpperLeft, true);
        _small = TarkovUi.Label(12, TarkovUi.Grey);
        _complete = TarkovUi.Label(11, TarkovUi.Green, FontStyle.Bold);
        _progress = TarkovUi.Label(11, TarkovUi.Amber, FontStyle.Bold);
    }
}
