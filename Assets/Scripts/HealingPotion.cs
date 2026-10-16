using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One carried healing potion. H drinks it for a capped heal and a short red glow.
/// </summary>
public class HealingPotion : MonoBehaviour
{
    [SerializeField] PlayerHealth health;
    [SerializeField] OrayaMovement movement;
    [SerializeField, Min(1f)] float healAmount = 30f;
    [SerializeField, Min(0.1f)] float glowDuration = 0.85f;

    public int Charges { get; private set; }
    public float HealAmount => healAmount;

    public event Action<int> ChargesChanged;

    void Awake()
    {
        if (health == null)
            health = GetComponent<PlayerHealth>();
        if (movement == null)
            movement = GetComponent<OrayaMovement>();
    }

    void Update()
    {
        if (DrinkPressed())
            TryDrink();
    }

    public void AddCharge()
    {
        Charges++;
        ChargesChanged?.Invoke(Charges);
    }

    public bool TryDrink()
    {
        if (Charges <= 0 || health == null || health.IsDead)
            return false;
        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return false;
        if (health.CurrentHealth >= health.MaxHealth - 0.01f)
            return false;

        Charges--;
        health.Heal(healAmount);
        ChargesChanged?.Invoke(Charges);
        SpawnGlow();
        return true;
    }

    void SpawnGlow()
    {
        var glow = new GameObject("HealGlow");
        glow.transform.SetParent(transform, false);
        glow.transform.localPosition = new Vector3(0f, 1.15f, 0f);

        var light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.38f, 0.62f);
        light.range = 2.6f;
        light.intensity = 1.7f;
        light.shadows = LightShadows.None;

        var particles = glow.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.35f;
        main.startLifetime = 0.55f;
        main.startSpeed = 0.45f;
        main.startSize = 0.07f;
        main.startColor = new Color(1f, 0.42f, 0.68f, 0.85f);
        main.maxParticles = 24;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 14) });
        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;
        var renderer = glow.GetComponent<ParticleSystemRenderer>();
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader != null)
        {
            var material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", new Color(1f, 0.4f, 0.66f, 0.8f));
            renderer.material = material;
        }
        particles.Play();

        var fade = glow.AddComponent<HealGlowFade>();
        fade.Begin(light, renderer.material, glowDuration);
    }

    static bool DrinkPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.hKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}

sealed class HealGlowFade : MonoBehaviour
{
    Light glow;
    Material particleMaterial;
    float duration;
    float age;

    public void Begin(Light point, Material material, float seconds)
    {
        glow = point;
        particleMaterial = material;
        duration = Mathf.Max(0.1f, seconds);
    }

    void Update()
    {
        age += Time.deltaTime;
        float fade = 1f - Mathf.Clamp01(age / duration);
        if (glow != null)
            glow.intensity = 1.7f * fade;
        if (age < duration)
            return;

        if (particleMaterial != null)
            Destroy(particleMaterial);
        Destroy(gameObject);
    }
}
