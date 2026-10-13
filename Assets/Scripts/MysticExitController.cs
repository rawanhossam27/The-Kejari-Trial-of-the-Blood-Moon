using UnityEngine;

/// <summary>
/// Wakes the existing mystic exit after the Blood Moon finishes.
/// Until then the exit stays dim and its particles stay quiet.
/// </summary>
[DefaultExecutionOrder(100)]
public class MysticExitController : MonoBehaviour
{
    [SerializeField, Min(0.1f)] float activationDuration = 4.5f;
    [SerializeField, Min(0f)] float targetLightIntensity = 1f;
    [SerializeField, Min(0f)] float pulseSpeed = 0.4f;
    [SerializeField, Range(0f, 0.45f)] float pulseStrength = 0.14f;
    [SerializeField, Min(0f)] float ringGlow = 1.65f;

    [Header("Existing Exit")]
    [SerializeField] Light[] lights;
    [SerializeField] Renderer[] runeRenderers;
    [SerializeField] Renderer glowVolume;
    [SerializeField] ParticleSystem[] particles;
    [SerializeField] Renderer[] gateRings;

    const float DormantScale = 0.05f;

    static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    MaterialPropertyBlock block;
    float[] lightAwakened;
    Color[] runeAwakened;
    Color[] ringAwakened;
    Color glowAwakened;
    float elapsed;
    bool rising;
    bool awakened;
    bool captured;

    public bool IsAwakened => awakened;
    public bool IsRising => rising;
    public float Progress => !awakened ? 0f : rising ? Mathf.Clamp01(elapsed / activationDuration) : 1f;

    void Awake()
    {
        block = new MaterialPropertyBlock();
        ResolveReferences();
        Capture();
        ApplyDormant();
    }

    void Start()
    {
        if (!awakened)
            QuietParticles();
    }

    void OnEnable()
    {
        if (!awakened)
            ApplyDormant();
    }

    void Update()
    {
        if (!awakened)
            return;

        float pulse = 1f;
        float level = 1f;
        if (rising)
        {
            elapsed += Time.deltaTime;
            level = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / activationDuration));
            if (elapsed >= activationDuration)
                rising = false;
        }
        else
        {
            pulse = 1f + Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f) * pulseStrength;
        }

        ApplyLevel(level, pulse);
    }

    public void ActivateExit()
    {
        if (awakened)
            return;

        Capture();
        awakened = true;
        rising = true;
        elapsed = 0f;
        ApplyLevel(0f, 1f);
        PlayParticles();
    }

    void ResolveReferences()
    {
        if (lights == null || lights.Length == 0)
            lights = GetComponentsInChildren<Light>(true);

        if (runeRenderers == null || runeRenderers.Length == 0)
        {
            var sigil = transform.Find("Rune_Sigil");
            if (sigil != null)
                runeRenderers = sigil.GetComponentsInChildren<Renderer>(true);
        }

        if (glowVolume == null)
        {
            var glow = transform.Find("Glow_Behind");
            if (glow != null)
                glowVolume = glow.GetComponent<Renderer>();
        }

        if (particles == null || particles.Length == 0)
            particles = GetComponentsInChildren<ParticleSystem>(true);

        if (gateRings == null || gateRings.Length == 0)
            gateRings = FindGateRings();
    }

    Renderer[] FindGateRings()
    {
        var gate = transform.parent;
        if (gate == null)
            return new Renderer[0];

        var renderers = gate.GetComponentsInChildren<Renderer>(true);
        int count = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (IsGateRing(renderers[i]))
                count++;
        }

        var rings = new Renderer[count];
        int index = 0;
        for (int i = 0; i < renderers.Length; i++)
        {
            if (!IsGateRing(renderers[i]))
                continue;
            rings[index] = renderers[i];
            index++;
        }

        return rings;
    }

    static bool IsGateRing(Renderer renderer)
    {
        if (renderer == null)
            return false;
        var name = renderer.name;
        return name.StartsWith("Ring");
    }

    void Capture()
    {
        if (captured)
            return;

        captured = true;
        lightAwakened = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            lightAwakened[i] = lights[i] != null ? lights[i].intensity : 0f;

        runeAwakened = new Color[runeRenderers.Length];
        for (int i = 0; i < runeRenderers.Length; i++)
        {
            var renderer = runeRenderers[i];
            runeAwakened[i] = renderer != null && renderer.sharedMaterial != null
                ? renderer.sharedMaterial.GetColor(EmissionId)
                : Color.black;
        }

        if (glowVolume != null && glowVolume.sharedMaterial != null)
            glowAwakened = glowVolume.sharedMaterial.GetColor(BaseColorId);

        ringAwakened = new Color[gateRings.Length];
        for (int i = 0; i < gateRings.Length; i++)
        {
            var renderer = gateRings[i];
            ringAwakened[i] = renderer != null && renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(EmissionId)
                ? renderer.sharedMaterial.GetColor(EmissionId)
                : Color.black;
        }
    }

    void ApplyDormant()
    {
        if (!captured)
            return;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
                continue;
            lights[i].enabled = true;
            lights[i].intensity = lightAwakened[i] * DormantScale;
        }

        for (int i = 0; i < runeRenderers.Length; i++)
            SetEmission(runeRenderers[i], runeAwakened[i] * DormantScale);

        for (int i = 0; i < gateRings.Length; i++)
            SetEmission(gateRings[i], ringAwakened[i] * DormantScale);

        SetGlow(Color.Lerp(Color.clear, glowAwakened, DormantScale));
        QuietParticles();
    }

    void ApplyLevel(float level, float pulse)
    {
        float scale = targetLightIntensity * pulse;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] == null)
                continue;
            float awakenedIntensity = lightAwakened[i] * scale;
            lights[i].intensity = Mathf.Lerp(lightAwakened[i] * DormantScale, awakenedIntensity, level);
        }

        for (int i = 0; i < runeRenderers.Length; i++)
        {
            Color awakenedColor = runeAwakened[i] * pulse;
            SetEmission(runeRenderers[i], Color.Lerp(runeAwakened[i] * DormantScale, awakenedColor, level));
        }

        for (int i = 0; i < gateRings.Length; i++)
        {
            Color awakenedColor = ringAwakened[i] * ringGlow * pulse;
            SetEmission(gateRings[i], Color.Lerp(ringAwakened[i] * DormantScale, awakenedColor, level));
        }

        Color glow = Color.Lerp(glowAwakened * DormantScale, glowAwakened * pulse, level);
        glow.a = Mathf.Lerp(glowAwakened.a * DormantScale, glowAwakened.a * pulse, level);
        SetGlow(glow);
    }

    void PlayParticles()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var system = particles[i];
            if (system == null)
                continue;
            var emission = system.emission;
            emission.enabled = true;
            system.Play(true);
        }
    }

    void QuietParticles()
    {
        for (int i = 0; i < particles.Length; i++)
        {
            var system = particles[i];
            if (system == null)
                continue;
            var emission = system.emission;
            emission.enabled = false;
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    void SetEmission(Renderer renderer, Color color)
    {
        if (renderer == null)
            return;

        renderer.GetPropertyBlock(block);
        block.SetColor(EmissionId, color);
        renderer.SetPropertyBlock(block);
    }

    void SetGlow(Color color)
    {
        if (glowVolume == null)
            return;

        glowVolume.GetPropertyBlock(block);
        block.SetColor(BaseColorId, color);
        glowVolume.SetPropertyBlock(block);
    }
}
