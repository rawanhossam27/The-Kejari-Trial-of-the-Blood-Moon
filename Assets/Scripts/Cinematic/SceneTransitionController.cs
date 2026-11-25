using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fades the screen, then loads another scene. The overlay stays alive across the load
/// so the next scene can open the eyes instead of cutting in.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(-400)]
public class SceneTransitionController : MonoBehaviour
{
    public const string RecoveryRoomScene = "RecoveryRoom";

    const int CanvasSortOrder = 500;

    static SceneTransitionController instance;

    CanvasGroup group;
    RectTransform topLid;
    RectTransform bottomLid;
    Image veil;
    Coroutine running;

    public static SceneTransitionController Instance => instance;

    public static bool Exists => instance != null;

    public bool IsTransitioning { get; private set; }

    /// <summary>The live overlay, created on first use so either scene can drive it.</summary>
    public static SceneTransitionController Ensure()
    {
        if (instance != null)
            return instance;

        var host = new GameObject("SceneTransition");
        return host.AddComponent<SceneTransitionController>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);
        BuildOverlay();
        SetReveal(1f, 0f);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    /// <summary>
    /// Fades to black, then loads <paramref name="sceneName"/>.
    /// A second call while a fade is running does nothing.
    /// </summary>
    public void TransitionTo(string sceneName)
    {
        if (IsTransitioning || string.IsNullOrEmpty(sceneName))
            return;

        if (running != null)
            StopCoroutine(running);

        running = StartCoroutine(FadeAndLoad(sceneName));
    }

    /// <summary>
    /// <paramref name="eyeOpen"/> 0 is shut and 1 is fully open.
    /// <paramref name="veil"/> is extra darkness over the open gap, used for the first soft glimpses.
    /// </summary>
    public void SetReveal(float eyeOpen, float veilAmount)
    {
        if (topLid == null || bottomLid == null || veil == null)
            return;

        float open = Mathf.Clamp01(eyeOpen);
        float cover = (1f - open) * 0.5f;
        float seam = open >= 0.995f ? 0f : 0.01f;
        SetBand(topLid, 1f - cover - seam, 1f);
        SetBand(bottomLid, 0f, cover + seam);

        Color color = Color.black;
        color.a = Mathf.Clamp01(veilAmount);
        veil.color = color;

        if (group != null)
        {
            bool blocking = open < 0.995f || color.a > 0.02f;
            group.blocksRaycasts = blocking;
            group.interactable = false;
            group.alpha = 1f;
        }
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        IsTransitioning = true;
        float duration = 0.9f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float veilAmount = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetReveal(1f, veilAmount);
            yield return null;
        }

        SetReveal(0f, 1f);

        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        if (load == null)
        {
            Debug.LogError("SceneTransitionController could not load " + sceneName + ".");
            IsTransitioning = false;
            running = null;
            yield break;
        }

        while (!load.isDone)
            yield return null;

        SetReveal(0f, 1f);
        IsTransitioning = false;
        running = null;
    }

    void BuildOverlay()
    {
        var canvasObject = new GameObject("TransitionCanvas", typeof(RectTransform));
        canvasObject.transform.SetParent(transform, false);

        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortOrder;
        canvasObject.AddComponent<CanvasScaler>();
        group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 1f;
        group.interactable = false;

        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = Color.white;
        texture.SetPixels(pixels);
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

        veil = CreatePanel(canvasObject.transform, "Veil", sprite);
        topLid = CreatePanel(canvasObject.transform, "EyelidTop", sprite).rectTransform;
        bottomLid = CreatePanel(canvasObject.transform, "EyelidBottom", sprite).rectTransform;
        topLid.GetComponent<Image>().color = Color.black;
        bottomLid.GetComponent<Image>().color = Color.black;
    }

    static Image CreatePanel(Transform parent, string name, Sprite sprite)
    {
        var panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var image = panel.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.black;
        image.raycastTarget = true;
        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    static void SetBand(RectTransform rect, float yMin, float yMax)
    {
        yMin = Mathf.Clamp01(yMin);
        yMax = Mathf.Clamp01(yMax);
        if (yMax < yMin)
            yMax = yMin;

        rect.anchorMin = new Vector2(0f, yMin);
        rect.anchorMax = new Vector2(1f, yMax);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
