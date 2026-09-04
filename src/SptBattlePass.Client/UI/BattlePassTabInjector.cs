using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EFT.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SptBattlePass.Client.UI;

internal static class BattlePassTabInjector
{
    private static GameObject _button;
    private static TextMeshProUGUI _label;
    private static Image _icon;
    private static Color _idleColor = new Color(0.73f, 0.73f, 0.73f, 1f);

    public static void TryInject()
    {
        if (_button != null)
        {
            _button.SetActive(true);
            return;
        }

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

        var labeled = new List<RectTransform>();
        foreach (Transform child in tabs)
        {
            if (child is RectTransform rect && child.GetComponentInChildren<TextMeshProUGUI>(true) != null)
            {
                labeled.Add(rect);
            }
        }

        if (labeled.Count == 0)
        {
            Plugin.Log.LogWarning("[BattlePass] No inventory tabs found to clone");
            return;
        }

        labeled.Sort((a, b) => a.anchoredPosition.x.CompareTo(b.anchoredPosition.x));
        RectTransform template = labeled[0];
        RectTransform last = labeled[labeled.Count - 1];
        RectTransform previous = labeled.Count > 1 ? labeled[labeled.Count - 2] : null;
        _button = BuildFromClone(template, last, previous, tabs);
        Plugin.Log.LogInfo("[BattlePass] Character tab injected");
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

    private static GameObject BuildFromClone(RectTransform template, RectTransform last, RectTransform previous, Transform tabs)
    {
        GameObject clone = UnityEngine.Object.Instantiate(template.gameObject, tabs);
        clone.name = "BattlePassTab";
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

        RectTransform cloneRect = clone.GetComponent<RectTransform>();
        float spacing = previous != null
            ? last.anchoredPosition.x - previous.anchoredPosition.x
            : last.rect.width;
        cloneRect.anchoredPosition = new Vector2(last.anchoredPosition.x + spacing, last.anchoredPosition.y);
        Vector2 size = cloneRect.sizeDelta;
        size.x = Mathf.Max(size.x, last.rect.width, 178f);
        cloneRect.sizeDelta = size;

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
