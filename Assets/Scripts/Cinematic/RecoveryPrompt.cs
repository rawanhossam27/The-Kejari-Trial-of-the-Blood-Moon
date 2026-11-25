using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Bottom-center interaction line for the Recovery Room.
/// Standing prompts come from <see cref="ArenaPickups"/>. Bed rest prompts come from <see cref="RecoveryRoomFlow"/>.
/// </summary>
[DisallowMultipleComponent]
public class RecoveryPrompt : MonoBehaviour
{
    [SerializeField] RecoveryRoomFlow flow;
    [SerializeField] OrayaMovement movement;

    [SerializeField] Sprite plateSprite;

    CanvasGroup group;
    RectTransform plate;
    TMP_Text label;

    void Awake()
    {
        if (flow == null)
            flow = FindAnyObjectByType<RecoveryRoomFlow>();
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        Build();
    }

    void Update()
    {
        string text = flow != null ? flow.OverridePrompt : null;
        if (string.IsNullOrEmpty(text) && movement != null && movement.InputEnabled && !movement.CinematicMoveActive)
        {
            IArenaPickup closest = ArenaPickups.ClosestInRange();
            if (closest != null)
                text = closest.Prompt;
        }

        bool show = !string.IsNullOrEmpty(text);
        if (label != null && show && label.text != text)
        {
            label.text = text;
            FitPlate();
        }
        if (group != null)
            group.alpha = show ? 1f : 0f;
    }

    void FitPlate()
    {
        if (plate == null || label == null)
            return;

        label.ForceMeshUpdate();
        float width = Mathf.Max(340f, label.preferredWidth + 36f);
        plate.sizeDelta = new Vector2(width, 38f);
    }

    void Build()
    {
        var canvasObject = new GameObject("RecoveryPromptCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;

        var plateObject = new GameObject("Prompt", typeof(RectTransform));
        plateObject.transform.SetParent(canvasObject.transform, false);
        plate = plateObject.GetComponent<RectTransform>();
        plate.anchorMin = new Vector2(0.5f, 0f);
        plate.anchorMax = new Vector2(0.5f, 0f);
        plate.pivot = new Vector2(0.5f, 0f);
        plate.anchoredPosition = new Vector2(0f, 78f);
        plate.sizeDelta = new Vector2(340f, 38f);

        var background = CreateImage("Background", plate, plateSprite, new Color(0.125f, 0.094f, 0.149f, 0.9f));
        Stretch(background.rectTransform);
        var accent = CreateImage("Accent", plate, plateSprite, new Color(0.28f, 0.7f, 0.38f, 1f));
        accent.rectTransform.anchorMin = new Vector2(0f, 0f);
        accent.rectTransform.anchorMax = new Vector2(0f, 1f);
        accent.rectTransform.pivot = new Vector2(0f, 0.5f);
        accent.rectTransform.anchoredPosition = Vector2.zero;
        accent.rectTransform.sizeDelta = new Vector2(3f, 0f);

        var textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(plate, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12f, 0f);
        rect.offsetMax = new Vector2(-8f, 0f);
        label = textObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 18f;
        label.characterSpacing = 1.4f;
        label.color = new Color(0.753f, 0.71f, 0.784f, 1f);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
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

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
