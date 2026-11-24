using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// World-space bar for any <see cref="Damageable"/>. Stays parented to that character.
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] Damageable health;
    [SerializeField] string displayName = "Serana";
    [SerializeField] float height = 2.05f;

    static Sprite quad;

    Transform barRoot;
    Image fill;
    Text nameLabel;
    bool dead;

    static readonly Color Plate = new Color(0.06f, 0.035f, 0.05f, 0.94f);
    static readonly Color Frame = new Color(0.56f, 0.42f, 0.67f, 0.95f);
    static readonly Color Fill = new Color(0.62f, 0.14f, 0.2f, 1f);
    static readonly Color Name = new Color(0.86f, 0.72f, 0.45f, 1f);

    void Awake()
    {
        if (health == null)
            health = GetComponent<Damageable>();
        Build();
    }

    void OnEnable()
    {
        if (health != null)
            health.HealthChanged += OnHealthChanged;
    }

    void Start()
    {
        if (health != null)
            Apply(health.CurrentHealth, health.MaxHealth);
    }

    void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= OnHealthChanged;
    }

    void LateUpdate()
    {
        if (barRoot == null || dead)
            return;

        var camera = Camera.main;
        if (camera == null)
            return;

        Vector3 toCamera = barRoot.position - camera.transform.position;
        toCamera.y = 0f;
        if (toCamera.sqrMagnitude < 0.0001f)
            return;

        barRoot.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
    }

    void OnHealthChanged(float current, float max)
    {
        Apply(current, max);
    }

    void Apply(float current, float max)
    {
        if (fill != null)
            fill.fillAmount = max <= 0f ? 0f : Mathf.Clamp01(current / max);

        if (current <= 0f)
            Hide();
    }

    void Hide()
    {
        dead = true;
        if (barRoot != null)
            barRoot.gameObject.SetActive(false);
    }

    void Build()
    {
        if (barRoot != null)
            return;

        var root = new GameObject("EnemyHealthBar");
        root.transform.SetParent(transform, false);
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 5;
        barRoot = root.transform;
        barRoot.localPosition = new Vector3(0f, height, 0f);
        barRoot.localRotation = Quaternion.identity;
        barRoot.localScale = Vector3.one * 0.005f;

        var rect = root.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(180f, 36f);

        var frame = CreateImage("Frame", root.transform, Frame);
        Stretch(frame.rectTransform, 0f);
        var plate = CreateImage("Plate", root.transform, Plate);
        Stretch(plate.rectTransform, 3f);
        fill = CreateImage("Fill", plate.transform, Fill);
        Stretch(fill.rectTransform, 2f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;

        var labelObject = new GameObject("Name");
        labelObject.transform.SetParent(root.transform, false);
        nameLabel = labelObject.AddComponent<Text>();
        nameLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nameLabel.text = displayName;
        nameLabel.alignment = TextAnchor.MiddleCenter;
        nameLabel.color = Name;
        nameLabel.fontSize = 14;
        nameLabel.raycastTarget = false;
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 0f);
        labelRect.sizeDelta = new Vector2(0f, 16f);
        labelRect.anchoredPosition = new Vector2(0f, 2f);
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        var imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.AddComponent<Image>();
        image.sprite = White();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect, float inset)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(inset, inset);
        rect.offsetMax = new Vector2(-inset, -inset);
    }

    static Sprite White()
    {
        if (quad != null)
            return quad;

        var texture = Texture2D.whiteTexture;
        quad = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return quad;
    }
}
