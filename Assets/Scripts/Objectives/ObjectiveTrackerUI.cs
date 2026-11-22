using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Top-center objective plate. It uses the same scale and colors as the dialogue panel,
/// and stays above the health plate and clear of the bottom dialogue and prompt.
/// </summary>
[DisallowMultipleComponent]
public class ObjectiveTrackerUI : MonoBehaviour
{
    [SerializeField] ObjectiveManager manager;
    [SerializeField, Min(0.2f)] float completedHold = 1.5f;

    Canvas canvas;
    CanvasGroup group;
    Image frame;
    TMP_Text kicker;
    TMP_Text title;
    TMP_Text body;
    TMP_Text indexLabel;
    RectTransform panelRect;
    EntrySequenceController entry;

    ObjectiveData banner;
    float bannerUntil;
    bool built;

    static readonly Color PanelColor = new Color(0.06f, 0.035f, 0.05f, 0.94f);
    static readonly Color FrameColor = new Color(0.56f, 0.42f, 0.67f, 0.95f);
    static readonly Color CompleteFrameColor = new Color(0.86f, 0.72f, 0.45f, 0.98f);
    static readonly Color KickerColor = new Color(0.86f, 0.72f, 0.45f, 1f);
    static readonly Color CompleteColor = new Color(0.78f, 0.38f, 0.42f, 1f);
    static readonly Color TitleColor = new Color(0.93f, 0.90f, 0.86f, 1f);
    static readonly Color BodyColor = new Color(0.78f, 0.74f, 0.70f, 1f);

    public Canvas RootCanvas => canvas;
    public RectTransform Panel => panelRect;
    public string KickerText => kicker != null ? kicker.text : string.Empty;
    public string TitleText => title != null ? title.text : string.Empty;
    public string BodyText => body != null ? body.text : string.Empty;
    public bool IsShowingCompletion => banner != null;
    public float PanelAlpha => group != null ? group.alpha : 0f;

    void Awake()
    {
        if (manager == null)
            manager = GetComponent<ObjectiveManager>();
        EnsureBuilt();
        HideImmediate();
    }

    void OnEnable()
    {
        if (manager == null)
            manager = GetComponent<ObjectiveManager>();
        if (manager == null)
            return;

        manager.ObjectiveActivated += HandleActivated;
        manager.ObjectiveCompleted += HandleCompleted;
        manager.ObjectiveReplaced += HandleReplaced;
    }

    void Start()
    {
        entry = FindAnyObjectByType<EntrySequenceController>();
        if (manager != null && banner == null)
            ShowObjective(manager.Current, manager.IsCurrentComplete);
    }

    void OnDisable()
    {
        if (manager == null)
            return;

        manager.ObjectiveActivated -= HandleActivated;
        manager.ObjectiveCompleted -= HandleCompleted;
        manager.ObjectiveReplaced -= HandleReplaced;
    }

    void Update()
    {
        if (!built)
            return;

        if (banner != null && Time.unscaledTime >= bannerUntil)
        {
            banner = null;
            ShowObjective(manager != null ? manager.Current : null, manager != null && manager.IsCurrentComplete);
        }

        bool visible = banner != null || (manager != null && manager.Current != null);
        if (entry != null && !entry.IsComplete)
            visible = false;

        float target = visible ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.35f);

        if (banner != null && frame != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
            frame.color = Color.Lerp(FrameColor, CompleteFrameColor, pulse);
        }
    }

    void HandleActivated(ObjectiveData objective)
    {
        if (banner != null)
            return;

        ShowObjective(objective, false);
    }

    void HandleCompleted(ObjectiveData objective)
    {
        banner = objective;
        bannerUntil = Time.unscaledTime + completedHold;
        ShowObjective(objective, true);
    }

    void HandleReplaced(ObjectiveData previous, ObjectiveData next)
    {
        if (banner != null)
            return;

        ShowObjective(next, false);
    }

    void ShowObjective(ObjectiveData objective, bool completed)
    {
        EnsureBuilt();
        if (objective == null)
        {
            HideImmediate();
            return;
        }

        panelRect.gameObject.SetActive(true);
        kicker.text = completed ? "COMPLETE" : "OBJECTIVE";
        kicker.color = completed ? CompleteColor : KickerColor;
        title.text = string.IsNullOrEmpty(objective.title) ? "..." : objective.title;
        title.fontStyle = completed ? FontStyles.Strikethrough : FontStyles.SmallCaps;
        title.color = completed ? KickerColor : TitleColor;
        body.text = objective.description ?? string.Empty;
        indexLabel.text = IndexText(objective);
        if (!completed && frame != null)
            frame.color = FrameColor;
    }

    string IndexText(ObjectiveData objective)
    {
        if (manager == null || manager.Sequence == null || manager.Sequence.Count == 0)
            return string.Empty;

        int found = -1;
        int count = 0;
        for (int i = 0; i < manager.Sequence.Count; i++)
        {
            if (manager.Sequence[i] == null)
                continue;

            count++;
            if (manager.Sequence[i] == objective)
                found = count;
        }

        if (found < 0)
            return string.Empty;

        return found + "  /  " + count;
    }

    void HideImmediate()
    {
        if (panelRect != null)
            panelRect.gameObject.SetActive(false);
    }

    void EnsureBuilt()
    {
        if (built)
            return;

        built = true;
        Sprite sprite = SolidSprite();
        TMP_FontAsset font = Font();

        var canvasObject = new GameObject("ObjectiveCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        group = canvasObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        var frameImage = CreateImage("Frame", canvasObject.transform, sprite, FrameColor);
        frame = frameImage;
        PlaceTopCenter(frameImage.rectTransform, new Vector2(500f, 132f), new Vector2(0f, -28f));

        var panel = CreateImage("Panel", canvasObject.transform, sprite, PanelColor);
        panelRect = panel.rectTransform;
        PlaceTopCenter(panelRect, new Vector2(484f, 116f), new Vector2(0f, -36f));

        kicker = CreateText("Kicker", panelRect, font, 16, KickerColor, FontStyles.SmallCaps);
        var kickerRect = kicker.rectTransform;
        kickerRect.anchorMin = new Vector2(0f, 1f);
        kickerRect.anchorMax = new Vector2(1f, 1f);
        kickerRect.pivot = new Vector2(0f, 1f);
        kickerRect.offsetMin = new Vector2(22f, -36f);
        kickerRect.offsetMax = new Vector2(-90f, -12f);
        kicker.text = "OBJECTIVE";

        indexLabel = CreateText("Index", panelRect, font, 16, KickerColor, FontStyles.Normal);
        indexLabel.alignment = TextAlignmentOptions.MidlineRight;
        var indexRect = indexLabel.rectTransform;
        indexRect.anchorMin = new Vector2(1f, 1f);
        indexRect.anchorMax = new Vector2(1f, 1f);
        indexRect.pivot = new Vector2(1f, 1f);
        indexRect.sizeDelta = new Vector2(90f, 24f);
        indexRect.anchoredPosition = new Vector2(-18f, -12f);

        title = CreateText("Title", panelRect, font, 26, TitleColor, FontStyles.SmallCaps);
        var titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.offsetMin = new Vector2(22f, -72f);
        titleRect.offsetMax = new Vector2(-22f, -36f);

        body = CreateText("Body", panelRect, font, 18, BodyColor, FontStyles.Normal);
        body.alignment = TextAlignmentOptions.TopLeft;
        var bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(22f, 12f);
        bodyRect.offsetMax = new Vector2(-22f, -74f);
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

    static TMP_Text CreateText(string objectName, Transform parent, TMP_FontAsset font, float size, Color color, FontStyles style)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        var text = textObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        return text;
    }

    static void PlaceTopCenter(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    static TMP_FontAsset Font()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        return Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
    }

    static Sprite solidSprite;

    static Sprite SolidSprite()
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
