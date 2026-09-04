using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SptBattlePass.Client.UI;

internal static class BattlePassTabInjector
{
    private const string TabObjectName = "BattlePassTab";
    private const float BackPad = 12f;
    private const float TabGap = 6f;
    private const float MinTabWidth = 88f;
    private const float DesiredTabWidth = 156f;

    private static readonly FieldInfo BackButtonField = typeof(InventoryScreen).GetField(
        "_backButton",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static GameObject _button;
    private static TextMeshProUGUI _label;
    private static Image _icon;
    private static Color _idleColor = new Color(0.73f, 0.73f, 0.73f, 1f);
    private static bool _following;

    public static void TryInject()
    {
        InventoryScreen screen = UnityEngine.Object.FindObjectOfType<InventoryScreen>(true);
        if (screen == null)
        {
            Plugin.Log.LogWarning("[BattlePass] InventoryScreen not found");
            return;
        }

        Transform tabs = FindTabsContainer(screen);
        if (tabs == null)
        {
            Plugin.Log.LogWarning("[BattlePass] Tabs container not found");
            return;
        }

        if (_button == null)
        {
            List<RectTransform> labeled = CollectTabs(tabs);
            if (labeled.Count == 0)
            {
                Plugin.Log.LogWarning("[BattlePass] No inventory tabs found to clone");
                return;
            }

            RectTransform template = PickTemplate(labeled);
            _button = BuildFromClone(template, tabs);
            Plugin.Log.LogInfo("[BattlePass] Character tab injected");
        }

        _button.SetActive(true);
        Relayout(screen, tabs);
        if (!_following && Plugin.Instance != null)
        {
            _following = true;
            Plugin.Instance.StartCoroutine(FollowLayout(screen, tabs));
        }
    }

    private static IEnumerator FollowLayout(InventoryScreen screen, Transform tabs)
    {
        for (int i = 0; i < 60; i++)
        {
            if (_button == null || tabs == null)
            {
                break;
            }

            Relayout(screen, tabs);
            yield return null;
        }

        _following = false;
    }

    private static List<RectTransform> CollectTabs(Transform tabs)
    {
        var labeled = new List<RectTransform>();
        foreach (Transform child in tabs)
        {
            if (child is RectTransform rect && child.GetComponentInChildren<TextMeshProUGUI>(true) != null)
            {
                labeled.Add(rect);
            }
        }

        labeled.Sort((a, b) => WorldLeft(a).CompareTo(WorldLeft(b)));
        return labeled;
    }

    private static RectTransform PickTemplate(List<RectTransform> labeled)
    {
        foreach (RectTransform tab in labeled)
        {
            if (!IsExtraModTab(tab))
            {
                return tab;
            }
        }

        return labeled[0];
    }

    private static bool IsExtraModTab(RectTransform tab)
    {
        if (tab == null)
        {
            return false;
        }

        if (_button != null && tab.gameObject == _button)
        {
            return true;
        }

        string name = tab.name ?? "";
        return name == TabObjectName
               || name == "WeekendDropsTab"
               || name.IndexOf("Weekend", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static Transform FindTabsContainer(InventoryScreen screen)
    {
        Transform best = null;
        int bestCount = 0;
        foreach (Transform transform in screen.GetComponentsInChildren<Transform>(true))
        {
            if (transform.name != "Tabs")
            {
                continue;
            }

            int labeled = 0;
            foreach (Transform child in transform)
            {
                if (child.GetComponentInChildren<TextMeshProUGUI>(true) != null)
                {
                    labeled++;
                }
            }

            if (labeled > bestCount)
            {
                bestCount = labeled;
                best = transform;
            }
        }

        return best;
    }

    private static GameObject BuildFromClone(RectTransform template, Transform tabs)
    {
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, tabs);
        clone.name = TabObjectName;
        clone.transform.SetAsLastSibling();

        foreach (MonoBehaviour behaviour in clone.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (behaviour == null || behaviour is TextMeshProUGUI)
            {
                continue;
            }

            string ns = behaviour.GetType().Namespace ?? string.Empty;
            if (!ns.StartsWith("UnityEngine") && !ns.StartsWith("TMPro"))
            {
                behaviour.enabled = false;
            }
        }

        foreach (Transform child in clone.GetComponentsInChildren<Transform>(true))
        {
            string name = child.name ?? "";
            if (name.IndexOf("badge", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("notif", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("counter", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                child.gameObject.SetActive(false);
            }
        }

        clone.SetActive(true);

        Transform selected = clone.transform.Find("Selected");
        if (selected != null)
        {
            selected.gameObject.SetActive(false);
        }

        Transform normal = clone.transform.Find("Normal") ?? clone.transform;
        _label = normal.GetComponentsInChildren<TextMeshProUGUI>(true).FirstOrDefault();
        if (_label != null)
        {
            _idleColor = _label.color;
        }

        Image hitbox = clone.GetComponent<Image>() ?? clone.AddComponent<Image>();
        hitbox.color = Color.clear;
        hitbox.raycastTarget = true;

        Button button = clone.GetComponent<Button>() ?? clone.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        button.transition = Selectable.Transition.None;
        button.interactable = true;
        button.targetGraphic = hitbox;
        button.onClick.AddListener(Plugin.TogglePanel);

        EventTrigger trigger = clone.GetComponent<EventTrigger>() ?? clone.AddComponent<EventTrigger>();
        trigger.triggers.Clear();
        AddHover(trigger, EventTriggerType.PointerEnter, TarkovUi.Amber);
        AddHover(trigger, EventTriggerType.PointerExit, new Color(0f, 0f, 0f, 0f));

        Plugin.Instance.StartCoroutine(ForceLabel("BATTLE PASS"));
        ApplyTabIcon(normal);

        return clone;
    }

    private static void Relayout(InventoryScreen screen, Transform tabs)
    {
        if (_button == null || tabs == null)
        {
            return;
        }

        RectTransform self = _button.GetComponent<RectTransform>();
        List<RectTransform> labeled = CollectTabs(tabs);
        RectTransform drops = labeled.FirstOrDefault(tab => tab != self && IsExtraModTab(tab));
        RectTransform vanillaLast = labeled
            .Where(tab => tab != self && tab != drops)
            .OrderBy(WorldLeft)
            .LastOrDefault();
        if (vanillaLast == null)
        {
            return;
        }

        RectTransform back = FindBackButton(screen);
        float leftLimit = WorldRight(vanillaLast) + TabGap;
        float rightLimit = back != null ? WorldLeft(back) - BackPad : leftLimit + DesiredTabWidth * 2f;
        if (rightLimit <= leftLimit + 8f)
        {
            rightLimit = leftLimit + DesiredTabWidth;
        }

        if (drops != null)
        {
            float dropsWidth = Mathf.Max(WorldWidth(drops), MinTabWidth);
            float dropsLeftMax = rightLimit - dropsWidth;
            float dropsLeftMin = leftLimit + MinTabWidth + TabGap;
            float dropsLeft = dropsLeftMin <= dropsLeftMax
                ? Mathf.Clamp(WorldLeft(drops), dropsLeftMin, dropsLeftMax)
                : dropsLeftMax;

            NudgeToWorldLeft(drops, dropsLeft);

            float selfRight = WorldLeft(drops) - TabGap;
            float available = selfRight - leftLimit;
            float selfWidth = Mathf.Min(DesiredTabWidth, Mathf.Max(40f, available));
            SetWorldWidth(self, selfWidth);
            NudgeToWorldLeft(self, selfRight - selfWidth);
            return;
        }

        float width = Mathf.Clamp(rightLimit - leftLimit, MinTabWidth, DesiredTabWidth);
        SetWorldWidth(self, width);
        NudgeToWorldLeft(self, leftLimit);
        if (WorldRight(self) > rightLimit)
        {
            NudgeToWorldLeft(self, rightLimit - WorldWidth(self));
        }
    }

    private static RectTransform FindBackButton(InventoryScreen screen)
    {
        if (screen == null)
        {
            return null;
        }

        if (BackButtonField?.GetValue(screen) is Component component)
        {
            return component.GetComponent<RectTransform>();
        }

        foreach (RectTransform rect in screen.GetComponentsInChildren<RectTransform>(true))
        {
            string name = rect.name ?? "";
            if (name.IndexOf("BackButton", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("CloseButton", System.StringComparison.OrdinalIgnoreCase) >= 0
                || name == "Back")
            {
                return rect;
            }
        }

        return null;
    }

    private static float WorldLeft(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Mathf.Min(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
    }

    private static float WorldRight(RectTransform rect)
    {
        var corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return Mathf.Max(corners[0].x, corners[1].x, corners[2].x, corners[3].x);
    }

    private static float WorldWidth(RectTransform rect)
    {
        return Mathf.Max(1f, WorldRight(rect) - WorldLeft(rect));
    }

    private static void NudgeToWorldLeft(RectTransform rect, float worldLeft)
    {
        float delta = worldLeft - WorldLeft(rect);
        if (Mathf.Abs(delta) < 0.4f)
        {
            return;
        }

        Vector3 position = rect.position;
        position.x += delta;
        rect.position = position;
    }

    private static void SetWorldWidth(RectTransform rect, float worldWidth)
    {
        float current = WorldWidth(rect);
        float scale = Mathf.Abs(rect.lossyScale.x);
        if (scale < 0.01f)
        {
            scale = 1f;
        }

        Vector2 size = rect.sizeDelta;
        size.x += (worldWidth - current) / scale;
        rect.sizeDelta = size;
    }

    private static void ApplyTabIcon(Transform normal)
    {
        float glyph = 24f;
        foreach (Image image in normal.GetComponentsInChildren<Image>(true))
        {
            if (image == null)
            {
                continue;
            }

            bool wrapsLabel = image.GetComponentInChildren<TextMeshProUGUI>(true) != null;
            if (wrapsLabel)
            {
                Color plate = image.color;
                if (plate.r > 0.85f && plate.g > 0.85f && plate.b > 0.85f && plate.a > 0.35f)
                {
                    image.color = Color.clear;
                }

                image.raycastTarget = false;
                continue;
            }

            glyph = Mathf.Max(20f, image.rectTransform.rect.height);
            image.enabled = false;
            image.sprite = null;
        }

        if (_label != null)
        {
            glyph = Mathf.Clamp(_label.fontSize + 4f, 20f, 28f);
        }

        Sprite sprite = TabIcon.Get();
        if (sprite == null)
        {
            return;
        }

        var iconObject = new GameObject("BattlePassIcon");
        iconObject.transform.SetParent(normal, false);
        RectTransform rect = iconObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(8f, 0f);
        rect.sizeDelta = new Vector2(glyph, glyph);
        _icon = iconObject.AddComponent<Image>();
        _icon.sprite = sprite;
        _icon.color = _idleColor;
        _icon.preserveAspect = true;
        _icon.raycastTarget = false;

        if (_label != null)
        {
            RectTransform labelRect = _label.rectTransform;
            labelRect.offsetMin = new Vector2(8f + glyph + 4f, labelRect.offsetMin.y);
            _label.alignment = TextAlignmentOptions.MidlineLeft;
            _label.color = _idleColor;
        }
    }

    private static IEnumerator ForceLabel(string text)
    {
        for (int i = 0; i < 15; i++)
        {
            if (_label != null)
            {
                _label.text = text;
            }

            yield return null;
        }
    }

    private static void AddHover(EventTrigger trigger, EventTriggerType type, Color color)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(_ =>
        {
            Color tint = color.a <= 0.01f ? _idleColor : color;
            if (_label != null)
            {
                _label.color = tint;
            }

            if (_icon != null)
            {
                _icon.color = tint;
            }
        });
        trigger.triggers.Add(entry);
    }
}
