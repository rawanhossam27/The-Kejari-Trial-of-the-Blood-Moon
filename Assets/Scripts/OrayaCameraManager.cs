using Unity.Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Switches Oraya between Cinemachine cameras. Third person is the default.
/// Close-combat (3) and cinematic cameras can be added later without changing movement.
/// </summary>
public class OrayaCameraManager : MonoBehaviour
{
    public enum Mode
    {
        ThirdPerson = 1,
        FirstPerson = 2
    }

    [SerializeField] CinemachineCamera thirdPersonCamera;
    [SerializeField] CinemachineCamera firstPersonCamera;
    [SerializeField] Renderer[] hiddenInFirstPerson;
    [SerializeField] int livePriority = 20;
    [SerializeField] float firstPersonPitch = 0f;

    Mode mode = Mode.ThirdPerson;
    bool ready;

    void Awake()
    {
        ready = true;
        Apply(Mode.ThirdPerson);
    }

    void OnEnable()
    {
        if (ready)
            Apply(mode);
    }

    void OnDisable()
    {
        SetFirstPersonMeshesVisible(true);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            SetMode(Mode.ThirdPerson);
        else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            SetMode(Mode.FirstPerson);
#endif
    }

    public Mode CurrentMode => mode;

    public void SetMode(Mode next)
    {
        if (next == mode)
            return;

        if (next == Mode.FirstPerson)
            AlignFirstPersonToView();
        else if (next == Mode.ThirdPerson)
            AlignThirdPersonBehindView();

        mode = next;
        Apply(next);
    }

    void Apply(Mode next)
    {
        bool firstPerson = next == Mode.FirstPerson;
        SetLive(thirdPersonCamera, !firstPerson);
        SetLive(firstPersonCamera, firstPerson);
        SetFirstPersonMeshesVisible(!firstPerson);
    }

    void SetLive(CinemachineCamera camera, bool live)
    {
        if (camera == null)
            return;

        camera.Priority = live ? livePriority : 0;
        // Cinemachine sorts its camera queue from Prioritize(), not from the priority value alone.
        camera.Prioritize();

        var input = camera.GetComponent<CinemachineInputAxisController>();
        if (input != null)
            input.enabled = live;
    }

    void SetFirstPersonMeshesVisible(bool visible)
    {
        if (hiddenInFirstPerson == null)
            return;

        foreach (var renderer in hiddenInFirstPerson)
        {
            if (renderer != null)
                renderer.enabled = visible;
        }
    }

    /// <summary>
    /// Faces the first-person camera along the current view yaw, with a level pitch.
    /// The third-person camera looks down at Oraya, so copying that pitch would stare at the ground.
    /// </summary>
    void AlignFirstPersonToView()
    {
        if (firstPersonCamera == null)
            return;

        firstPersonCamera.BlendHint = 0;

        var panTilt = firstPersonCamera.GetComponent<CinemachinePanTilt>();
        Camera view = Camera.main;
        if (panTilt == null || view == null)
            return;

        var pan = panTilt.PanAxis;
        pan.Value = view.transform.eulerAngles.y;
        panTilt.PanAxis = pan;

        var tilt = panTilt.TiltAxis;
        tilt.Value = Mathf.Clamp(firstPersonPitch, tilt.Range.x, tilt.Range.y);
        panTilt.TiltAxis = tilt;
    }

    /// <summary>
    /// Keeps the existing orbit radius and pitch, and turns the orbit yaw
    /// to sit behind the current view so the blend pulls back along the look direction.
    /// </summary>
    void AlignThirdPersonBehindView()
    {
        if (thirdPersonCamera == null)
            return;

        var orbital = thirdPersonCamera.GetComponent<CinemachineOrbitalFollow>();
        Camera view = Camera.main;
        if (orbital == null || view == null)
            return;

        var horizontal = orbital.HorizontalAxis;
        horizontal.Value = view.transform.eulerAngles.y;
        orbital.HorizontalAxis = horizontal;
    }
}
