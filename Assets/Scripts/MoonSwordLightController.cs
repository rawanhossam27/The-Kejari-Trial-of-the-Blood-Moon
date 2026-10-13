using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Toggles the existing MoonSword point light with the F key.
/// The light stays parented to the sword, so it follows Oraya.
/// </summary>
public class MoonSwordLightController : MonoBehaviour
{
    [SerializeField] Light swordLight;
    [SerializeField] bool startEnabled = true;
    [SerializeField, Min(0f)] float fadeDuration = 0.15f;
    [Tooltip("Glow particles on this sword. The blade mesh is not changed.")]
    [SerializeField] ParticleSystem glowParticles;
    [SerializeField] bool toggleGlow = true;

    float onIntensity = 1f;
    float targetIntensity = 1f;
    float fadeVelocity;
    bool lit;

    public bool IsLit => lit;

    void Awake()
    {
        if (swordLight == null)
            swordLight = GetComponent<Light>();
        if (glowParticles == null && toggleGlow)
            glowParticles = GetComponentInParent<ParticleSystem>();

        if (swordLight != null && swordLight.intensity > 0.0001f)
            onIntensity = swordLight.intensity;

        ApplyImmediate(startEnabled);
    }

    void Update()
    {
        if (TogglePressed())
            SetLit(!lit);

        FadeLight();
    }

    public void SetLit(bool enabled)
    {
        lit = enabled;
        targetIntensity = enabled ? onIntensity : 0f;

        if (swordLight != null)
        {
            if (enabled)
            {
                swordLight.enabled = true;
                if (fadeDuration <= 0f)
                    swordLight.intensity = onIntensity;
            }
            else if (fadeDuration <= 0f)
            {
                swordLight.intensity = 0f;
                swordLight.enabled = false;
            }
        }

        ApplyGlow(enabled);
    }

    void ApplyImmediate(bool enabled)
    {
        fadeVelocity = 0f;
        lit = enabled;
        targetIntensity = enabled ? onIntensity : 0f;

        if (swordLight != null)
        {
            swordLight.enabled = enabled;
            swordLight.intensity = enabled ? onIntensity : 0f;
        }

        ApplyGlow(enabled);
    }

    void FadeLight()
    {
        if (swordLight == null || fadeDuration <= 0f)
            return;
        if (!swordLight.enabled && !lit)
            return;

        swordLight.intensity = Mathf.SmoothDamp(
            swordLight.intensity, targetIntensity, ref fadeVelocity, fadeDuration);

        if (!lit && swordLight.intensity <= 0.02f)
        {
            swordLight.intensity = 0f;
            swordLight.enabled = false;
            fadeVelocity = 0f;
        }
    }

    void ApplyGlow(bool enabled)
    {
        if (!toggleGlow || glowParticles == null)
            return;

        var renderer = glowParticles.GetComponent<ParticleSystemRenderer>();
        if (enabled)
        {
            if (renderer != null)
                renderer.enabled = true;
            if (!glowParticles.isPlaying)
                glowParticles.Play(true);
        }
        else
        {
            glowParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            if (renderer != null)
                renderer.enabled = false;
        }
    }

    static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
