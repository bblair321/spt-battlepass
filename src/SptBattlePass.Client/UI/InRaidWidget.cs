using System;
using System.Collections.Generic;
using SptBattlePass.Client.Models;
using SptBattlePass.Client.Services;
using UnityEngine;

namespace SptBattlePass.Client.UI;

public sealed class InRaidWidget
{
    private enum Phase
    {
        Hidden,
        SlideIn,
        Active,
        SlideOut
    }

    private const float SlideSeconds = 0.35f;
    private const float AutoHideSeconds = 6f;

    private BattlePassStatusDto _status;
    private Phase _phase;
    private float _slideStart;
    private float _phaseEnd;
    private bool _autoHide;
    private float _autoHideEnd;
    private GUIStyle _title;
    private GUIStyle _row;
    private GUIStyle _count;
    private GUIStyle _section;
    private Texture2D _bg;
    private Texture2D _accent;
    private Texture2D _barBg;
    private Texture2D _barFill;
    private Texture2D _barDone;
    private Texture2D _sep;

    public void SetStatus(BattlePassStatusDto status)
    {
        _status = status;
    }

    public void ShowAuto()
    {
        if (!BattlePassSettings.Widget || !HasRows())
        {
            return;
        }

        float now = Time.unscaledTime;
        if (_phase == Phase.Hidden || _phase == Phase.SlideOut)
        {
            _autoHide = true;
            _slideStart = now;
            _phaseEnd = now + SlideSeconds;
            _phase = Phase.SlideIn;
        }
        else if (_phase == Phase.Active && _autoHide)
        {
            _autoHideEnd = now + AutoHideSeconds;
        }
    }

    public void NotifyProgress()
    {
        foreach (BattlePassChallengeDto challenge in VisibleChallenges())
        {
            if (RaidProgress.DeltaFor(challenge) > 0)
            {
                ShowAuto();
                return;
            }
        }
    }

    public void Toggle()
    {
        float now = Time.unscaledTime;
        if (_phase == Phase.Hidden || _phase == Phase.SlideOut)
        {
            _autoHide = false;
            _slideStart = now;
            _phaseEnd = now + SlideSeconds;
            _phase = Phase.SlideIn;
        }
        else
        {
            _slideStart = now;
            _phaseEnd = now + SlideSeconds;
            _phase = Phase.SlideOut;
        }
    }

    public void Hide()
    {
        _phase = Phase.Hidden;
    }

    public void Draw()
    {
        if (_phase == Phase.Hidden)
        {
            return;
        }

        EnsureStyles();
        float now = Time.unscaledTime;
        float scale = Mathf.Max(0.85f, Screen.height / 1080f);
        float width = 300f * scale;
        float shownX = Screen.width - width - 8f * scale;
        float hiddenX = Screen.width + 12f * scale;
        switch (_phase)
        {
            case Phase.SlideIn:
                if (now >= _phaseEnd)
                {
                    _phase = Phase.Active;
                    if (_autoHide)
                    {
                        _autoHideEnd = now + AutoHideSeconds;
                    }
                }

                break;
            case Phase.Active:
                if (_autoHide && now >= _autoHideEnd)
                {
                    _slideStart = now;
                    _phaseEnd = now + SlideSeconds;
                    _phase = Phase.SlideOut;
                }

                break;
            case Phase.SlideOut:
                if (now >= _phaseEnd)
                {
                    _phase = Phase.Hidden;
                    return;
                }

                break;
        }

        float t = Mathf.Clamp01((now - _slideStart) / SlideSeconds);
        float x = _phase switch
        {
            Phase.SlideIn => Mathf.Lerp(hiddenX, shownX, 1f - Mathf.Pow(1f - t, 3f)),
            Phase.SlideOut => Mathf.Lerp(shownX, hiddenX, t * t),
            _ => shownX
        };

        List<(string Section, BattlePassChallengeDto Challenge)> rows = BuildRows();
        int sections = 0;
        string seen = null;
        foreach ((string section, BattlePassChallengeDto _) in rows)
        {
            if (section != seen)
            {
                sections++;
                seen = section;
            }
        }

        float height = (rows.Count == 0
            ? 54f
            : 34f + sections * 14f + rows.Count * 26f + 10f) * scale;
        float y = 72f * scale;
        var box = new Rect(x, y, width, height);
        GUI.DrawTexture(box, _bg);
        GUI.DrawTexture(new Rect(x, y, width, 3f * scale), _accent);
        TarkovUi.Outline(box, new Color(TarkovUi.Amber.r, TarkovUi.Amber.g, TarkovUi.Amber.b, 0.8f));

        GUI.contentColor = new Color(0.92f, 0.86f, 0.62f);
        GUI.Label(new Rect(x + 12f * scale, y + 6f * scale, width - 24f * scale, 20f * scale), "BATTLE PASS", _title);
        GUI.contentColor = Color.white;
        GUI.DrawTexture(new Rect(x + 12f * scale, y + 28f * scale, width - 24f * scale, 1f), _sep);

        if (rows.Count == 0)
        {
            GUI.contentColor = new Color(0.62f, 0.62f, 0.62f);
            GUI.Label(new Rect(x + 12f * scale, y + 32f * scale, width - 24f * scale, 18f * scale), "No challenges for this raid.", _row);
            GUI.contentColor = Color.white;
            return;
        }

        float rowY = y + 32f * scale;
        string lastSection = null;
        foreach ((string section, BattlePassChallengeDto challenge) in rows)
        {
            if (section != lastSection)
            {
                lastSection = section;
                GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
                GUI.Label(new Rect(x + 12f * scale, rowY, width - 24f * scale, 14f * scale), section, _section);
                GUI.contentColor = Color.white;
                rowY += 14f * scale;
            }

            DrawRow(x, rowY, width, scale, challenge);
            rowY += 26f * scale;
        }
    }

    private void DrawRow(float x, float y, float width, float scale, BattlePassChallengeDto challenge)
    {
        int live = Mathf.Min(challenge.Target, challenge.Progress + RaidProgress.DeltaFor(challenge));
        bool complete = challenge.Target > 0 && live >= challenge.Target;
        float fraction = challenge.Target <= 0 ? 0f : Mathf.Clamp01(live / (float)challenge.Target);
        string name = challenge.Name ?? "";
        if (name.Length > 28)
        {
            name = name.Substring(0, 27) + "…";
        }

        GUI.contentColor = complete
            ? new Color(0.45f, 0.85f, 0.5f)
            : BattlePassSettings.IsPinned(challenge)
                ? new Color(0.95f, 0.82f, 0.4f)
                : new Color(0.88f, 0.88f, 0.88f);
        string label = BattlePassSettings.IsPinned(challenge) ? "★ " + name : name;
        GUI.Label(new Rect(x + 12f * scale, y, width - 78f * scale, 16f * scale), label, _row);
        GUI.contentColor = complete ? new Color(0.45f, 0.85f, 0.5f) : new Color(0.95f, 0.75f, 0.28f);
        GUI.Label(new Rect(x + width - 62f * scale, y, 50f * scale, 16f * scale), $"{live}/{challenge.Target}", _count);
        GUI.contentColor = Color.white;

        var bar = new Rect(x + 12f * scale, y + 17f * scale, width - 24f * scale, 3f * scale);
        GUI.DrawTexture(bar, _barBg);
        if (fraction > 0f)
        {
            GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fraction, bar.height), complete ? _barDone : _barFill);
        }
    }

    private bool HasRows()
    {
        return BuildRows().Count > 0;
    }

    private List<(string Section, BattlePassChallengeDto Challenge)> BuildRows()
    {
        var rows = new List<(string, BattlePassChallengeDto)>();
        BattlePassChallengeDto pinned = FindPinned();
        if (pinned != null)
        {
            rows.Add(("PINNED", pinned));
        }

        AddGroup(rows, "DAILY", _status?.Challenges?.Daily, pinned);
        AddGroup(rows, "WEEKLY", _status?.Challenges?.Weekly, pinned);
        AddGroup(rows, "MONTHLY", _status?.Challenges?.Monthly, pinned);
        return rows;
    }

    private BattlePassChallengeDto FindPinned()
    {
        return FindPinnedIn(_status?.Challenges?.Daily)
               ?? FindPinnedIn(_status?.Challenges?.Weekly)
               ?? FindPinnedIn(_status?.Challenges?.Monthly);
    }

    private static BattlePassChallengeDto FindPinnedIn(List<BattlePassChallengeDto> challenges)
    {
        if (challenges == null)
        {
            return null;
        }

        foreach (BattlePassChallengeDto challenge in challenges)
        {
            if (BattlePassSettings.IsPinned(challenge))
            {
                return challenge;
            }
        }

        return null;
    }

    private static void AddGroup(
        List<(string Section, BattlePassChallengeDto Challenge)> rows,
        string section,
        List<BattlePassChallengeDto> challenges,
        BattlePassChallengeDto pinned)
    {
        if (challenges == null)
        {
            return;
        }

        foreach (BattlePassChallengeDto challenge in challenges)
        {
            if (pinned != null && BattlePassSettings.ChallengeId(challenge) == BattlePassSettings.ChallengeId(pinned))
            {
                continue;
            }

            if (RaidProgress.CanProgress(challenge) || LiveCompleteThisRaid(challenge))
            {
                rows.Add((section, challenge));
            }
        }
    }

    private static bool LiveCompleteThisRaid(BattlePassChallengeDto challenge)
    {
        if (challenge == null || challenge.Completed)
        {
            return false;
        }

        int live = challenge.Progress + RaidProgress.DeltaFor(challenge);
        return challenge.Target > 0 && live >= challenge.Target;
    }

    private IEnumerable<BattlePassChallengeDto> VisibleChallenges()
    {
        foreach ((string _, BattlePassChallengeDto challenge) in BuildRows())
        {
            yield return challenge;
        }
    }

    private void EnsureStyles()
    {
        if (_title != null)
        {
            return;
        }

        TarkovUi.Ensure();
        _bg = TarkovUi.PanelTex;
        _accent = TarkovUi.AmberTex;
        _barBg = TarkovUi.BarBgTex;
        _barFill = TarkovUi.AmberTex;
        _barDone = TarkovUi.GreenTex;
        _sep = TarkovUi.SepTex;
        _title = TarkovUi.Label(12, TarkovUi.Amber, FontStyle.Bold);
        _row = TarkovUi.Label(11, TarkovUi.Text);
        _count = TarkovUi.Label(11, TarkovUi.Amber, FontStyle.Bold, TextAnchor.MiddleRight);
        _section = TarkovUi.Label(9, TarkovUi.Grey, FontStyle.Bold);
    }
}
