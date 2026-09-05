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
    private const float BackPad = 10f;
    private const float PrestigeClearance = 18f;

    private static readonly FieldInfo BackButtonField = typeof(InventoryScreen).GetField(
        "_backButton",
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    private static GameObject _button;
    private static TextMeshProUGUI _label;
    private static Image _icon;
    private static Color _idleColor = new Color(0.73f, 0.73f, 0.73f, 1f);
    private static bool _placing;
    private static bool _placed;

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

            _button = BuildFromClone(PickTemplate(labeled), tabs);
            _placed = false;
            Plugin.Log.LogInfo("[BattlePass] Character tab injected");
        }

        _button.SetActive(true);
        if (!_placing && Plugin.Instance != null)
        {
            _placing = true;
            Plugin.Instance.StartCoroutine(PlaceWhenReady(screen, tabs));
        }
    }

    private static IEnumerator PlaceWhenReady(InventoryScreen screen, Transform tabs)
    {
        for (int i = 0; i < 20; i++)
        {
            if (_button == null || tabs == null)
            {
                break;
            }

            if (TryPlace(screen, tabs))
            {
                _placed = true;
                break;
            }

            yield return null;
        }

        _placing = false;
    }

    private static bool TryPlace(InventoryScreen screen, Transform tabs)
    {
        RectTransform self = _button.GetComponent<RectTransform>();
        List<RectTransform> labeled = CollectTabs(tabs);
        RectTransform drops = labeled.FirstOrDefault(tab => tab != self && IsDropsTab(tab));
        List<RectTransform> vanilla = labeled.Where(tab => tab != self && tab != drops).ToList();
        if (vanilla.Count == 0)
        {
            return false;
        }

        RectTransform last = vanilla[vanilla.Count - 1];
        RectTransform previous = vanilla.Count > 1 ? vanilla[vanilla.Count - 2] : null;
        float pitch = previous != null
            ? last.anchoredPosition.x - previous.anchoredPosition.x
            : last.rect.width;
        if (Mathf.Abs(pitch) < 20f)
        {
            pitch = Mathf.Max(last.rect.width, 120f);
        }

        MatchVanillaSize(self, last);
        Canvas.ForceUpdateCanvases();
        if (Mathf.Abs(WorldRight(last) - WorldLeft(last)) < 8f)
        {
            return false;
        }

        if (drops == null)
        {
            if (_placed)
            {
                return true;
            }

            self.anchoredPosition = new Vector2(last.anchoredPosition.x + pitch + PrestigeClearance, last.anchoredPosition.y);
            return false;
        }

        float y = last.anchoredPosition.y;
        self.anchoredPosition = new Vector2(last.anchoredPosition.x + pitch + PrestigeClearance, y);
        drops.anchoredPosition = new Vector2(self.anchoredPosition.x + pitch, y);
        return true;
    }

    private static void MatchVanillaSize(RectTransform self, RectTransform vanilla)
    {
        Vector2 size = self.sizeDelta;
        size.x = vanilla.sizeDelta.x;
        self.sizeDelta = size;
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

        labeled.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
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

    private static bool IsDropsTab(RectTransform tab)
    {
        if (tab == null || (_button != null && tab.gameObject == _button))
        {
            return false;
        }

        string name = tab.name ?? "";
        if (name == "WeekendDropsTab"
            || name.IndexOf("Weekend", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        TextMeshProUGUI label = tab.GetComponentInChildren<TextMeshProUGUI>(true);
        string text = label != null ? label.text ?? "" : "";
        return text.IndexOf("DROPS", System.StringComparison.OrdinalIgnoreCase) >= 0;
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
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Ellipsis;
            _label.text = "BATTLE PASS";
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

    private static RectTransform FindBackButton(InventoryScreen screen, RectTransform relativeTo)
    {
        RectTransform candidate = null;
        if (screen != null && BackButtonField?.GetValue(screen) is Component component)
        {
            candidate = component.GetComponent<RectTransform>();
        }

        if (IsRightOf(candidate, relativeTo))
        {
            return candidate;
        }

        Transform row = relativeTo != null ? relativeTo.parent : null;
        if (row == null && screen != null)
        {
            row = screen.transform;
        }

        if (row == null)
        {
            return null;
        }

        foreach (RectTransform rect in row.GetComponentsInChildren<RectTransform>(true))
        {
            string name = rect.name ?? "";
            if (name.IndexOf("BackButton", System.StringComparison.OrdinalIgnoreCase) < 0
                && name.IndexOf("CloseButton", System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (IsRightOf(rect, relativeTo))
            {
                return rect;
            }
        }

        return null;
    }

    private static bool IsRightOf(RectTransform candidate, RectTransform relativeTo)
    {
        return candidate != null && relativeTo != null && WorldLeft(candidate) > WorldRight(relativeTo) - 4f;
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

    private static float WorldLeftInclusive(RectTransform root)
    {
        float left = WorldLeft(root);
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(false);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || !graphic.isActiveAndEnabled)
            {
                continue;
            }

            left = Mathf.Min(left, WorldLeft(graphic.rectTransform));
        }

        return left;
    }

    private static float WorldRightInclusive(RectTransform root)
    {
        float right = WorldRight(root);
        Graphic[] graphics = root.GetComponentsInChildren<Graphic>(false);
        foreach (Graphic graphic in graphics)
        {
            if (graphic == null || !graphic.isActiveAndEnabled)
            {
                continue;
            }

            right = Mathf.Max(right, WorldRight(graphic.rectTransform));
        }

        return right;
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
