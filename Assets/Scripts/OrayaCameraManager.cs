using Unity.Cinemachine;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Switches Oraya between Cinemachine cameras. Third person is the default.
/// An interaction camera can later take a higher priority, then call RestoreGameplayCamera.
/// </summary>
public class OrayaCameraManager : MonoBehaviour
{
    public enum Mode
    {
        ThirdPerson = 1,
        FirstPerson = 2,
        CloseCombat = 3
    }

    [SerializeField] CinemachineCamera thirdPersonCamera;
    [SerializeField] CinemachineCamera firstPersonCamera;
    [SerializeField] CinemachineCamera closeCombatCamera;
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
        else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            SetMode(Mode.CloseCombat);
#endif
    }

    public Mode CurrentMode => mode;

    /// <summary>
    /// The gameplay camera the player last chose. A later interaction camera can
    /// override the brain with a higher priority, then call this to come back.
    /// </summary>
    public void RestoreGameplayCamera()
    {
        Apply(mode);
    }

    public void SetMode(Mode next)
    {
        if (next == mode)
            return;

        if (next == Mode.FirstPerson)
            AlignFirstPersonToView();
        else if (next == Mode.ThirdPerson)
            AlignOrbitalBehindView(thirdPersonCamera);
        else if (next == Mode.CloseCombat)
            AlignOrbitalBehindView(closeCombatCamera);

        mode = next;
        Apply(next);
    }

    void Apply(Mode next)
    {
        SetLive(thirdPersonCamera, next == Mode.ThirdPerson);
        SetLive(firstPersonCamera, next == Mode.FirstPerson);
        SetLive(closeCombatCamera, next == Mode.CloseCombat);
        SetFirstPersonMeshesVisible(next != Mode.FirstPerson);
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
    /// Keeps the orbit radius and pitch, and turns the orbit yaw to sit behind
    /// the current view so the blend pulls back along the look direction.
    /// </summary>
    static void AlignOrbitalBehindView(CinemachineCamera camera)
    {
        if (camera == null)
            return;

        var orbital = camera.GetComponent<CinemachineOrbitalFollow>();
        Camera view = Camera.main;
        if (orbital == null || view == null)
            return;

        var horizontal = orbital.HorizontalAxis;
        horizontal.Value = view.transform.eulerAngles.y;
        orbital.HorizontalAxis = horizontal;
    }
}
