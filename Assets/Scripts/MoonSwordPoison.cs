using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// One poison charge, then a timed blade coating that multiplies sword damage.
/// The imported Moon Sword material is never edited.
/// </summary>
public class MoonSwordPoison : MonoBehaviour
{
    [SerializeField] OrayaMovement movement;
    [SerializeField] Transform sword;
    [SerializeField, Min(0.1f)] float duration = 30f;
    [SerializeField, Min(1f)] float damageMultiplier = 1.5f;
    [SerializeField] float bladeMinLocalY = 0.32f;

    static readonly Color CoatColor = new Color(0.08f, 0.72f, 0.32f, 0.62f);
    static readonly Color CoatEmission = new Color(0.05f, 1.15f, 0.38f, 1f);

    GameObject coatObject;
    MeshRenderer coatRenderer;
    Material coatMaterial;
    Color baseColor;
    bool expiring;

    public int Charges { get; private set; }
    public bool IsActive { get; private set; }
    public float Remaining => Mathf.Max(0f, remaining);
    public float Duration => duration;
    public float DamageMultiplier => damageMultiplier;

    float remaining;
    float fade;

    public event Action<int> ChargesChanged;
    public event Action PoisonStateChanged;

    void Awake()
    {
        if (movement == null)
            movement = GetComponent<OrayaMovement>();
        if (sword == null)
        {
            var found = transform.Find("root/pelvis/spine_01/spine_02/spine_03/spine_04/spine_05/clavicle_r/upperarm_r/lowerarm_r/hand_r/weapon_r/MoonSword");
            if (found != null)
                sword = found;
        }
    }

    void Update()
    {
        if (ActivatePressed())
            TryActivate();

        if (IsActive)
        {
            remaining -= Time.deltaTime;
            fade = Mathf.MoveTowards(fade, 1f, Time.deltaTime / 0.35f);
            ApplyCoatVisual();
            if (remaining <= 0f)
                EndPoison();
            return;
        }

        if (!expiring)
            return;

        fade = Mathf.MoveTowards(fade, 0f, Time.deltaTime / 0.6f);
        ApplyCoatVisual();
        if (fade <= 0.001f)
        {
            expiring = false;
            if (coatObject != null)
                coatObject.SetActive(false);
        }
    }

    public void AddCharge()
    {
        Charges++;
        ChargesChanged?.Invoke(Charges);
    }

    public bool TryActivate()
    {
        if (IsActive || expiring || Charges <= 0)
            return false;
        if (movement != null && (!movement.InputEnabled || movement.CinematicMoveActive))
            return false;

        Charges--;
        ChargesChanged?.Invoke(Charges);
        IsActive = true;
        remaining = duration;
        fade = 0f;
        expiring = false;
        EnsureCoat();
        if (coatObject != null)
            coatObject.SetActive(true);
        ApplyCoatVisual();
        PoisonStateChanged?.Invoke();
        return true;
    }

    void EndPoison()
    {
        IsActive = false;
        remaining = 0f;
        expiring = true;
        PoisonStateChanged?.Invoke();
    }

    static bool ActivatePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard.qKey.wasPressedThisFrame;
#else
        return false;
#endif
    }

    void EnsureCoat()
    {
        if (sword == null || coatObject != null)
            return;

        var sourceFilter = sword.GetComponent<MeshFilter>();
        if (sourceFilter == null || sourceFilter.sharedMesh == null)
            return;

        var mesh = BuildBladeMesh(sourceFilter.sharedMesh, bladeMinLocalY);
        coatMaterial = CreateCoatMaterial();
        baseColor = CoatColor;

        coatObject = new GameObject("PoisonCoat");
        coatObject.hideFlags = HideFlags.DontSave;
        coatObject.transform.SetParent(sword, false);
        coatObject.AddComponent<MeshFilter>().sharedMesh = mesh;
        coatRenderer = coatObject.AddComponent<MeshRenderer>();
        coatRenderer.sharedMaterial = coatMaterial;
        coatRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        coatRenderer.receiveShadows = false;
        coatObject.SetActive(false);
    }

    void ApplyCoatVisual()
    {
        if (coatMaterial == null)
            return;

        float pulse = 0.82f + Mathf.Sin(Time.time * 2.6f) * 0.18f;
        var color = baseColor;
        color.a *= fade * pulse;
        coatMaterial.SetColor("_BaseColor", color);
        coatMaterial.SetColor("_EmissionColor", CoatEmission * (fade * pulse));
    }

    static Material CreateCoatMaterial()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        var material = new Material(shader);
        material.name = "MoonSwordPoisonCoat";
        material.hideFlags = HideFlags.DontSave;
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_EMISSION");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.SetColor("_BaseColor", CoatColor);
        material.SetColor("_EmissionColor", CoatEmission);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        return material;
    }

    static Mesh BuildBladeMesh(Mesh source, float minY)
    {
        var vertices = source.vertices;
        var normals = source.normals;
        var triangles = source.triangles;
        var mesh = new Mesh { name = "MoonSwordBladeCoat" };
        mesh.hideFlags = HideFlags.DontSave;

        var newVertices = new List<Vector3>();
        var newNormals = new List<Vector3>();
        var newTriangles = new List<int>();
        var remap = new Dictionary<int, int>();

        int Add(int index)
        {
            if (remap.TryGetValue(index, out int existing))
                return existing;

            Vector3 normal = normals != null && index < normals.Length ? normals[index] : Vector3.up;
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector3.up;
            int added = newVertices.Count;
            newVertices.Add(vertices[index] + normal.normalized * 0.004f);
            newNormals.Add(normal);
            remap.Add(index, added);
            return added;
        }

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i];
            int b = triangles[i + 1];
            int c = triangles[i + 2];
            if (vertices[a].y < minY || vertices[b].y < minY || vertices[c].y < minY)
                continue;

            newTriangles.Add(Add(a));
            newTriangles.Add(Add(b));
            newTriangles.Add(Add(c));
        }

        if (newTriangles.Count == 0)
        {
            newVertices.Add(new Vector3(-0.035f, minY, 0.008f));
            newVertices.Add(new Vector3(0.035f, minY, 0.008f));
            newVertices.Add(new Vector3(0.02f, 1.5f, 0.008f));
            newVertices.Add(new Vector3(-0.02f, 1.5f, 0.008f));
            newNormals.Add(Vector3.forward);
            newNormals.Add(Vector3.forward);
            newNormals.Add(Vector3.forward);
            newNormals.Add(Vector3.forward);
            newTriangles.AddRange(new[] { 0, 1, 2, 0, 2, 3 });
        }

        mesh.SetVertices(newVertices);
        mesh.SetNormals(newNormals);
        mesh.SetTriangles(newTriangles, 0);
        mesh.RecalculateBounds();
        return mesh;
    }
}
