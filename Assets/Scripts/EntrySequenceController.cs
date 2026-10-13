using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Events;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Plays the arena entry in three shots: a locked view of the gate opening,
/// a follow as Oraya walks in, then a locked view of the gate closing.
/// Blood Moon can subscribe to <see cref="onEntryCompleted"/> later.
/// </summary>
[DefaultExecutionOrder(150)]
public class EntrySequenceController : MonoBehaviour
{
    enum Phase
    {
        Approach,
        Opening,
        OpenHold,
        Entering,
        Closing,
        Done
    }

    [SerializeField] ArenaGate gate;
    [SerializeField] EntryCinematicController cinematic;
    [SerializeField] CinemachineCamera followCamera;
    [SerializeField] CinemachineCamera gateCloseShot;
    [SerializeField] OrayaMovement movement;
    [SerializeField] Transform oraya;
    [SerializeField, Min(0f)] float approachDuration = 0.85f;
    [SerializeField, Min(0f)] float openHold = 0.45f;
    [SerializeField, Min(1f)] float turnSpeed = 220f;
    [SerializeField] Vector3 outsidePoint = new Vector3(0f, 1f, -15.7f);

    [SerializeField] UnityEvent onGateOpened;
    [SerializeField] UnityEvent onEntryCompleted;

    public event Action GateOpened;
    public event Action EntryCompleted;

    Phase phase;
    float phaseTime;
    Vector3 homePosition;
    Quaternion homeRotation;
    bool entryNotified;
    bool finishRequested;
    bool closeRequested;
    bool closeShotReady;
    int closeShotFrames;
    bool snappedHome;
    float walkDistance = 1f;

    public bool IsComplete => phase == Phase.Done && entryNotified;

    void Awake()
    {
        if (cinematic == null)
            cinematic = GetComponent<EntryCinematicController>();
        if (followCamera == null)
        {
            var followObject = GameObject.Find("EntryCinematicCamera");
            if (followObject != null)
                followCamera = followObject.GetComponent<CinemachineCamera>();
        }
        if (gateCloseShot == null)
        {
            var closeObject = GameObject.Find("EntryGateCloseShot");
            if (closeObject != null)
                gateCloseShot = closeObject.GetComponent<CinemachineCamera>();
        }
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (oraya == null && movement != null)
            oraya = movement.transform;
        if (gate == null)
        {
            var gateObject = GameObject.Find("Arena_Entry_Gate");
            if (gateObject != null)
                gate = gateObject.GetComponent<ArenaGate>();
        }

        if (cinematic != null)
            cinematic.SetExternallyDriven(true);

        if (oraya != null)
        {
            homePosition = oraya.position;
            homeRotation = oraya.rotation;
            PlaceOutside();
        }

        if (movement != null)
            movement.BeginCinematicMove();

        if (gate != null)
            gate.Opened += HandleGateOpened;
    }

    void OnDestroy()
    {
        if (gate != null)
            gate.Opened -= HandleGateOpened;
    }

    void Update()
    {
        if (phase == Phase.Done)
            return;

        if (SkipPressed())
        {
            Skip();
            return;
        }

        switch (phase)
        {
            case Phase.Approach:
                TickApproach();
                break;
            case Phase.Opening:
                TickOpening();
                break;
            case Phase.OpenHold:
                TickOpenHold();
                break;
            case Phase.Entering:
                TickEntering();
                break;
            case Phase.Closing:
                TickClosing();
                break;
        }
    }

    void LateUpdate()
    {
        if (!finishRequested || entryNotified)
            return;
        if (cinematic != null && cinematic.IsPlaying)
            return;

        entryNotified = true;
        EntryCompleted?.Invoke();
        onEntryCompleted?.Invoke();
    }

    void TickApproach()
    {
        phaseTime += Time.deltaTime;
        HoldStill();
        SetPath(0f);
        if (phaseTime < approachDuration)
            return;

        phase = Phase.Opening;
        phaseTime = 0f;
        if (gate != null)
            gate.Open();
        else
            BeginEntering();
    }

    void TickOpening()
    {
        HoldStill();
        SetPath(0f);
        if (gate == null || gate.IsOpen)
        {
            phase = Phase.OpenHold;
            phaseTime = 0f;
        }
    }

    void TickOpenHold()
    {
        HoldStill();
        SetPath(0f);
        phaseTime += Time.deltaTime;
        if (phaseTime >= openHold)
            BeginEntering();
    }

    void BeginEntering()
    {
        phase = Phase.Entering;
        phaseTime = 0f;
        walkDistance = oraya != null ? Vector3.Distance(
            new Vector3(oraya.position.x, 0f, oraya.position.z),
            new Vector3(homePosition.x, 0f, homePosition.z)) : 1f;
        if (walkDistance < 0.25f)
            walkDistance = 0.25f;
    }

    static float Smooth(float value)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(value));
    }

    void TickEntering()
    {
        if (oraya == null || movement == null)
        {
            BeginClosing();
            return;
        }

        Vector3 flat = homePosition - oraya.position;
        flat.y = 0f;
        float remaining = flat.magnitude;
        float walked = walkDistance > 0.01f ? 1f - Mathf.Clamp01(remaining / walkDistance) : 1f;
        SetPath(Smooth(walked));

        if (remaining > 0.08f)
        {
            float ease = Mathf.Clamp01(remaining / 0.75f);
            movement.SetCinematicDirection(flat.normalized * ease);
            return;
        }

        movement.SetCinematicDirection(Vector3.zero);
        BeginClosing();
    }

    void BeginClosing()
    {
        phase = Phase.Closing;
        phaseTime = 0f;
        if (movement != null)
        {
            movement.SetCinematicDirection(Vector3.zero);
            movement.EndCinematicMove();
        }

        SetPath(1f);
        if (gateCloseShot != null)
        {
            gateCloseShot.Priority = 30;
            gateCloseShot.Prioritize();
        }

        if (followCamera != null)
        {
            followCamera.Priority = 0;
            followCamera.Prioritize();
        }
    }

    void TickClosing()
    {
        bool turned = true;
        if (oraya != null && !snappedHome)
        {
            oraya.rotation = Quaternion.RotateTowards(
                oraya.rotation,
                homeRotation,
                turnSpeed * Time.deltaTime);
            turned = Quaternion.Angle(oraya.rotation, homeRotation) <= 1.25f;
        }

        if (turned && !snappedHome)
        {
            SnapHome();
            snappedHome = true;
        }

        if (!closeShotReady)
        {
            closeShotFrames++;
            var brain = CinemachineBrain.GetActiveBrain(0);
            bool blendFinished = gateCloseShot == null || (closeShotFrames > 6 && (brain == null || !brain.IsBlending));
            if (!blendFinished)
                return;

            closeShotReady = true;
            if (!closeRequested && gate != null)
            {
                closeRequested = true;
                gate.Close();
            }
        }

        if (snappedHome && closeShotReady && (gate == null || gate.IsClosed))
            Complete(false);
    }

    void Skip()
    {
        if (gate != null)
            gate.SetClosedImmediate();

        if (movement != null)
        {
            movement.SetCinematicDirection(Vector3.zero);
            movement.EndCinematicMove();
        }

        SnapHome();
        SetPath(1f);
        ReleaseCloseShot();
        Complete(true);
    }

    void Complete(bool skipped)
    {
        if (phase == Phase.Done)
            return;

        phase = Phase.Done;
        finishRequested = true;
        ReleaseCloseShot();
        if (cinematic != null)
            cinematic.RequestFinish(skipped);
        else
            LateUpdate();
    }

    void PlaceOutside()
    {
        Vector3 point = outsidePoint;
        Vector3 origin = point + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            point.y = hit.point.y;

        var body = oraya.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;
        oraya.SetPositionAndRotation(point, Quaternion.LookRotation(Vector3.forward, Vector3.up));
        if (body != null)
            body.enabled = true;
    }

    void SnapHome()
    {
        if (oraya == null)
            return;

        var body = oraya.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;
        oraya.SetPositionAndRotation(homePosition, homeRotation);
        if (body != null)
            body.enabled = true;
    }

    void HoldStill()
    {
        if (movement != null)
            movement.SetCinematicDirection(Vector3.zero);
    }

    void ReleaseCloseShot()
    {
        if (gateCloseShot == null)
            return;

        gateCloseShot.Priority = 0;
        gateCloseShot.Prioritize();
    }

    void SetPath(float progress)
    {
        if (cinematic != null)
            cinematic.SetPathProgress(progress);
    }

    void HandleGateOpened()
    {
        GateOpened?.Invoke();
        onGateOpened?.Invoke();
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
