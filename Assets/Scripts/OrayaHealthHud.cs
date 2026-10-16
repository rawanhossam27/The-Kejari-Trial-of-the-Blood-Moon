using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Upper-left health plate for Oraya. Listens to <see cref="PlayerHealth"/> and stays hidden until the entry cinematic finishes.
/// </summary>
public class OrayaHealthHud : MonoBehaviour
{
    [SerializeField] PlayerHealth health;
    [SerializeField] EntrySequenceController entry;
    [SerializeField] CanvasGroup group;
    [SerializeField] Image fill;
    [SerializeField] Image trail;
    [SerializeField] Image flash;
    [SerializeField] Image emblemGlow;
    [SerializeField] TMP_Text nameLabel;
    [SerializeField] TMP_Text hpLabel;

    [SerializeField, Min(0.05f)] float fillSmoothTime = 0.22f;
    [SerializeField, Min(0f)] float trailDelay = 0.32f;
    [SerializeField, Min(0.05f)] float trailSmoothTime = 0.55f;
    [SerializeField, Range(0.05f, 0.9f)] float lowHealthThreshold = 0.25f;
    [SerializeField, Min(0.05f)] float revealDuration = 0.7f;

    static readonly Color GlowColor = new Color(0.561f, 0.420f, 0.667f, 0.42f);

    float targetRatio = 1f;
    float displayed = 1f;
    float trailRatio = 1f;
    float displayVelocity;
    float trailVelocity;
    float trailWait;
    float flashAlpha;
    bool reveal;
    bool bound;

    void Awake()
    {
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
        {
            if (graphic.GetComponentInParent<HealthTestButton>(true) != null)
                continue;
            graphic.raycastTarget = false;
        }

        if (group != null)
        {
            group.blocksRaycasts = false;
            group.interactable = false;
        }
    }

    void OnEnable()
    {
        Bind();
    }

    void Start()
    {
        Bind();
        if (entry == null || entry.IsComplete)
        {
            reveal = true;
            if (group != null)
                group.alpha = 1f;
        }
        else if (group != null)
        {
            group.alpha = 0f;
        }

        if (health != null)
            Apply(health.CurrentHealth, health.MaxHealth, true);
    }

    void OnDisable()
    {
        if (health != null)
            health.HealthChanged -= HandleHealthChanged;
        if (entry != null)
            entry.EntryCompleted -= HandleEntryCompleted;
        bound = false;
    }

    void Update()
    {
        if (group != null && reveal)
            group.alpha = Mathf.MoveTowards(group.alpha, 1f, Time.deltaTime / revealDuration);

        displayed = Mathf.SmoothDamp(displayed, targetRatio, ref displayVelocity, fillSmoothTime);
        if (targetRatio < trailRatio - 0.0001f)
        {
            trailWait -= Time.deltaTime;
            if (trailWait <= 0f)
                trailRatio = Mathf.SmoothDamp(trailRatio, targetRatio, ref trailVelocity, trailSmoothTime);
        }
        else
        {
            trailRatio = targetRatio;
            trailVelocity = 0f;
            trailWait = trailDelay;
        }

        if (fill != null)
            fill.fillAmount = displayed;
        if (trail != null)
            trail.fillAmount = trailRatio;

        flashAlpha = Mathf.MoveTowards(flashAlpha, 0f, Time.deltaTime * 1.35f);
        if (flash != null)
        {
            var color = flash.color;
            color.a = flashAlpha;
            flash.color = color;
        }

        PulseGlow();
    }

    void Bind()
    {
        if (bound)
            return;

        if (health == null)
            health = FindAnyObjectByType<PlayerHealth>();
        if (entry == null)
            entry = FindAnyObjectByType<EntrySequenceController>();
        if (health == null)
            return;

        health.HealthChanged += HandleHealthChanged;
        if (entry != null)
            entry.EntryCompleted += HandleEntryCompleted;
        bound = true;
    }

    void HandleEntryCompleted()
    {
        reveal = true;
    }

    void HandleHealthChanged(float current, float max)
    {
        Apply(current, max, false);
    }

    void Apply(float current, float max, bool immediate)
    {
        float next = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        if (!immediate && next < targetRatio - 0.0001f)
        {
            trailWait = trailDelay;
            flashAlpha = 0.42f;
        }

        targetRatio = next;
        if (next > trailRatio)
            trailRatio = next;

        if (immediate)
        {
            displayed = next;
            trailRatio = next;
            displayVelocity = 0f;
            trailVelocity = 0f;
            flashAlpha = 0f;
            if (fill != null)
                fill.fillAmount = next;
            if (trail != null)
                trail.fillAmount = next;
        }

        if (hpLabel != null)
            hpLabel.text = $"{Mathf.RoundToInt(current)} / {Mathf.RoundToInt(max)}";
        if (nameLabel != null && string.IsNullOrEmpty(nameLabel.text))
            nameLabel.text = "ORAYA";
    }

    void PulseGlow()
    {
        if (emblemGlow == null)
            return;

        bool low = displayed <= lowHealthThreshold;
        float wave = Mathf.Sin(Time.unscaledTime * 2.1f);
        float alpha = low ? 0.34f + (wave * 0.5f + 0.5f) * 0.28f : GlowColor.a;
        float scale = low ? 1f + wave * 0.04f : 1f;
        var color = GlowColor;
        color.a = alpha;
        emblemGlow.color = color;
        emblemGlow.rectTransform.localScale = new Vector3(scale, scale, 1f);
    }
}
