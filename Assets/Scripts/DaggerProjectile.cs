using UnityEngine;

/// <summary>
/// Straight dagger flight. A sphere cast each step keeps it from tunneling through targets.
/// </summary>
public class DaggerProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] float hitRadius = 0.05f;
    [SerializeField, Min(0f)] float stickTime = 0.8f;
    [SerializeField, Min(0f)] float minTravel = 0.35f;
    [SerializeField] float tipLocalDistance = 0.93f;

    Vector3 direction = Vector3.forward;
    float speed = 22f;
    float damage = 1f;
    float lifetime = 2.5f;
    Transform owner;
    float age;
    float flown;
    bool stuck;
    bool spent;

    public void Launch(Vector3 flightDirection, float flightSpeed, float hitDamage, float life, Transform ignoreRoot)
    {
        direction = flightDirection.sqrMagnitude > 0.0001f ? flightDirection.normalized : Vector3.forward;
        speed = flightSpeed;
        damage = hitDamage;
        lifetime = life;
        owner = ignoreRoot;
        transform.rotation = BladeTipAlong(direction);
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
            renderer.enabled = true;
    }

    /// <summary>
    /// RubyDagger's mesh is longest on local +Y, and that end is the thin tip.
    /// Velocity stays on <paramref name="flightDirection"/>; only the mesh is rotated.
    /// </summary>
    public static Quaternion BladeTipAlong(Vector3 flightDirection)
    {
        Vector3 tip = flightDirection.normalized;
        Vector3 bladeFlat = Vector3.Cross(Vector3.Cross(tip, Vector3.up), tip);
        if (bladeFlat.sqrMagnitude < 0.0001f)
            bladeFlat = Vector3.forward;
        bladeFlat.Normalize();
        return Quaternion.LookRotation(bladeFlat, tip);
    }

    void Update()
    {
        if (stuck)
        {
            age += Time.deltaTime;
            if (age >= stickTime)
                Destroy(gameObject);
            return;
        }

        float step = speed * Time.deltaTime;
        float clear = Mathf.Max(0f, minTravel - flown);
        if (clear > 0f)
        {
            float hop = Mathf.Min(step, clear);
            transform.position += direction * hop;
            flown += hop;
            step -= hop;
        }

        if (step > 0.001f && TryHit(step))
            return;

        if (step > 0.001f)
        {
            transform.position += direction * step;
            flown += step;
        }

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
                float skip = hit.distance + hitRadius;
                origin += direction * skip;
                remaining -= skip;
                continue;
            }

            if (!spent)
            {
                spent = true;
                var target = hit.collider.GetComponentInParent<Damageable>();
                if (target != null)
                    target.TakeDamage(damage);
            }

            float tip = tipLocalDistance * Mathf.Abs(transform.lossyScale.y);
            transform.position = hit.point + hit.normal * 0.04f - direction * (tip * 0.35f);
            stuck = true;
            age = 0f;
            return true;
        }

        return false;
    }

    bool IsOwner(Collider collider)
    {
        if (owner == null || collider == null)
            return false;

        Transform hitTransform = collider.transform;
        return hitTransform == owner || hitTransform.IsChildOf(owner);
    }
}
