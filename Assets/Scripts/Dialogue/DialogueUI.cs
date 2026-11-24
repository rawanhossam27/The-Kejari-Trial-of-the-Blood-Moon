using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dark-fantasy dialogue panel: name, portrait, typewriter text, a continue mark, and choice buttons.
/// Builds its own canvas the first time it wakes, using the same scale as the gameplay HUD.
/// </summary>
[DisallowMultipleComponent]
public class DialogueUI : MonoBehaviour
{
    [SerializeField, Min(1f)] float charactersPerSecond = 42f;

    Canvas canvas;
    GameObject frame;
    GameObject panel;
    Image portraitImage;
    GameObject portraitFrame;
    TMP_Text nameLabel;
    TMP_Text bodyLabel;
    TMP_Text continueLabel;
    RectTransform choiceRoot;
    readonly System.Collections.Generic.List<Button> choiceButtons = new System.Collections.Generic.List<Button>();

    string fullText = string.Empty;
    float revealed;
    bool revealing;
    bool choicesVisible;

    static readonly Color PanelColor = new Color(0.06f, 0.035f, 0.05f, 0.94f);
    static readonly Color FrameColor = new Color(0.56f, 0.42f, 0.67f, 0.95f);
    static readonly Color NameColor = new Color(0.86f, 0.72f, 0.45f, 1f);
    static readonly Color BodyColor = new Color(0.93f, 0.90f, 0.86f, 1f);
    static readonly Color ContinueColor = new Color(0.78f, 0.38f, 0.42f, 1f);

    public bool IsRevealing => revealing;
    public bool ChoicesVisible => choicesVisible;
    public Canvas RootCanvas => canvas;

    void Awake()
    {
        EnsureBuilt();
        Hide();
    }

    /// <summary>Shows one line and starts the typewriter. Choices stay hidden until the manager asks for them.</summary>
    public void ShowLine(string speakerName, Sprite portrait, string text)
    {
        EnsureBuilt();
        frame.SetActive(true);
        panel.SetActive(true);
        ClearChoices();

        nameLabel.text = string.IsNullOrEmpty(speakerName) ? "..." : speakerName;
        fullText = text ?? string.Empty;
        revealed = 0f;
        revealing = fullText.Length > 0;
        bodyLabel.text = revealing ? string.Empty : fullText;
        continueLabel.gameObject.SetActive(false);

        bool hasPortrait = portrait != null;
        portraitFrame.SetActive(hasPortrait);
        if (hasPortrait)
            portraitImage.sprite = portrait;

        FitToText(hasPortrait);
        bodyLabel.text = revealing ? string.Empty : fullText;
    }

    /// <summary>
    /// Sizes the plate around the spoken line. A short line stays narrow, and a missing portrait
    /// does not leave an empty column.
    /// </summary>
    void FitToText(bool hasPortrait)
    {
        const float bottomMargin = 48f;
        const float framePad = 7f;
        const float maxTextWidth = 520f;
        const float minTextWidth = 260f;
        const float sidePad = 22f;
        const float topPad = 14f;
        const float nameBlock = 32f;
        const float continueBlock = 28f;
        const float portraitSize = 92f;
        const float portraitGap = 14f;

        float left = sidePad + (hasPortrait ? portraitSize + portraitGap : 0f);
        var bodyRect = bodyLabel.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(0f, 1f);
        bodyRect.pivot = new Vector2(0f, 1f);
        bodyRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxTextWidth);

        bodyLabel.text = fullText;
        bodyLabel.ForceMeshUpdate();
        Vector2 preferred = bodyLabel.GetPreferredValues(fullText, maxTextWidth, 0f);
        float textWidth = Mathf.Clamp(preferred.x, minTextWidth, maxTextWidth);
        float textHeight = Mathf.Max(28f, preferred.y);

        float panelWidth = left + textWidth + sidePad;
        float panelHeight = topPad + nameBlock + 4f + textHeight + continueBlock;

        if (hasPortrait)
        {
            var portraitRect = portraitFrame.GetComponent<RectTransform>();
            portraitRect.sizeDelta = new Vector2(portraitSize, portraitSize);
            portraitRect.anchoredPosition = new Vector2(sidePad, 0f);
        }

        var nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(0f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.anchoredPosition = new Vector2(left, -topPad);
        nameRect.sizeDelta = new Vector2(textWidth, nameBlock);

        bodyRect.anchoredPosition = new Vector2(left, -(topPad + nameBlock + 2f));
        bodyRect.sizeDelta = new Vector2(textWidth, textHeight);

        var continueRect = continueLabel.rectTransform;
        continueRect.anchorMin = new Vector2(1f, 0f);
        continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.sizeDelta = new Vector2(Mathf.Min(168f, textWidth), 22f);
        continueRect.anchoredPosition = new Vector2(-sidePad, 8f);

        var plate = panel.GetComponent<RectTransform>();
        StretchBottom(plate, new Vector2(panelWidth, panelHeight), new Vector2(0f, bottomMargin + framePad));
        StretchBottom(frame.GetComponent<RectTransform>(), new Vector2(panelWidth + framePad * 2f, panelHeight + framePad * 2f), new Vector2(0f, bottomMargin));

        choiceRoot.sizeDelta = new Vector2(Mathf.Max(360f, panelWidth), 220f);
        choiceRoot.anchoredPosition = new Vector2(0f, bottomMargin + panelHeight + framePad * 2f + 12f);
    }

    /// <summary>Fills the line immediately. The continue mark appears when there are no choices yet.</summary>
    public void CompleteReveal()
    {
        if (!revealing)
            return;

        revealing = false;
        bodyLabel.text = fullText;
        if (!choicesVisible)
            continueLabel.gameObject.SetActive(true);
    }

    public void ShowChoices(string[] labels, Action<int> chosen)
    {
        EnsureBuilt();
        ClearChoices();
        if (labels == null || labels.Length == 0)
            return;

        choicesVisible = true;
        continueLabel.gameObject.SetActive(false);
        Sprite sprite = SolidSprite();
        TMP_FontAsset font = Font();

        for (int i = 0; i < labels.Length; i++)
        {
            int captured = i;
            Button button = CreateChoiceButton(choiceRoot, sprite, font, labels[i]);
            button.onClick.AddListener(() => chosen?.Invoke(captured));
            choiceButtons.Add(button);
        }
    }

    public void ClearChoices()
    {
        for (int i = 0; i < choiceButtons.Count; i++)
        {
            if (choiceButtons[i] != null)
                Destroy(choiceButtons[i].gameObject);
        }

        choiceButtons.Clear();
        choicesVisible = false;
    }

    public void Hide()
    {
        ClearChoices();
        revealing = false;
        if (frame != null)
            frame.SetActive(false);
        if (panel != null)
            panel.SetActive(false);
    }

    void Update()
    {
        if (panel == null || !panel.activeSelf)
            return;

        if (revealing)
        {
            revealed += charactersPerSecond * Time.deltaTime;
            int count = Mathf.Clamp(Mathf.FloorToInt(revealed), 0, fullText.Length);
            bodyLabel.text = fullText.Substring(0, count);
            if (count >= fullText.Length)
                CompleteReveal();
            return;
        }

        if (!choicesVisible && continueLabel.gameObject.activeSelf)
        {
            Color color = ContinueColor;
            color.a = 0.45f + Mathf.PingPong(Time.unscaledTime * 0.8f, 0.55f);
            continueLabel.color = color;
        }
    }

    void EnsureBuilt()
    {
        if (canvas != null)
            return;

        Sprite sprite = SolidSprite();
        TMP_FontAsset font = Font();

        var canvasObject = new GameObject("DialogueCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // Frame sits just behind the plate so the purple edge reads as a border.
        Image frameImage = CreateImage("Frame", canvasObject.transform, sprite, FrameColor);
        frame = frameImage.gameObject;
        StretchBottom(frameImage.rectTransform, new Vector2(560f, 150f), new Vector2(0f, 48f));
        frameImage.raycastTarget = false;

        panel = new GameObject("Panel", typeof(RectTransform));
        panel.transform.SetParent(canvasObject.transform, false);
        var plate = panel.AddComponent<Image>();
        plate.sprite = sprite;
        plate.type = Image.Type.Simple;
        plate.color = PanelColor;
        StretchBottom(plate.rectTransform, new Vector2(546f, 136f), new Vector2(0f, 55f));

        portraitFrame = CreateImage("PortraitFrame", panel.transform, sprite, new Color(0.10f, 0.06f, 0.09f, 1f)).gameObject;
        var portraitRect = portraitFrame.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0f, 0.5f);
        portraitRect.anchorMax = new Vector2(0f, 0.5f);
        portraitRect.pivot = new Vector2(0f, 0.5f);
        portraitRect.sizeDelta = new Vector2(168f, 168f);
        portraitRect.anchoredPosition = new Vector2(24f, 0f);

        var portrait = CreateImage("Portrait", portraitFrame.transform, null, Color.white);
        portrait.preserveAspect = true;
        Stretch(portrait.rectTransform, 10f);
        portraitImage = portrait;

        nameLabel = CreateText("Speaker", panel.transform, font, 28, NameColor, FontStyles.SmallCaps);
        var nameRect = nameLabel.rectTransform;
        nameRect.anchorMin = new Vector2(0f, 1f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.pivot = new Vector2(0f, 1f);
        nameRect.offsetMin = new Vector2(216f, -58f);
        nameRect.offsetMax = new Vector2(-28f, -16f);

        bodyLabel = CreateText("Body", panel.transform, font, 24, BodyColor, FontStyles.Normal);
        bodyLabel.alignment = TextAlignmentOptions.TopLeft;
        var bodyRect = bodyLabel.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(216f, 48f);
        bodyRect.offsetMax = new Vector2(-28f, -64f);

        continueLabel = CreateText("Continue", panel.transform, font, 18, ContinueColor, FontStyles.Normal);
        continueLabel.alignment = TextAlignmentOptions.MidlineRight;
        continueLabel.text = "E   ·   Space";
        var continueRect = continueLabel.rectTransform;
        continueRect.anchorMin = new Vector2(1f, 0f);
        continueRect.anchorMax = new Vector2(1f, 0f);
        continueRect.pivot = new Vector2(1f, 0f);
        continueRect.sizeDelta = new Vector2(220f, 32f);
        continueRect.anchoredPosition = new Vector2(-24f, 12f);

        var choices = new GameObject("Choices", typeof(RectTransform));
        choices.transform.SetParent(canvasObject.transform, false);
        choiceRoot = choices.GetComponent<RectTransform>();
        choiceRoot.anchorMin = new Vector2(0.5f, 0f);
        choiceRoot.anchorMax = new Vector2(0.5f, 0f);
        choiceRoot.pivot = new Vector2(0.5f, 0f);
        choiceRoot.sizeDelta = new Vector2(546f, 180f);
        choiceRoot.anchoredPosition = new Vector2(0f, 210f);
        var layout = choices.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.LowerCenter;
        layout.spacing = 8f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    Button CreateChoiceButton(RectTransform parent, Sprite sprite, TMP_FontAsset font, string label)
    {
        var buttonObject = new GameObject("Choice", typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = Color.white;

        var button = buttonObject.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.24f, 0.13f, 0.17f, 0.96f);
        colors.highlightedColor = new Color(0.45f, 0.24f, 0.30f, 1f);
        colors.pressedColor = new Color(0.58f, 0.26f, 0.32f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        var layoutElement = buttonObject.AddComponent<LayoutElement>();
        layoutElement.minHeight = 46f;
        layoutElement.preferredHeight = 46f;

        TMP_Text text = CreateText("Label", buttonObject.transform, font, 22, NameColor, FontStyles.Normal);
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.text = string.IsNullOrEmpty(label) ? "..." : label;
        Stretch(text.rectTransform, 18f);
        return button;
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static void StretchBottom(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    static void Stretch(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    static TMP_FontAsset Font()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    static Sprite solidSprite;

    /// <summary>Flat white sprite for the plates. A sliced built-in sprite is not available in this player.</summary>
    public static Sprite SolidSprite()
    {
        if (solidSprite != null)
            return solidSprite;

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        solidSprite.hideFlags = HideFlags.HideAndDontSave;
        return solidSprite;
    }
}
