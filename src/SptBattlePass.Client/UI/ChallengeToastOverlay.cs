using System;
using System.Collections.Generic;
using SptBattlePass.Client.Models;
using SptBattlePass.Client.Services;
using UnityEngine;

namespace SptBattlePass.Client.UI;

public sealed class ChallengeToastOverlay
{
    private const float Width = 320f;
    private const float Height = 68f;
    private const float SlideIn = 0.28f;
    private const float Hold = 3.4f;
    private const float SlideOut = 0.22f;
    private const float Gap = 8f;

    private readonly HashSet<string> _toasted = new HashSet<string>();
    private readonly List<Toast> _queue = new List<Toast>();
    private GUIStyle _title;
    private GUIStyle _body;
    private GUIStyle _reward;
    private Texture2D _bg;
    private Texture2D _done;

    private sealed class Toast
    {
        public string Title;
        public string Body;
        public int Tickets;
        public int Xp;
        public float Born;
    }

    public void Reset()
    {
        _toasted.Clear();
        _queue.Clear();
    }

    public void Hide()
    {
        _queue.Clear();
    }

    public void CheckCompletions(BattlePassStatusDto status)
    {
        if (status?.Challenges == null || !RaidProgress.Active || !BattlePassSettings.Toasts)
        {
            return;
        }

        int added = 0;
        added += CheckGroup(status.Challenges.Daily, status.XpDaily);
        added += CheckGroup(status.Challenges.Weekly, status.XpWeekly);
        added += CheckGroup(status.Challenges.Monthly, status.XpMonthly);
        if (added > 0)
        {
            SoundUtil.Play("AchievementCompleted", "QuestCompleted", "QuestFinished", "ButtonClick");
        }
    }

    private int CheckGroup(List<BattlePassChallengeDto> challenges, int xp)
    {
        if (challenges == null)
        {
            return 0;
        }

        int added = 0;
        foreach (BattlePassChallengeDto challenge in challenges)
        {
            if (challenge == null || challenge.Completed)
            {
                continue;
            }

            string id = string.IsNullOrEmpty(challenge.InstanceId)
                ? challenge.TemplateId + ":" + challenge.Name
                : challenge.InstanceId;
            int live = challenge.Progress + RaidProgress.DeltaFor(challenge);
            if (challenge.Target <= 0 || live < challenge.Target || !_toasted.Add(id))
            {
                continue;
            }

            _queue.Add(new Toast
            {
                Title = "CHALLENGE COMPLETE",
                Body = string.IsNullOrEmpty(challenge.Name) ? "Challenge" : challenge.Name,
                Tickets = Math.Max(0, challenge.TicketReward),
                Xp = Math.Max(0, xp),
                Born = Time.unscaledTime
            });
            added++;
        }

        return added;
    }

    public void Draw()
    {
        if (_queue.Count == 0)
        {
            return;
        }

        EnsureStyles();
        float now = Time.unscaledTime;
        float scale = Mathf.Max(0.85f, Screen.height / 1080f);
        float width = Width * scale;
        float height = Height * scale;
        float gap = Gap * scale;
        float shownX = Screen.width - width - 12f * scale;
        float hiddenX = Screen.width + 16f * scale;
        float y = Screen.height - height - 24f * scale;

        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            Toast toast = _queue[i];
            float age = now - toast.Born;
            float life = SlideIn + Hold + SlideOut;
            if (age >= life)
            {
                _queue.RemoveAt(i);
                continue;
            }

            float x;
            if (age < SlideIn)
            {
                float t = age / SlideIn;
                x = Mathf.Lerp(hiddenX, shownX, 1f - Mathf.Pow(1f - t, 3f));
            }
            else if (age < SlideIn + Hold)
            {
                x = shownX;
            }
            else
            {
                float t = (age - SlideIn - Hold) / SlideOut;
                x = Mathf.Lerp(shownX, hiddenX, t * t);
            }

            float drawY = y - (height + gap) * (_queue.Count - 1 - i);
            var box = new Rect(x, drawY, width, height);
            GUI.DrawTexture(box, _bg);
            GUI.DrawTexture(new Rect(x, drawY, width, 3f * scale), _done);
            TarkovUi.Outline(box, new Color(TarkovUi.Green.r, TarkovUi.Green.g, TarkovUi.Green.b, 0.85f));
            GUI.contentColor = new Color(0.45f, 0.85f, 0.5f);
            GUI.Label(new Rect(x + 14f * scale, drawY + 8f * scale, width - 28f * scale, 18f * scale), toast.Title, _title);
            GUI.contentColor = new Color(0.9f, 0.9f, 0.9f);
            GUI.Label(new Rect(x + 14f * scale, drawY + 28f * scale, width - 110f * scale, 18f * scale), toast.Body, _body);
            if (toast.Tickets > 0 || toast.Xp > 0)
            {
                GUI.contentColor = new Color(0.95f, 0.75f, 0.28f);
                string reward = toast.Tickets > 0 ? $"+{toast.Tickets} tickets" : "";
                if (toast.Xp > 0)
                {
                    reward = string.IsNullOrEmpty(reward) ? $"+{toast.Xp} XP" : reward + $"\n+{toast.Xp} XP";
                }

                GUI.Label(new Rect(x + width - 108f * scale, drawY + 22f * scale, 94f * scale, 36f * scale), reward, _reward);
            }

            GUI.contentColor = Color.white;
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
        _done = TarkovUi.GreenTex;
        _title = TarkovUi.Label(12, TarkovUi.Green, FontStyle.Bold);
        _body = TarkovUi.Label(13, TarkovUi.Text);
        _reward = TarkovUi.Label(12, TarkovUi.Amber, FontStyle.Bold, TextAnchor.MiddleRight, true);
    }
}
