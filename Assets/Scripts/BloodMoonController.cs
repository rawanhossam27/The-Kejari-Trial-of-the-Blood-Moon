using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Awakens the Blood Moon after the arena entry finishes.
/// Night lighting stays as authored until <see cref="ActivateBloodMoon"/> runs.
/// </summary>
public class BloodMoonController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float duration = 12f;

    [Header("Moon")]
    [SerializeField] Renderer moonDisc;
    [SerializeField] Renderer moonHalo;
    [SerializeField] Color awakenedDiscColor = new Color(1.22f, 0.42f, 0.32f, 1f);
    [SerializeField] Color awakenedHaloColor = new Color(0.82f, 0.24f, 0.18f, 0.34f);
    [SerializeField, Min(1f)] float haloScaleMultiplier = 1.05f;

    [Header("Directional Light")]
    [SerializeField] Light directionalLight;
    [SerializeField] Color awakenedLightColor = new Color(0.76f, 0.7f, 0.88f, 1f);
    [SerializeField, Min(0f)] float awakenedLightIntensity = 1.72f;

    [Header("Fog")]
    [SerializeField] Color awakenedFogColor = new Color(0.09f, 0.078f, 0.12f, 1f);
    [SerializeField, Min(0f)] float awakenedFogDensity = 0.0049f;

    [Header("Ambient")]
    [SerializeField, Min(0f)] float awakenedAmbientIntensity = 1.32f;

    [Header("Post")]
    [SerializeField] Color colorFilter = new Color(1f, 0.96f, 0.97f, 1f);
    [SerializeField] float hueShift = 12f;
    [SerializeField] float postExposure = -0.05f;
    [SerializeField] float contrast = 2f;
    [SerializeField] float saturation = -4f;
    [SerializeField, Min(0f)] float bloomIntensity = 0.4f;
    [SerializeField] Color bloomTint = new Color(1f, 0.9f, 0.92f, 1f);
    [SerializeField, Min(0f)] float bloomThreshold = 0.74f;
    [SerializeField, Range(0f, 1f)] float vignetteIntensity = 0.24f;
    [SerializeField] Color vignetteColor = new Color(0.04f, 0.02f, 0.035f, 1f);

    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");

    MaterialPropertyBlock discBlock;
    MaterialPropertyBlock haloBlock;
    Volume overlayVolume;
    Color discFrom;
    Color haloFrom;
    Vector3 haloScaleFrom;
    Color lightFrom;
    float lightIntensityFrom;
    LightShadows shadowMode;
    float shadowStrength;
    Color fogFrom;
    float fogDensityFrom;
    float ambientFrom;
    float elapsed;
    bool running;
    bool started;

    public bool HasStarted => started;
    public float Progress => !started ? 0f : Mathf.Clamp01(elapsed / Mathf.Max(0.1f, duration));

    void Awake()
    {
        ResolveReferences();
        CaptureBaselines();
        CreateOverlayVolume();
    }

    void Update()
    {
        if (!running)
            return;

        elapsed += Time.deltaTime;
        float u = Ease(elapsed / duration);
        Apply(u);
        if (elapsed >= duration)
        {
            Apply(1f);
            running = false;
        }
    }

    public void ActivateBloodMoon()
    {
        if (started)
            return;

        started = true;
        running = true;
        elapsed = 0f;
        Apply(0f);
    }

    void ResolveReferences()
    {
        if (moonDisc == null)
        {
            var disc = transform.Find("Moon_Disc");
            if (disc != null)
                moonDisc = disc.GetComponent<Renderer>();
        }

        if (moonHalo == null)
        {
            var halo = transform.Find("Moon_Halo");
            if (halo != null)
                moonHalo = halo.GetComponent<Renderer>();
        }

        if (directionalLight == null)
        {
            var lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null && lights[i].type == LightType.Directional)
                {
                    directionalLight = lights[i];
                    break;
                }
            }
        }
    }

    void CaptureBaselines()
    {
        discBlock = new MaterialPropertyBlock();
        haloBlock = new MaterialPropertyBlock();

        if (moonDisc != null && moonDisc.sharedMaterial != null)
            discFrom = moonDisc.sharedMaterial.GetColor(BaseColorId);
        if (moonHalo != null)
        {
            haloScaleFrom = moonHalo.transform.localScale;
            if (moonHalo.sharedMaterial != null)
                haloFrom = moonHalo.sharedMaterial.GetColor(BaseColorId);
        }

        if (directionalLight != null)
        {
            lightFrom = directionalLight.color;
            lightIntensityFrom = directionalLight.intensity;
            shadowMode = directionalLight.shadows;
            shadowStrength = directionalLight.shadowStrength;
        }

        fogFrom = RenderSettings.fogColor;
        fogDensityFrom = RenderSettings.fogDensity;
        ambientFrom = RenderSettings.ambientIntensity;
    }

    void CreateOverlayVolume()
    {
        var volumeObject = new GameObject("BloodMoonVolume");
        volumeObject.transform.SetParent(transform, false);
        volumeObject.hideFlags = HideFlags.DontSave;

        overlayVolume = volumeObject.AddComponent<Volume>();
        overlayVolume.isGlobal = true;
        overlayVolume.priority = 10f;
        overlayVolume.weight = 0f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        profile.hideFlags = HideFlags.DontSave;
        overlayVolume.sharedProfile = profile;

        var color = profile.Add<ColorAdjustments>();
        color.colorFilter.Override(colorFilter);
        color.hueShift.Override(hueShift);
        color.postExposure.Override(postExposure);
        color.contrast.Override(contrast);
        color.saturation.Override(saturation);

        var bloom = profile.Add<Bloom>();
        bloom.threshold.Override(bloomThreshold);
        bloom.intensity.Override(bloomIntensity);
        bloom.tint.Override(bloomTint);

        var vignette = profile.Add<Vignette>();
        vignette.color.Override(vignetteColor);
        vignette.intensity.Override(vignetteIntensity);
        vignette.smoothness.Override(0.3f);
    }

    static float Ease(float amount)
    {
        float t = Mathf.Clamp01(amount);
        return t * t * (3f - 2f * t);
    }

    void Apply(float amount)
    {
        SetRendererColor(moonDisc, discBlock, Color.Lerp(discFrom, awakenedDiscColor, amount));

        if (moonHalo != null)
        {
            SetRendererColor(moonHalo, haloBlock, Color.Lerp(haloFrom, awakenedHaloColor, amount));
            moonHalo.transform.localScale = Vector3.Lerp(haloScaleFrom, haloScaleFrom * haloScaleMultiplier, amount);
        }

        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(lightFrom, awakenedLightColor, amount);
            directionalLight.intensity = Mathf.Lerp(lightIntensityFrom, awakenedLightIntensity, amount);
            directionalLight.shadows = shadowMode;
            directionalLight.shadowStrength = shadowStrength;
        }

        RenderSettings.fogColor = Color.Lerp(fogFrom, awakenedFogColor, amount);
        RenderSettings.fogDensity = Mathf.Lerp(fogDensityFrom, awakenedFogDensity, amount);
        RenderSettings.ambientIntensity = Mathf.Lerp(ambientFrom, awakenedAmbientIntensity, amount);

        if (overlayVolume != null)
            overlayVolume.weight = amount;
    }

    static void SetRendererColor(Renderer renderer, MaterialPropertyBlock block, Color color)
    {
        if (renderer == null || block == null)
            return;

        renderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        block.SetColor(ColorId, color);
        renderer.SetPropertyBlock(block);
    }
}
