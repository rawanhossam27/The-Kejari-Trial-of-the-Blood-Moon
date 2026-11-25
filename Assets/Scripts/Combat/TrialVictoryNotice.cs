using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Center-screen notice when the Blood Moon trial is completed.
/// Uses the same plate colors as the objective tracker and shows only once per flag change.
/// </summary>
[DisallowMultipleComponent]
public class TrialVictoryNotice : MonoBehaviour
{
    [SerializeField, Min(1f)] float holdSeconds = 6.5f;

    CanvasGroup group;
    float hideAt = -1f;
    bool shown;
    bool built;

    static readonly Color PanelColor = new Color(0.06f, 0.035f, 0.05f, 0.94f);
    static readonly Color FrameColor = new Color(0.86f, 0.72f, 0.45f, 0.98f);
    static readonly Color KickerColor = new Color(0.86f, 0.72f, 0.45f, 1f);
    static readonly Color TitleColor = new Color(0.93f, 0.90f, 0.86f, 1f);
    static readonly Color BodyColor = new Color(0.78f, 0.74f, 0.70f, 1f);

    void OnEnable()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged += HandleFlag;
        if (BloodMoonTrial.Instance != null)
            BloodMoonTrial.Instance.Victory += Show;
    }

    void Start()
    {
        if (StoryStateManager.Exists)
        {
            StoryStateManager.Instance.FlagChanged -= HandleFlag;
            StoryStateManager.Instance.FlagChanged += HandleFlag;
        }

        BloodMoonTrial trial = BloodMoonTrial.Instance != null ? BloodMoonTrial.Instance : FindAnyObjectByType<BloodMoonTrial>();
        if (trial != null)
        {
            trial.Victory -= Show;
            trial.Victory += Show;
        }
    }

    void OnDisable()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.FlagChanged -= HandleFlag;
        if (BloodMoonTrial.Instance != null)
            BloodMoonTrial.Instance.Victory -= Show;
    }

    void Update()
    {
        if (!built)
            return;

        float target = shown && Time.unscaledTime < hideAt ? 1f : 0f;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.unscaledDeltaTime / 0.3f);
    }

    void HandleFlag(string flagId, bool value)
    {
        if (value && flagId == StoryStateManager.Flags.TrialCompleted)
            Show();
    }

    public void Show()
    {
        if (shown)
            return;

        shown = true;
        EnsureBuilt();
        hideAt = Time.unscaledTime + holdSeconds;
        group.alpha = 1f;
    }

    void EnsureBuilt()
    {
        if (built)
            return;

        built = true;
        var canvasObject = new GameObject("TrialVictoryCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        group = canvasObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;
        group.alpha = 0f;

        Sprite sprite = Solid();
        TMP_FontAsset font = TMP_Settings.defaultFontAsset;
        if (font == null)
            font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        var frame = CreateImage("Frame", canvasObject.transform, sprite, FrameColor);
        Place(frame.rectTransform, new Vector2(760f, 188f));
        var panel = CreateImage("Panel", canvasObject.transform, sprite, PanelColor);
        Place(panel.rectTransform, new Vector2(744f, 172f));

        var kicker = CreateText("Kicker", panel.transform, font, 18, KickerColor, FontStyles.SmallCaps);
        var kickerRect = kicker.rectTransform;
        kickerRect.anchorMin = new Vector2(0f, 1f);
        kickerRect.anchorMax = new Vector2(1f, 1f);
        kickerRect.pivot = new Vector2(0.5f, 1f);
        kickerRect.offsetMin = new Vector2(28f, -50f);
        kickerRect.offsetMax = new Vector2(-28f, -14f);
        kicker.alignment = TextAlignmentOptions.Center;
        kicker.text = "TRIAL COMPLETED";

        var body = CreateText("Body", panel.transform, font, 24, BodyColor, FontStyles.Normal);
        var bodyRect = body.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(36f, 16f);
        bodyRect.offsetMax = new Vector2(-36f, -58f);
        body.alignment = TextAlignmentOptions.Top;
        body.text = "The Alpha Demon has fallen. All remaining demons have been defeated.";
    }

    static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
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
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    static void Place(RectTransform rect, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, 80f);
    }

    static Sprite solid;

    static Sprite Solid()
    {
        if (solid != null)
            return solid;

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        texture.hideFlags = HideFlags.HideAndDontSave;
        solid = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f), 100f);
        solid.hideFlags = HideFlags.HideAndDontSave;
        return solid;
    }
}
