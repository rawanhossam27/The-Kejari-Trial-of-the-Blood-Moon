using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Master volume and look sensitivity. There is no music or effects bus in this project,
/// so those sliders are not shown.
/// </summary>
public static class GameSettings
{
    const string VolumeKey = "kejari.masterVolume";
    const string LookKey = "kejari.lookSensitivity";

    static readonly Dictionary<CinemachineInputAxisController.Reader, float> lookBaselines =
        new Dictionary<CinemachineInputAxisController.Reader, float>();

    public static float MasterVolume
    {
        get => PlayerPrefs.GetFloat(VolumeKey, 1f);
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, volume);
            PlayerPrefs.Save();
            AudioListener.volume = volume;
        }
    }

    public static float LookSensitivity
    {
        get => PlayerPrefs.GetFloat(LookKey, 1f);
        set
        {
            float sensitivity = Mathf.Clamp(value, 0.4f, 2f);
            PlayerPrefs.SetFloat(LookKey, sensitivity);
            PlayerPrefs.Save();
            ApplyLook(false);
        }
    }

    public static void ApplyVolume()
    {
        AudioListener.volume = MasterVolume;
    }

    /// <summary>
    /// Multiplies each camera reader's authored gain. Recapture after a scene load,
    /// because the readers are new objects and the previous baselines belong to the old scene.
    /// </summary>
    public static void ApplyLook(bool recapture)
    {
        if (recapture)
            lookBaselines.Clear();

        float scale = LookSensitivity;
        var controllers = Object.FindObjectsByType<CinemachineInputAxisController>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int c = 0; c < controllers.Length; c++)
        {
            var readers = controllers[c].Controllers;
            if (readers == null)
                continue;

            for (int i = 0; i < readers.Count; i++)
            {
                var reader = readers[i].Input;
                if (reader == null)
                    continue;

                if (!lookBaselines.TryGetValue(reader, out float baseline))
                {
                    baseline = reader.Gain;
                    lookBaselines.Add(reader, baseline);
                }

                reader.Gain = baseline * scale;
            }
        }
    }
}

/// <summary>
/// Level 2 pause menu. Built once and kept with <see cref="GameSessionHost"/>.
/// Escape is handled only by that host, so the cursor lock does not also consume it.
/// </summary>
[DisallowMultipleComponent]
public class LevelPauseMenu : MonoBehaviour
{
    public static int ClosedFrame { get; private set; } = -1;

    public static bool OwnsEscape =>
        SceneManagerActiveTrial();

    public bool IsOpen { get; private set; }

    float savedTimeScale = 1f;
    CanvasGroup rootGroup;
    GameObject commands;
    GameObject settings;
    Button resumeButton;
    Button settingsButton;
    Slider volumeSlider;
    Slider lookSlider;
    TextMeshProUGUI volumeValue;
    TextMeshProUGUI lookValue;
    GameObject corner;

    static bool SceneManagerActiveTrial()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        return scene.IsValid() && scene.name == GameSession.TrialScene;
    }

    void Awake()
    {
        if (rootGroup == null)
            Build();
    }

    void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshCorner();
    }

    void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        RefreshCorner();
    }

    /// <summary>Opens the menu from the corner button. Escape uses the same rules.</summary>
    public void RequestOpen()
    {
        if (IsOpen || !SceneManagerActiveTrial())
            return;

        if (SceneTransitionController.Exists && SceneTransitionController.Instance.IsTransitioning)
            return;

        Open();
    }

    public void Open()
    {
        if (IsOpen)
            return;

        if (rootGroup == null)
            Build();

        savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        IsOpen = true;
        OrayaMovement.GameplaySuspended = true;
        RefreshCameras();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        rootGroup.alpha = 1f;
        rootGroup.interactable = true;
        rootGroup.blocksRaycasts = true;
        ShowCommands();
        RefreshCorner();
    }

    /// <summary>
    /// <paramref name="resumeGameplay"/> restores the time scale and cursor from before the pause.
    /// Leaving the scene passes false so the next scene starts at a normal time scale.
    /// </summary>
    public void Close(bool resumeGameplay)
    {
        bool wasOpen = IsOpen;
        IsOpen = false;
        if (wasOpen)
            ClosedFrame = Time.frameCount;

        if (rootGroup != null)
        {
            rootGroup.alpha = 0f;
            rootGroup.interactable = false;
            rootGroup.blocksRaycasts = false;
        }

        OrayaMovement.GameplaySuspended = false;
        RefreshCameras();
        Time.timeScale = resumeGameplay ? savedTimeScale : 1f;

        if (resumeGameplay)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        RefreshCorner();
    }

    void RefreshCorner()
    {
        if (corner != null)
            corner.SetActive(!IsOpen && SceneManagerActiveTrial());
    }

    static void RefreshCameras()
    {
        var cameras = Object.FindObjectsByType<OrayaCameraManager>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
            cameras[i].RefreshInputGate();
    }

    void ShowCommands()
    {
        commands.SetActive(true);
        settings.SetActive(false);
        if (EventSystem.current != null && resumeButton != null)
            EventSystem.current.SetSelectedGameObject(resumeButton.gameObject);
    }

    void ShowSettings()
    {
        volumeSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
        lookSlider.SetValueWithoutNotify(GameSettings.LookSensitivity);
        RefreshValueLabels();
        commands.SetActive(false);
        settings.SetActive(true);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(volumeSlider.gameObject);
    }

    void RestartCheckpoint()
    {
        Close(false);
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.ResetStory();
        if (ObjectiveManager.Exists)
            ObjectiveManager.Instance.ResetProgress();

        GameSession.Load(GameSession.TrialScene);
    }

    void ReturnToMenu()
    {
        Close(false);
        GameSession.ReturnToMenu();
    }

    void QuitGame()
    {
        Close(false);
        GameSession.Quit();
    }

    void RefreshValueLabels()
    {
        volumeValue.text = Mathf.RoundToInt(volumeSlider.value * 100f) + "%";
        lookValue.text = lookSlider.value.ToString("0.00");
    }

    void Build()
    {
        EnsureEventSystem();

        var canvasObject = new GameObject("PauseCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 450;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        BuildCorner(canvasObject.transform);

        var menuRoot = new GameObject("Menu", typeof(RectTransform));
        menuRoot.transform.SetParent(canvasObject.transform, false);
        Stretch(menuRoot.GetComponent<RectTransform>());
        rootGroup = menuRoot.AddComponent<CanvasGroup>();
        rootGroup.alpha = 0f;
        rootGroup.interactable = false;
        rootGroup.blocksRaycasts = false;

        var shade = CreateImage(menuRoot.transform, "Shade", new Color(0.015f, 0.006f, 0.01f, 0.66f));
        Stretch(shade.rectTransform);

        var plate = CreateImage(menuRoot.transform, "Plate", new Color(0.05f, 0.022f, 0.032f, 0.9f));
        var plateRect = plate.rectTransform;
        plateRect.anchorMin = plateRect.anchorMax = new Vector2(0.5f, 0.5f);
        plateRect.sizeDelta = new Vector2(560f, 700f);

        var frame = CreateImage(plate.transform, "Frame", new Color(0.56f, 0.42f, 0.28f, 0.9f));
        Stretch(frame.rectTransform);
        frame.raycastTarget = false;
        var inset = CreateImage(plate.transform, "Inset", new Color(0.05f, 0.022f, 0.032f, 0.96f));
        Stretch(inset.rectTransform);
        inset.rectTransform.offsetMin = new Vector2(2f, 2f);
        inset.rectTransform.offsetMax = new Vector2(-2f, -2f);

        commands = new GameObject("Commands", typeof(RectTransform));
        commands.transform.SetParent(plate.transform, false);
        Stretch(commands.GetComponent<RectTransform>());
        BuildCommands(commands.transform);

        settings = new GameObject("Settings", typeof(RectTransform));
        settings.transform.SetParent(plate.transform, false);
        Stretch(settings.GetComponent<RectTransform>());
        BuildSettings(settings.transform);
        settings.SetActive(false);
        RefreshCorner();
    }

    void BuildCorner(Transform parent)
    {
        corner = new GameObject("PauseCorner", typeof(RectTransform));
        corner.transform.SetParent(parent, false);
        var plate = corner.AddComponent<Image>();
        plate.color = new Color(0.08f, 0.04f, 0.055f, 0.92f);
        var rect = plate.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-28f, -28f);
        rect.sizeDelta = new Vector2(142f, 42f);

        var accent = CreateImage(corner.transform, "Accent", new Color(0.9f, 0.74f, 0.42f, 1f));
        var accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(3f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accent.raycastTarget = false;

        var barColor = new Color(0.9f, 0.78f, 0.5f, 1f);
        var leftBar = CreateImage(corner.transform, "GlyphLeft", barColor);
        Place(leftBar.rectTransform, new Vector2(-46f, 0f), new Vector2(4f, 16f));
        leftBar.raycastTarget = false;
        var rightBar = CreateImage(corner.transform, "GlyphRight", barColor);
        Place(rightBar.rectTransform, new Vector2(-36f, 0f), new Vector2(4f, 16f));
        rightBar.raycastTarget = false;

        var label = CreateText(corner.transform, "PAUSE", 16, new Color(0.93f, 0.9f, 0.86f), FontStyles.Normal, 1.5f);
        var labelRect = label.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(40f, 0f);
        labelRect.offsetMax = new Vector2(-8f, 0f);

        var button = corner.AddComponent<Button>();
        button.targetGraphic = plate;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.04f, 0.055f, 0.92f);
        colors.highlightedColor = new Color(0.24f, 0.08f, 0.1f, 0.98f);
        colors.pressedColor = new Color(0.38f, 0.1f, 0.12f, 1f);
        colors.selectedColor = new Color(0.32f, 0.16f, 0.1f, 1f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(RequestOpen);

        var view = corner.AddComponent<MenuButtonView>();
        view.Bind(accent, label);
    }

    void BuildCommands(Transform parent)
    {
        var title = CreateText(parent, "THE KEJARI", 46, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 8f);
        Place(title.rectTransform, new Vector2(0f, 250f), new Vector2(500f, 56f));

        var rule = CreateImage(parent, "Rule", new Color(0.56f, 0.42f, 0.28f, 0.9f));
        Place(rule.rectTransform, new Vector2(0f, 214f), new Vector2(180f, 2f));
        rule.raycastTarget = false;

        var subtitle = CreateText(parent, "TRIAL OF THE BLOOD MOON", 18, new Color(0.78f, 0.42f, 0.46f), FontStyles.Normal, 2.2f);
        Place(subtitle.rectTransform, new Vector2(0f, 184f), new Vector2(500f, 28f));

        var paused = CreateText(parent, "PAUSED", 26, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 4f);
        Place(paused.rectTransform, new Vector2(0f, 142f), new Vector2(500f, 36f));

        resumeButton = CreateButton(parent, "RESUME", new Vector2(0f, 64f), () => Close(true));
        Button restart = CreateButton(parent, "RESTART CHECKPOINT", new Vector2(0f, 4f), RestartCheckpoint);
        settingsButton = CreateButton(parent, "SETTINGS", new Vector2(0f, -56f), ShowSettings);
        Button menu = CreateButton(parent, "MAIN MENU", new Vector2(0f, -116f), ReturnToMenu);
        Button quit = CreateButton(parent, "QUIT GAME", new Vector2(0f, -176f), QuitGame);

        Link(resumeButton, quit, restart);
        Link(restart, resumeButton, settingsButton);
        Link(settingsButton, restart, menu);
        Link(menu, settingsButton, quit);
        Link(quit, menu, resumeButton);
    }

    void BuildSettings(Transform parent)
    {
        var heading = CreateText(parent, "SETTINGS", 32, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 4f);
        Place(heading.rectTransform, new Vector2(0f, 220f), new Vector2(500f, 42f));

        var rule = CreateImage(parent, "Rule", new Color(0.56f, 0.42f, 0.28f, 0.9f));
        Place(rule.rectTransform, new Vector2(0f, 188f), new Vector2(140f, 2f));
        rule.raycastTarget = false;

        volumeValue = CreateText(parent, "100%", 18, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 0.6f);
        lookValue = CreateText(parent, "1.00", 18, new Color(0.9f, 0.78f, 0.5f), FontStyles.Normal, 0.6f);
        volumeSlider = CreateSlider(parent, "MASTER VOLUME", new Vector2(0f, 90f), 0f, 1f, GameSettings.MasterVolume, volumeValue, value => GameSettings.MasterVolume = value);
        lookSlider = CreateSlider(parent, "LOOK SENSITIVITY", new Vector2(0f, -10f), 0.4f, 2f, GameSettings.LookSensitivity, lookValue, value => GameSettings.LookSensitivity = value);

        Button back = CreateButton(parent, "BACK", new Vector2(0f, -180f), ShowCommands);
        var sliderNav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = back, selectOnDown = lookSlider };
        volumeSlider.navigation = sliderNav;
        lookSlider.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = volumeSlider, selectOnDown = back };
        back.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = lookSlider, selectOnDown = volumeSlider };
    }

    Button CreateButton(Transform parent, string caption, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var buttonObject = new GameObject(caption, typeof(RectTransform));
        buttonObject.transform.SetParent(parent, false);
        var plate = buttonObject.AddComponent<Image>();
        plate.color = new Color(0.08f, 0.04f, 0.055f, 0.94f);
        var rect = plate.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(440f, 50f);

        var accent = CreateImage(buttonObject.transform, "Accent", new Color(0.28f, 0.14f, 0.12f, 0.9f));
        var accentRect = accent.rectTransform;
        accentRect.anchorMin = new Vector2(0f, 0f);
        accentRect.anchorMax = new Vector2(0f, 1f);
        accentRect.pivot = new Vector2(0f, 0.5f);
        accentRect.sizeDelta = new Vector2(3f, 0f);
        accentRect.anchoredPosition = Vector2.zero;
        accent.raycastTarget = false;

        var label = CreateText(buttonObject.transform, caption, 20, new Color(0.93f, 0.9f, 0.86f), FontStyles.Normal, 1.6f);
        Stretch(label.rectTransform);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = plate;
        var colors = button.colors;
        colors.normalColor = new Color(0.08f, 0.04f, 0.055f, 0.94f);
        colors.highlightedColor = new Color(0.24f, 0.08f, 0.1f, 0.98f);
        colors.pressedColor = new Color(0.38f, 0.1f, 0.12f, 1f);
        colors.selectedColor = new Color(0.32f, 0.16f, 0.1f, 1f);
        colors.disabledColor = new Color(0.08f, 0.04f, 0.055f, 0.38f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        button.onClick.AddListener(action);

        var view = buttonObject.AddComponent<MenuButtonView>();
        view.Bind(accent, label);
        buttonObject.AddComponent<PauseButtonMotion>();
        return button;
    }

    Slider CreateSlider(Transform parent, string caption, Vector2 position, float min, float max, float value, TextMeshProUGUI valueLabel, UnityEngine.Events.UnityAction<float> onChanged)
    {
        var label = CreateText(parent, caption, 16, new Color(0.75f, 0.71f, 0.78f), FontStyles.Normal, 1.2f);
        Place(label.rectTransform, position + new Vector2(-150f, 28f), new Vector2(280f, 24f));
        label.alignment = TextAlignmentOptions.MidlineLeft;
        Place(valueLabel.rectTransform, position + new Vector2(160f, 28f), new Vector2(90f, 24f));
        valueLabel.alignment = TextAlignmentOptions.MidlineRight;

        var sliderObject = new GameObject(caption, typeof(RectTransform));
        sliderObject.transform.SetParent(parent, false);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = position;
        sliderRect.sizeDelta = new Vector2(420f, 22f);

        var background = CreateImage(sliderObject.transform, "Background", new Color(0.1f, 0.05f, 0.06f, 1f));
        Stretch(background.rectTransform);

        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        Stretch(fillArea.GetComponent<RectTransform>());
        var fill = CreateImage(fillArea.transform, "Fill", new Color(0.45f, 0.12f, 0.16f, 1f));
        Stretch(fill.rectTransform);

        var handle = CreateImage(sliderObject.transform, "Handle", new Color(0.9f, 0.78f, 0.5f, 1f));
        handle.rectTransform.sizeDelta = new Vector2(14f, 26f);

        var slider = sliderObject.AddComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.SetValueWithoutNotify(value);
        slider.onValueChanged.AddListener(next =>
        {
            onChanged(next);
            RefreshValueLabels();
        });

        var colors = slider.colors;
        colors.normalColor = new Color(0.9f, 0.78f, 0.5f, 1f);
        colors.highlightedColor = new Color(1f, 0.9f, 0.62f, 1f);
        colors.pressedColor = new Color(0.72f, 0.5f, 0.28f, 1f);
        colors.selectedColor = new Color(1f, 0.9f, 0.62f, 1f);
        colors.disabledColor = new Color(0.4f, 0.34f, 0.3f, 0.6f);
        slider.colors = colors;
        return slider;
    }

    static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        var system = new GameObject("EventSystem");
        system.AddComponent<EventSystem>();
        system.AddComponent<InputSystemUIInputModule>();
    }

    static void Link(Selectable current, Selectable up, Selectable down)
    {
        current.navigation = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
    }

    static Image CreateImage(Transform parent, string name, Color color)
    {
        var imageObject = new GameObject(name, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.AddComponent<Image>();
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

/// <summary>A small scale response so the selected pause button is obvious to mouse and keyboard.</summary>
public class PauseButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    bool hot;

    void Update()
    {
        float target = hot ? 1.035f : 1f;
        float blend = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * target, blend);
    }

    public void OnPointerEnter(PointerEventData eventData) => hot = true;
    public void OnPointerExit(PointerEventData eventData) => hot = false;
    public void OnSelect(BaseEventData eventData) => hot = true;
    public void OnDeselect(BaseEventData eventData) => hot = false;
}
