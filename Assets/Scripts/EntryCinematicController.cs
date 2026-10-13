using Unity.Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Plays the arena entry spline once at scene start, then hands control to Camera 1.
/// </summary>
[DefaultExecutionOrder(100)]
public class EntryCinematicController : MonoBehaviour
{
    [SerializeField] OrayaCameraManager cameraManager;
    [SerializeField] OrayaMovement movement;
    [SerializeField] CinemachineCamera cinematicCamera;
    [SerializeField] CinemachineSplineDolly dolly;
    [SerializeField, Min(0.1f)] float duration = 6.5f;
    [SerializeField] AnimationCurve progress = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [SerializeField, Min(0f)] float skipBlendTime = 0.35f;

    bool externallyDriven;
    bool playing;
    bool waitingForBlend;
    bool sawBlend;
    int blendWaitFrames;
    bool restoreBlend;
    CinemachineBlendDefinition savedBlend;

    public bool IsPlaying => playing || waitingForBlend;

    /// <summary>
    /// The entry sequence drives the spline. This controller still owns the camera and the gameplay handoff.
    /// </summary>
    public void SetExternallyDriven(bool driven)
    {
        externallyDriven = driven;
    }

    public void SetPathProgress(float normalized)
    {
        if (dolly != null)
            dolly.CameraPosition = Mathf.Clamp01(normalized);
    }

    public void RequestFinish(bool skipped)
    {
        if (skipped)
            SkipToGameplay();
        else
            Finish(false);
    }

    void Awake()
    {
        if (dolly != null)
            dolly.CameraPosition = 0f;

        if (movement != null)
            movement.InputEnabled = false;

        if (cameraManager != null)
            cameraManager.BeginEntryCinematic(cinematicCamera);

        playing = true;
    }

    void Update()
    {
        if (playing && !externallyDriven && SkipPressed())
        {
            SkipToGameplay();
            return;
        }

        if (!playing || externallyDriven)
            return;

        float durationSafe = Mathf.Max(0.1f, duration);
        float t = Mathf.Clamp01(Time.time / durationSafe);
        if (dolly != null)
            dolly.CameraPosition = progress.Evaluate(t);

        if (t >= 1f)
            Finish(false);
    }

    void LateUpdate()
    {
        if (!waitingForBlend)
            return;

        var brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null && brain.IsBlending)
            sawBlend = true;

        blendWaitFrames++;
        if (sawBlend && (brain == null || !brain.IsBlending))
            RestoreGameplay();
        else if (!sawBlend && blendWaitFrames > 5)
            RestoreGameplay();
    }

    void SkipToGameplay()
    {
        SetPathProgress(1f);

        var brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null)
        {
            savedBlend = brain.DefaultBlend;
            brain.DefaultBlend = new CinemachineBlendDefinition(
                CinemachineBlendDefinition.Styles.EaseInOut,
                skipBlendTime);
            restoreBlend = true;
        }

        Finish(true);
    }

    void Finish(bool skipped)
    {
        if (!playing && !skipped)
            return;

        playing = false;
        waitingForBlend = true;
        sawBlend = false;
        blendWaitFrames = 0;

        if (cameraManager != null)
            cameraManager.EndEntryCinematic();
    }

    void RestoreGameplay()
    {
        waitingForBlend = false;

        if (movement != null)
            movement.InputEnabled = true;

        if (!restoreBlend)
            return;

        var brain = CinemachineBrain.GetActiveBrain(0);
        if (brain != null)
            brain.DefaultBlend = savedBlend;
        restoreBlend = false;
    }

    static bool SkipPressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.spaceKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
