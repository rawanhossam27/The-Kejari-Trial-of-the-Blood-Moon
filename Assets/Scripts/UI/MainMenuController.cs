using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Builds the gothic main menu. The scene only needs this component, a camera, and the background sprite.
/// </summary>
[DisallowMultipleComponent]
public class MainMenuController : MonoBehaviour
{
    [SerializeField] Sprite background;
    [SerializeField] Sprite buttonPlate;

    readonly List<RectTransform> motes = new List<RectTransform>();
    readonly List<float> moteSpeeds = new List<float>();
    RectTransform mist;
    Image moonGlow;
    Button playButton;
    Button continueButton;
    Button quitButton;
    MenuButtonView continueView;

    void Awake()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SceneTransitionController.Exists)
            SceneTransitionController.Instance.SetReveal(1f, 0f);

        Build();
    }

    void Start()
    {
        if (EventSystem.current != null && playButton != null)
            EventSystem.current.SetSelectedGameObject(playButton.gameObject);
    }

    void Update()
    {
        float t = Time.unscaledTime;
        if (moonGlow != null)
        {
            Color glow = moonGlow.color;
            glow.a = 0.2f + Mathf.Sin(t * 0.7f) * 0.08f;
            moonGlow.color = glow;
        }

        if (mist != null)
            mist.anchoredPosition = new Vector2(Mathf.Sin(t * 0.15f) * 36f, Mathf.Sin(t * 0.23f) * 10f);

        for (int i = 0; i < motes.Count; i++)
        {
            RectTransform mote = motes[i];
            Vector2 position = mote.anchoredPosition;
            position.y += moteSpeeds[i] * Time.unscaledDeltaTime;
            position.x += Mathf.Sin(t * 0.4f + i) * 6f * Time.unscaledDeltaTime;
            if (position.y > 620f)
                position.y = -520f;
            mote.anchoredPosition = position;
        }
    }

    void Build()
    {
        EnsureEventSystem();

        var canvasObject = new GameObject("MenuCanvas", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var backdrop = CreateImage(canvasObject.transform, "Background", background, Color.white);
        Stretch(backdrop.rectTransform);
        backdrop.preserveAspect = false;

        var vignette = CreateImage(canvasObject.transform, "Vignette", null, new Color(0.02f, 0.005f, 0.01f, 0.22f));
        Stretch(vignette.rectTransform);
        vignette.raycastTarget = false;

        moonGlow = CreateImage(canvasObject.transform, "MoonGlow", RadialSprite(), new Color(0.55f, 0.08f, 0.1f, 0.22f));
        var glowRect = moonGlow.rectTransform;
        glowRect.anchorMin = glowRect.anchorMax = new Vector2(0.2f, 0.74f);
        glowRect.sizeDelta = new Vector2(820f, 820f);
        moonGlow.raycastTarget = false;

        mist = CreateImage(canvasObject.transform, "Mist", MistSprite(), new Color(0.82f, 0.8f, 0.84f, 0.28f)).rectTransform;
        mist.anchorMin = new Vector2(0f, 0f);
        mist.anchorMax = new Vector2(1f, 0f);
        mist.pivot = new Vector2(0.5f, 0f);
        mist.sizeDelta = new Vector2(200f, 220f);
        mist.anchoredPosition = new Vector2(0f, 0f);
        mist.GetComponent<Image>().raycastTarget = false;

        BuildMotes(canvasObject.transform);
        BuildMainColumn(canvasObject.transform);
        RefreshContinue();
    }

    void BuildMotes(Transform parent)
    {
        for (int i = 0; i < 16; i++)
        {
            var mote = CreateImage(parent, "Mote", null, new Color(0.86f, 0.72f, 0.45f, Random.Range(0.15f, 0.4f)));
            var rect = mote.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            float size = Random.Range(2f, 5f);
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = new Vector2(Random.Range(-860f, 860f), Random.Range(-480f, 480f));
            mote.raycastTarget = false;
            motes.Add(rect);
            moteSpeeds.Add(Random.Range(8f, 22f));
        }
    }

    void BuildMainColumn(Transform parent)
    {
        var mainColumn = new GameObject("MainColumn", typeof(RectTransform));
        mainColumn.transform.SetParent(parent, false);
        var column = mainColumn.GetComponent<RectTransform>();
        column.anchorMin = column.anchorMax = new Vector2(0.5f, 0.5f);
        column.anchoredPosition = new Vector2(0f, 20f);
        column.sizeDelta = new Vector2(640f, 760f);

        var title = CreateText(column, "THE KEJARI", 78, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 14f);
        Place(title.rectTransform, new Vector2(0f, 250f), new Vector2(640f, 90f));

        var rule = CreateImage(column, "Rule", null, new Color(0.56f, 0.42f, 0.28f, 0.85f));
        Place(rule.rectTransform, new Vector2(0f, 196f), new Vector2(220f, 2f));
        rule.raycastTarget = false;

        var subtitle = CreateText(column, "Trial of the Blood Moon", 30, new Color(0.78f, 0.42f, 0.46f), FontStyles.Italic, 1.5f);
        Place(subtitle.rectTransform, new Vector2(0f, 150f), new Vector2(640f, 42f));

        var tagline = CreateText(column, "Survive the trials. Earn Nyaxia's favor.", 18, new Color(0.75f, 0.71f, 0.78f), FontStyles.Normal, 0.8f);
        Place(tagline.rectTransform, new Vector2(0f, 104f), new Vector2(640f, 32f));

        playButton = CreateMenuButton(column, "Play", new Vector2(0f, 10f), GameSession.BeginNewGame, out _);
        continueButton = CreateMenuButton(column, "Continue", new Vector2(0f, -62f), GameSession.Continue, out continueView);
        quitButton = CreateMenuButton(column, "Quit", new Vector2(0f, -134f), GameSession.Quit, out _);

        var footer = CreateText(column, "Prototype Build", 14, new Color(0.55f, 0.48f, 0.5f), FontStyles.Normal, 1.6f);
        Place(footer.rectTransform, new Vector2(0f, -320f), new Vector2(640f, 28f));
    }

    Button CreateMenuButton(Transform parent, string caption, Vector2 position, UnityEngine.Events.UnityAction action, out MenuButtonView view)
    {
        var buttonObject = new GameObject(caption, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        var plate = buttonObject.AddComponent<Image>();
        plate.sprite = buttonPlate;
        plate.type = buttonPlate != null ? Image.Type.Sliced : Image.Type.Simple;
        plate.color = new Color(0.08f, 0.04f, 0.055f, 0.9f);
        var rect = plate.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(460f, 52f);

        var accent = CreateImage(buttonObject.transform, "Accent", null, new Color(0.56f, 0.36f, 0.24f, 1f));
        var accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(3f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accent.raycastTarget = false;

        var label = CreateText(buttonObject.transform, caption, 22, new Color(0.93f, 0.9f, 0.86f), FontStyles.Normal, 1.8f);
        Stretch(label.rectTransform);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = plate;
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.04f, 0.055f, 0.92f);
        colors.highlightedColor = new Color(0.24f, 0.08f, 0.1f, 0.98f);
        colors.pressedColor = new Color(0.38f, 0.1f, 0.12f, 1f);
        colors.selectedColor = new Color(0.32f, 0.16f, 0.1f, 1f);
        colors.disabledColor = new Color(0.08f, 0.04f, 0.055f, 0.38f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        if (action != null)
            button.onClick.AddListener(action);

        view = buttonObject.AddComponent<MenuButtonView>();
        view.Bind(accent, label);
        return button;
    }

    void RefreshContinue()
    {
        bool available = GameSession.HasContinue;
        continueButton.interactable = available;
        continueView.SetDimmed(!available);
        if (available)
        {
            Link(playButton, quitButton, continueButton);
            Link(continueButton, playButton, quitButton);
            Link(quitButton, continueButton, playButton);
        }
        else
        {
            Link(playButton, quitButton, quitButton);
            Link(quitButton, playButton, playButton);
        }
    }

    static void Link(Button button, Button up, Button down)
    {
        var navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
        button.navigation = navigation;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var system = new GameObject("EventSystem");
        system.AddComponent<EventSystem>();
        system.AddComponent<InputSystemUIInputModule>();
    }

    static Sprite RadialSprite()
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color[size * size];
        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float falloff = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                pixels[y * size + x] = new Color(1f, 1f, 1f, falloff * falloff);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    static Sprite MistSprite()
    {
        const int height = 64;
        var texture = new Texture2D(4, height, TextureFormat.RGBA32, false);
        var pixels = new Color[4 * height];
        for (int y = 0; y < height; y++)
        {
            float along = y / (height - 1f);
            float alpha = Mathf.Sin(along * Mathf.PI);
            var color = new Color(1f, 1f, 1f, alpha * alpha * 0.55f);
            for (int x = 0; x < 4; x++)
                pixels[y * 4 + x] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 4f, height), new Vector2(0.5f, 0f));
    }

    static Image CreateImage(Transform parent, string name, Sprite sprite, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        return image;
    }

    static TextMeshProUGUI CreateText(Transform parent, string text, float size, Color color, FontStyles style, float spacing)
    {
        var labelObject = new GameObject(text, typeof(RectTransform));
        labelObject.transform.SetParent(parent, false);
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.font = TMP_Settings.defaultFontAsset;
        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.characterSpacing = spacing;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}

/// <summary>
/// Gold accent and label response for keyboard selection and mouse hover.
/// </summary>
public class MenuButtonView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    Image accent;
    TextMeshProUGUI label;
    bool dimmed;
    bool highlighted;

    static readonly Color AccentIdle = new Color(0.28f, 0.14f, 0.12f, 0.9f);
    static readonly Color AccentHot = new Color(0.9f, 0.74f, 0.42f, 1f);
    static readonly Color LabelIdle = new Color(0.93f, 0.9f, 0.86f, 1f);
    static readonly Color LabelHot = new Color(0.95f, 0.84f, 0.58f, 1f);
    static readonly Color LabelDim = new Color(0.55f, 0.48f, 0.5f, 0.55f);

    public void Bind(Image accentBar, TextMeshProUGUI caption)
    {
        accent = accentBar;
        label = caption;
        Apply();
    }

    public void SetDimmed(bool value)
    {
        dimmed = value;
        Apply();
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHot(true);
    public void OnPointerExit(PointerEventData eventData) => SetHot(false);
    public void OnSelect(BaseEventData eventData) => SetHot(true);
    public void OnDeselect(BaseEventData eventData) => SetHot(false);

    void SetHot(bool value)
    {
        highlighted = value;
        Apply();
    }

    void Apply()
    {
        if (accent != null)
            accent.color = highlighted && !dimmed ? AccentHot : AccentIdle;
        if (label != null)
            label.color = dimmed ? LabelDim : (highlighted ? LabelHot : LabelIdle);
    }
}
