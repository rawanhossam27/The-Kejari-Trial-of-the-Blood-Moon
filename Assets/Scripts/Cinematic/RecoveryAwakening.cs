using System.Collections;
using UnityEngine;

/// <summary>
/// First-person waking sequence on the Recovery Room bed.
/// It starts in darkness and ends holding the open-eyed view. Movement stays off.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-50)]
public class RecoveryAwakening : MonoBehaviour
{
    [SerializeField] Camera view;
    [SerializeField] float breatheDegrees = 1.1f;
    [SerializeField] float breatheMeters = 0.012f;

    Vector3 restPosition;
    Quaternion restRotation;
    float eyeOpen;
    bool breathe;
    bool playing;

    public bool IsPlaying => playing;
    public float EyeOpen => eyeOpen;

    void Awake()
    {
        if (view == null)
            view = Camera.main;

        LockBody();
        HideBodyFromThisCamera();
        HideArenaPlates();

        if (view != null)
        {
            restPosition = view.transform.position;
            restRotation = view.transform.rotation;
            view.enabled = true;
        }

        SceneTransitionController.Ensure().SetReveal(0f, 1f);
        eyeOpen = 0f;
    }

    void Start()
    {
        HideArenaPlates();
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.RecoveryArrived, true);

        WoundFromTheTrial();
        playing = true;
        StartCoroutine(Wake());
    }

    void LateUpdate()
    {
        if (view == null || !breathe)
            return;

        float time = Time.time;
        float pitch = Mathf.Sin(time * 0.45f) * breatheDegrees;
        float yaw = Mathf.Sin(time * 0.28f) * breatheDegrees * 0.55f;
        Vector3 shift = new Vector3(
            Mathf.Sin(time * 0.33f) * breatheMeters,
            Mathf.Sin(time * 0.62f) * breatheMeters * 0.65f,
            0f);

        view.transform.rotation = restRotation * Quaternion.Euler(pitch, yaw, 0f);
        view.transform.position = restPosition + restRotation * shift;
    }

    IEnumerator Wake()
    {
        yield return Hold(1.05f, 0f, 1f);

        yield return Tween(0.6f, 0f, 1f, 0.36f, 0.58f);
        breathe = true;
        yield return Hold(0.42f, 0.36f, 0.58f);
        breathe = false;
        yield return Tween(0.48f, 0.36f, 0.58f, 0f, 1f);

        yield return Hold(0.4f, 0f, 1f);

        yield return Tween(0.75f, 0f, 1f, 0.7f, 0.2f);
        breathe = true;
        yield return Hold(1.5f, 0.7f, 0.2f);
        yield return Tween(0.5f, 0.7f, 0.2f, 0f, 1f);
        breathe = false;

        yield return Hold(0.45f, 0f, 1f);

        yield return Tween(1.6f, 0f, 1f, 1f, 0f);
        breathe = false;
        playing = false;
        ShowObjectivePlate();
        BedRestLook rest = view != null ? view.GetComponent<BedRestLook>() : null;
        if (rest != null)
            rest.Begin();
    }

    IEnumerator Hold(float seconds, float open, float veil)
    {
        Apply(open, veil);
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }
    }

    IEnumerator Tween(float seconds, float fromOpen, float fromVeil, float toOpen, float toVeil)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            Apply(Mathf.Lerp(fromOpen, toOpen, t), Mathf.Lerp(fromVeil, toVeil, t));
            yield return null;
        }

        Apply(toOpen, toVeil);
    }

    void Apply(float open, float veil)
    {
        eyeOpen = open;
        if (SceneTransitionController.Exists)
            SceneTransitionController.Instance.SetReveal(open, veil);
    }

    static void LockBody()
    {
        OrayaMovement movement = Object.FindAnyObjectByType<OrayaMovement>();
        if (movement == null)
            return;

        movement.EndCinematicMove();
        movement.InputEnabled = false;
        movement.enabled = false;

        CharacterController body = movement.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;

        OrayaCameraManager cameras = movement.GetComponent<OrayaCameraManager>();
        if (cameras != null)
        {
            cameras.BeginEntryCinematic(null);
            cameras.enabled = false;
        }

        Camera[] camerasOnBody = movement.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < camerasOnBody.Length; i++)
        {
            if (camerasOnBody[i] != null)
                camerasOnBody[i].enabled = false;
        }

        AudioListener listener = movement.GetComponentInChildren<AudioListener>(true);
        if (listener != null)
            listener.enabled = false;
    }

    static void HideArenaPlates()
    {
        ObjectiveTrackerUI tracker = Object.FindAnyObjectByType<ObjectiveTrackerUI>();
        if (tracker != null && tracker.RootCanvas != null)
            tracker.RootCanvas.enabled = false;
    }

    static void ShowObjectivePlate()
    {
        ObjectiveTrackerUI tracker = Object.FindAnyObjectByType<ObjectiveTrackerUI>();
        if (tracker != null && tracker.RootCanvas != null)
            tracker.RootCanvas.enabled = true;
    }

    void HideBodyFromThisCamera()
    {
        if (view == null)
            return;

        int hidden = LayerMask.NameToLayer("FirstPersonHidden");
        if (hidden < 0)
            return;

        OrayaMovement movement = Object.FindAnyObjectByType<OrayaMovement>();
        if (movement != null)
        {
            Renderer[] renderers = movement.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].gameObject.layer = hidden;
            }
        }

        view.cullingMask &= ~(1 << hidden);
    }

    static void WoundFromTheTrial()
    {
        PlayerHealth health = Object.FindAnyObjectByType<PlayerHealth>();
        if (health == null || health.IsDead)
            return;

        float missing = health.CurrentHealth - 40f;
        if (missing > 1f)
            health.TakeDamage(missing);
    }
}
