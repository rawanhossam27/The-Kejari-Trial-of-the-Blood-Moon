using UnityEngine;

/// <summary>
/// Straight purple bolt. A sphere cast each step keeps it from tunneling through Oraya.
/// Only <see cref="PlayerHealth"/> is damaged, and only once.
/// </summary>
public class MagicProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float hitRadius = 0.18f;
    [SerializeField] GameObject impactPrefab;
    [SerializeField, Min(0.2f)] float impactLifetime = 1.6f;

    Vector3 direction = Vector3.forward;
    float speed = 10f;
    float damage = 15f;
    float lifetime = 4f;
    Transform owner;
    float age;
    bool spent;

    public bool HasHit => spent;

    public void Launch(Vector3 flightDirection, float flightSpeed, float hitDamage, float life, Transform ignoreRoot)
    {
        direction = flightDirection.sqrMagnitude > 0.0001f ? flightDirection.normalized : Vector3.forward;
        speed = Mathf.Max(0.1f, flightSpeed);
        damage = hitDamage;
        lifetime = Mathf.Max(0.2f, life);
        owner = ignoreRoot;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
    }

    void Update()
    {
        if (spent)
            return;

        float step = speed * Time.deltaTime;
        if (step > 0.001f && TryHit(step))
            return;

        transform.position += direction * step;
        age += Time.deltaTime;
        if (age >= lifetime)
            Destroy(gameObject);
    }

    bool TryHit(float step)
    {
        Vector3 origin = transform.position;
        float remaining = step;
        for (int i = 0; i < 4 && remaining > 0.001f; i++)
        {
            if (!Physics.SphereCast(origin, hitRadius, direction, out RaycastHit hit, remaining, ~0, QueryTriggerInteraction.Ignore))
                return false;

            if (IsOwner(hit.collider))
            {
                float skip = hit.distance + 0.05f;
                origin += direction * skip;
                remaining -= skip;
                continue;
            }

            spent = true;
            if (!IsOwnerDefeated())
            {
                var health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                    health.TakeDamage(damage);
            }

            SpawnImpact(hit.point);
            Destroy(gameObject);
            return true;
        }

        return false;
    }

    void SpawnImpact(Vector3 point)
    {
        if (impactPrefab == null)
            return;

        var fx = Instantiate(impactPrefab, point, Quaternion.identity);
        var root = fx.GetComponent<ParticleSystem>();
        if (root != null)
        {
            var main = root.main;
            main.loop = false;
            var emission = root.emission;
            emission.enabled = false;
            root.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        Destroy(fx, impactLifetime);
    }

    public static void DisarmFrom(Transform ownerRoot)
    {
        if (ownerRoot == null)
            return;

        var bolts = Object.FindObjectsByType<MagicProjectile>(FindObjectsSortMode.None);
        for (int i = 0; i < bolts.Length; i++)
        {
            var bolt = bolts[i];
            if (bolt == null || bolt.owner == null)
                continue;
            if (bolt.owner == ownerRoot || bolt.owner.IsChildOf(ownerRoot))
                bolt.Disarm();
        }
    }

    public void Disarm()
    {
        damage = 0f;
        if (spent)
            return;

        spent = true;
        Destroy(gameObject);
    }

    bool IsOwnerDefeated()
    {
        if (owner == null)
            return false;

        var health = owner.GetComponent<Damageable>();
        if (health == null)
            health = owner.GetComponentInParent<Damageable>();
        return health != null && health.IsDefeated;
    }

    bool IsOwner(Collider collider)
    {
        if (owner == null || collider == null)
            return false;

        Transform hitTransform = collider.transform;
        return hitTransform == owner || hitTransform.IsChildOf(owner);
    }
}
