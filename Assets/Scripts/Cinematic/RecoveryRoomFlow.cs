using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// After the bedside potion talk, Oraya gets up, reads the Kejari notice, hears Raihn, and sleeps.
/// Getting up and lying down reuse the third-person rig. There is no get-up clip, so a fade hides the reposition.
/// </summary>
[DisallowMultipleComponent]
public class RecoveryRoomFlow : MonoBehaviour
{
    const string ReadyLine = "The Crescent Moon Trial. Whatever they're planning, we'll need to be ready.";
    const string AloneLine = "The Crescent Moon Trial. Whatever they're planning, you'll need to be ready.";

    [SerializeField] Camera view;
    [SerializeField] OrayaMovement movement;
    [SerializeField] RecoveryAwakening awakening;
    [SerializeField] BedRestLook bedLook;
    [SerializeField] OrayaCameraManager cameras;
    [SerializeField] CinemachineBrain brain;
    [SerializeField] Transform letter;
    [SerializeField] DialogueData trialDialogue;

    Vector3 lyingPosition;
    Quaternion lyingRotation;
    Vector3 eyePosition;
    Quaternion eyeRotation;
    bool inspecting;
    bool busy;
    bool gotUp;
    bool trialStarted;
    bool talkHooked;
    int ignoreFrame = -1;
    GameObject heldPage;
    GameObject ending;

    public bool IsBusy => busy;
    public bool IsInspectingLetter => inspecting;

    public string OverridePrompt
    {
        get
        {
            if (inspecting)
                return "E - Put Down Letter";
            if (!gotUp && BedsideReady())
                return "E - Get Up";
            return null;
        }
    }

    void Awake()
    {
        if (view == null)
            view = Camera.main;
        if (movement == null)
            movement = FindAnyObjectByType<OrayaMovement>();
        if (awakening == null)
            awakening = FindAnyObjectByType<RecoveryAwakening>();
        if (bedLook == null && view != null)
            bedLook = view.GetComponent<BedRestLook>();
        if (cameras == null)
            cameras = FindAnyObjectByType<OrayaCameraManager>();
        if (brain == null && view != null)
            brain = view.GetComponent<CinemachineBrain>();
    }

    void Start()
    {
        if (movement != null)
        {
            lyingPosition = movement.transform.position;
            lyingRotation = movement.transform.rotation;
        }

        if (view != null)
        {
            eyePosition = view.transform.position;
            eyeRotation = view.transform.rotation;
        }

        if (brain != null)
            brain.enabled = false;
    }

    void Update()
    {
        HookTalk();
        if (!Pressed() || busy || Time.frameCount == ignoreFrame)
            return;
        if (DialogueManager.Instance != null && DialogueManager.Instance.IsRunning)
            return;

        if (inspecting)
        {
            StartCoroutine(PutDownLetter());
            return;
        }

        if (!gotUp && BedsideReady())
            StartCoroutine(GetUp());
    }

    public void BeginLetter()
    {
        if (busy || inspecting || letter == null)
            return;
        if (!StoryStateManager.Exists || !StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.OrayaGotUp))
            return;
        if (StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.LetterRead))
            return;

        ignoreFrame = Time.frameCount;
        StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.LetterDiscovered, true);
        StartCoroutine(OpenLetter());
    }

    void HookTalk()
    {
        if (talkHooked || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.ConversationFinished += IgnoreThisPress;
        talkHooked = true;
    }

    void IgnoreThisPress(DialogueData _)
    {
        ignoreFrame = Time.frameCount;
    }

    void OnDisable()
    {
        if (talkHooked && DialogueManager.Instance != null)
            DialogueManager.Instance.ConversationFinished -= IgnoreThisPress;
        talkHooked = false;
    }

    public void BeginSleep()
    {
        if (busy || inspecting || gotUp == false)
            return;
        if (!StoryStateManager.Exists)
            return;

        StoryStateManager story = StoryStateManager.Instance;
        if (!story.IsFlagSet(StoryStateManager.Flags.RaihnTrialSpoken) || story.IsFlagSet(StoryStateManager.Flags.RecoveryCompleted))
            return;

        StartCoroutine(Sleep());
    }

    bool BedsideReady()
    {
        if (awakening != null && awakening.IsPlaying)
            return false;
        if (!StoryStateManager.Exists)
            return false;
        return StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.RaihnBedsideSpoken);
    }

    IEnumerator GetUp()
    {
        busy = true;
        if (bedLook != null)
            bedLook.enabled = false;
        if (awakening != null)
            awakening.enabled = false;

        yield return Veil(0f, 1f, 0.45f);
        StandBesideBed();
        ShowBodyToGameplayCamera();
        EnableThirdPerson();
        yield return Veil(1f, 0f, 0.65f);

        if (movement != null)
            movement.InputEnabled = true;
        gotUp = true;
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.OrayaGotUp, true);
        busy = false;
    }

    void StandBesideBed()
    {
        if (movement == null)
            return;

        Renderer bed = null;
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && renderers[i].name == "Bed")
            {
                bed = renderers[i];
                break;
            }
        }

        Vector3 stand = movement.transform.position;
        stand.y = 0.02f;
        if (bed != null)
            stand = new Vector3(bed.bounds.center.x, 0.02f, bed.bounds.max.z + 1.15f);

        Vector3 face = Vector3.forward;
        if (letter != null)
        {
            face = letter.position - stand;
            face.y = 0f;
        }
        if (face.sqrMagnitude < 0.01f)
            face = Vector3.forward;

        CharacterController body = movement.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;

        movement.EndCinematicMove();
        movement.transform.SetPositionAndRotation(stand, Quaternion.LookRotation(face.normalized, Vector3.up));
        movement.enabled = true;
        movement.InputEnabled = false;
        if (body != null)
            body.enabled = true;
    }

    IEnumerator OpenLetter()
    {
        busy = true;
        inspecting = true;
        if (movement != null)
            movement.InputEnabled = false;
        if (cameras != null)
            cameras.BeginEntryCinematic(null);
        if (brain != null)
            brain.enabled = false;

        if (letter != null)
            letter.gameObject.SetActive(false);
        EnsureHeldPage();
        heldPage.SetActive(true);
        if (view != null)
            view.fieldOfView = 46f;
        HideBodyFromThisCamera();

        Vector3 from = view.transform.position;
        Quaternion fromRot = view.transform.rotation;
        Vector3 flat = movement != null ? movement.transform.forward : view.transform.forward;
        flat.y = 0f;
        if (flat.sqrMagnitude < 0.01f)
            flat = Vector3.forward;
        flat.Normalize();
        Vector3 to = (movement != null ? movement.transform.position : from) + Vector3.up * 1.55f + flat * 0.16f;
        Quaternion toRot = Quaternion.LookRotation(flat, Vector3.up) * Quaternion.Euler(10f, 0f, 0f);

        float elapsed = 0f;
        const float duration = 0.55f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            view.transform.SetPositionAndRotation(Vector3.Lerp(from, to, t), Quaternion.Slerp(fromRot, toRot, t));
            yield return null;
        }

        view.transform.SetPositionAndRotation(to, toRot);
        busy = false;
    }

    IEnumerator PutDownLetter()
    {
        busy = true;
        inspecting = false;
        if (heldPage != null)
            heldPage.SetActive(false);
        if (letter != null)
            letter.gameObject.SetActive(true);

        if (StoryStateManager.Exists)
            StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.LetterRead, true);

        Vector3 settle = movement != null
            ? movement.transform.position + (-movement.transform.forward * 2.1f) + Vector3.up * 1.55f
            : view.transform.position;
        Quaternion settleRot = movement != null
            ? Quaternion.LookRotation(movement.transform.position + Vector3.up * 1.2f - settle, Vector3.up)
            : view.transform.rotation;

        Vector3 from = view.transform.position;
        Quaternion fromRot = view.transform.rotation;
        float elapsed = 0f;
        const float duration = 0.45f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            view.transform.SetPositionAndRotation(Vector3.Lerp(from, settle, t), Quaternion.Slerp(fromRot, settleRot, t));
            yield return null;
        }

        ShowBodyToGameplayCamera();
        EnableThirdPerson();
        if (movement != null)
            movement.InputEnabled = true;
        busy = false;
        StartTrialTalk();
    }

    void StartTrialTalk()
    {
        if (trialStarted || trialDialogue == null || DialogueManager.Instance == null)
            return;
        if (StoryStateManager.Exists && StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.RaihnTrialSpoken))
        {
            trialStarted = true;
            return;
        }

        trialStarted = true;
        DialogueData copy = Instantiate(trialDialogue);
        bool declined = StoryStateManager.Exists
            && StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.AllianceDecisionMade)
            && !StoryStateManager.Instance.IsFlagSet(StoryStateManager.Flags.AllianceAccepted);
        if (copy.lines != null && copy.lines.Length > 2 && copy.lines[2] != null)
            copy.lines[2].text = declined ? AloneLine : ReadyLine;

        DialogueManager.Instance.StartDialogue(copy, true, 0f, true);
    }

    IEnumerator Sleep()
    {
        busy = true;
        if (movement != null)
            movement.InputEnabled = false;
        if (cameras != null)
            cameras.BeginEntryCinematic(null);

        yield return Veil(0f, 1f, 0.4f);
        LieDown();
        if (brain != null)
            brain.enabled = false;
        if (view != null)
            view.transform.SetPositionAndRotation(eyePosition, eyeRotation);
        HideBodyFromThisCamera();
        yield return Veil(1f, 0f, 0.55f);
        yield return new WaitForSeconds(0.85f);

        float elapsed = 0f;
        const float close = 2.4f;
        while (elapsed < close)
        {
            elapsed += Time.deltaTime;
            float shut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / close));
            SceneTransitionController.Ensure().SetReveal(1f - shut, shut * 0.35f);
            yield return null;
        }

        SceneTransitionController.Ensure().SetReveal(0f, 1f);
        ShowEnding();
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.SetFlag(StoryStateManager.Flags.RecoveryCompleted, true);
    }

    void LieDown()
    {
        if (movement == null)
            return;

        CharacterController body = movement.GetComponent<CharacterController>();
        if (body != null)
            body.enabled = false;
        movement.EndCinematicMove();
        movement.enabled = false;
        movement.transform.SetPositionAndRotation(lyingPosition, lyingRotation);
    }

    void EnableThirdPerson()
    {
        if (cameras != null)
        {
            cameras.enabled = true;
            cameras.EndEntryCinematic();
        }

        if (brain != null)
            brain.enabled = true;

        if (view != null && view.GetComponent<ThirdPersonCursorLock>() == null)
            view.gameObject.AddComponent<ThirdPersonCursorLock>();
    }

    void ShowBodyToGameplayCamera()
    {
        SetBodyLayer(0);
        if (view != null)
            view.cullingMask = ~0;
    }

    void HideBodyFromThisCamera()
    {
        int hidden = LayerMask.NameToLayer("FirstPersonHidden");
        if (hidden < 0 || view == null)
            return;

        SetBodyLayer(hidden);
        view.cullingMask &= ~(1 << hidden);
    }

    void SetBodyLayer(int layer)
    {
        if (movement == null)
            return;

        Renderer[] renderers = movement.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].gameObject.layer = layer;
        }
    }

    void EnsureHeldPage()
    {
        if (heldPage != null || view == null)
            return;

        heldPage = new GameObject("HeldNotice");
        heldPage.transform.SetParent(view.transform, false);
        heldPage.transform.localPosition = new Vector3(0f, -0.03f, 0.78f);
        heldPage.transform.localRotation = Quaternion.identity;
        heldPage.transform.localScale = Vector3.one * 0.00048f;

        var canvas = heldPage.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        var rect = heldPage.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(760f, 980f);

        var paper = new GameObject("Paper", typeof(RectTransform));
        paper.transform.SetParent(heldPage.transform, false);
        var paperRect = paper.GetComponent<RectTransform>();
        paperRect.anchorMin = Vector2.zero;
        paperRect.anchorMax = Vector2.one;
        paperRect.offsetMin = Vector2.zero;
        paperRect.offsetMax = Vector2.zero;
        var image = paper.AddComponent<Image>();
        image.color = new Color(0.78f, 0.67f, 0.48f, 1f);
        image.raycastTarget = false;

        var textObject = new GameObject("Words", typeof(RectTransform));
        textObject.transform.SetParent(paper.transform, false);
        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(54f, 48f);
        textRect.offsetMax = new Vector2(-54f, -48f);
        var words = textObject.AddComponent<TextMeshProUGUI>();
        words.alignment = TextAlignmentOptions.Top;
        words.fontSize = 36f;
        words.color = new Color(0.16f, 0.1f, 0.07f, 1f);
        words.raycastTarget = false;
        words.text = NoticeText();
        if (TMP_Settings.defaultFontAsset != null)
            words.font = TMP_Settings.defaultFontAsset;
        heldPage.SetActive(false);
    }

    void ShowEnding()
    {
        if (ending != null)
            return;

        ending = new GameObject("PrototypeEnding", typeof(RectTransform));
        var canvas = ending.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 620;
        var scaler = ending.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        var textObject = new GameObject("Ending", typeof(RectTransform));
        textObject.transform.SetParent(ending.transform, false);
        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(1200f, 360f);
        var label = textObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 42f;
        label.color = new Color(0.93f, 0.88f, 0.78f, 1f);
        label.raycastTarget = false;
        label.text = "END OF PROTOTYPE\n\nThe Crescent Moon Trial awaits.";
        if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
    }

    static string NoticeText()
    {
        return "THE KEJARI\n\n"
            + "Official Notice to Surviving Contestants\n\n"
            + "The next trial shall commence in fourteen days.\n\n"
            + "SECOND TRIAL: THE CRESCENT MOON TRIAL\n\n"
            + "Those who survived the Blood Moon Trial are hereby summoned to prepare for the next challenge.\n\n"
            + "Further instructions will be revealed when the Crescent Moon rises.\n\n"
            + "May Nyaxia judge you worthy.";
    }

    static IEnumerator Veil(float from, float to, float seconds)
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.05f, seconds);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SceneTransitionController.Ensure().SetReveal(1f, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SceneTransitionController.Ensure().SetReveal(1f, to);
    }

    static bool Pressed()
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.eKey.wasPressedThisFrame;
#else
        return false;
#endif
    }
}
