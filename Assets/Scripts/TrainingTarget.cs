using UnityEngine;

/// <summary>
/// Arena practice target. Flashes when hit and collapses when its health is gone.
/// </summary>
[RequireComponent(typeof(Damageable))]
public class TrainingTarget : MonoBehaviour
{
    [SerializeField] Renderer body;
    [SerializeField, Min(0.01f)] float flashDuration = 0.12f;
    [SerializeField] Color flashColor = new Color(0.75f, 0.9f, 1f, 1f);
    [SerializeField, Min(0f)] float collapseDelay = 1.15f;

    MaterialPropertyBlock block;
    Color restingColor;
    float flashUntil;
    float collapseAt;
    bool collapsing;
    bool retired;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        if (body == null)
            body = GetComponent<Renderer>();
        if (body != null && body.sharedMaterial != null && body.sharedMaterial.HasProperty("_BaseColor"))
            restingColor = body.sharedMaterial.GetColor("_BaseColor");

        var health = GetComponent<Damageable>();
        health.Hit += OnHit;
        health.Defeated += OnDefeated;
    }

    void Update()
    {
        if (body != null && Time.time >= flashUntil && !retired)
            SetColor(restingColor);

        if (!collapsing)
            return;

        float fall = Mathf.Clamp01((Time.time - (collapseAt - 0.45f)) / 0.45f);
        transform.rotation = Quaternion.Euler(Mathf.Lerp(0f, 80f, fall), transform.eulerAngles.y, 0f);
        if (Time.time >= collapseAt)
        {
            retired = true;
            gameObject.SetActive(false);
        }
    }

    void OnHit()
    {
        flashUntil = Time.time + flashDuration;
        SetColor(flashColor);
    }

    void OnDefeated()
    {
        collapsing = true;
        collapseAt = Time.time + collapseDelay;
        var collider = GetComponent<Collider>();
        if (collider != null)
            collider.enabled = false;
        SetColor(new Color(0.35f, 0.4f, 0.48f, 1f));
    }

    void SetColor(Color color)
    {
        if (body == null)
            return;

        body.GetPropertyBlock(block);
        block.SetColor("_BaseColor", color);
        block.SetColor("_Color", color);
        body.SetPropertyBlock(block);
    }
}
