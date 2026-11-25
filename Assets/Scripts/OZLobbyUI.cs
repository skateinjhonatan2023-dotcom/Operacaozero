using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

internal sealed class OZLobbyUI
{
    internal static readonly Color Gold = new(1f, .78f, .21f);
    internal static readonly Color White = new(.96f, .96f, .92f);
    internal static readonly Color Muted = new(.78f, .81f, .81f);
    internal static readonly Color Ink = new(.10f, .075f, .03f);
    internal static readonly Color Invisible = new(0, 0, 0, 0);
    internal static readonly Vector2 LeftTop = new(0, 1);
    internal static readonly Vector2 RightTop = new(1, 1);
    internal static readonly Vector2 LeftBottom = Vector2.zero;
    internal static readonly Vector2 RightBottom = Vector2.right;

    readonly Font normal, bold, italic;
    readonly Sprite dark, gold;

    internal OZLobbyUI(Font normal, Font bold, Font italic, Sprite dark, Sprite gold)
    {
        this.normal = normal;
        this.bold = bold;
        this.italic = italic;
        this.dark = dark;
        this.gold = gold;
    }

    internal static RectTransform Layer(Transform parent, string name)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        Fill(rect, Vector2.zero, Vector2.zero);
        return rect;
    }

    internal static void Fill(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
    }

    internal static RectTransform Place(Transform parent, string name, float x, float y, float width, float height,
        Vector2? anchor = null)
    {
        var rect = Layer(parent, name);
        rect.anchorMin = rect.anchorMax = anchor ?? LeftTop;
        rect.pivot = new Vector2(0, 1);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = new Vector2(x, -y);
        return rect;
    }

    internal static Image Picture(RectTransform rect, Sprite source, Color tint, bool hit = false, bool slice = false)
    {
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = source;
        image.type = slice ? Image.Type.Sliced : Image.Type.Simple;
        image.color = tint;
        image.raycastTarget = hit;
        return image;
    }

    internal RectTransform Panel(Transform parent, string name, float x, float y, float w, float h, Vector2? anchor = null)
    {
        var panel = Place(parent, name, x, y, w, h, anchor);
        Picture(panel, dark, Color.white, slice: true);
        return panel;
    }

    internal Text Label(Transform parent, string value, float x, float y, float w, float h, int size,
        Color? color = null, TextAnchor anchor = TextAnchor.MiddleLeft, bool strong = false, bool slanted = false)
    {
        var label = Place(parent, "Texto", x, y, w, h).gameObject.AddComponent<Text>();
        label.font = slanted ? italic : strong ? bold : normal;
        label.fontSize = size;
        label.color = color ?? White;
        label.alignment = anchor;
        label.supportRichText = false;
        label.raycastTarget = false;
        label.text = value;
        return label;
    }

    internal Button Button(Transform parent, string title, float x, float y, float w, float h,
        UnityAction onClick, bool primary = false, int size = 22, Vector2? anchor = null, bool bare = false)
    {
        var rect = Place(parent, title, x, y, w, h, anchor);
        var image = Picture(rect, bare ? null : primary ? gold : dark, bare ? Invisible : Color.white,
            hit: true, slice: !bare);
        var button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.colors = new ColorBlock
        {
            normalColor = Color.white, highlightedColor = new Color(1, .94f, .74f),
            selectedColor = new Color(1, .95f, .78f), pressedColor = new Color(.75f, .69f, .55f),
            disabledColor = new Color(.52f, .53f, .52f), colorMultiplier = 1f, fadeDuration = .09f
        };
        button.onClick.AddListener(onClick);
        if (title.Length > 0)
            Label(rect, title, 10, 0, w - 20, h, size, primary ? Ink : White,
                TextAnchor.MiddleCenter, strong: true, slanted: primary);
        return button;
    }

    internal InputField Input(Transform parent, string title, string value, float x, float y, float w, float h,
        InputField.ContentType content, int limit)
    {
        var rect = Place(parent, title, x, y, w, h);
        var image = Picture(rect, dark, new Color(.65f, .70f, .72f), hit: true, slice: true);
        var input = rect.gameObject.AddComponent<InputField>();
        input.targetGraphic = image;
        input.contentType = content;
        input.lineType = InputField.LineType.SingleLine;
        input.characterLimit = limit;
        input.textComponent = Label(rect, "", 14, 0, w - 28, h, 20);
        input.placeholder = Label(rect, title, 14, 0, w - 28, h, 20, Muted);
        input.text = value;
        return input;
    }
}
