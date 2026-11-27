using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-level continue for the prototype. Remembers the last playable scene in PlayerPrefs
/// and can return there from the main menu. It does not restore position, health, or story flags
/// from a previous application launch.
/// </summary>
public static class GameSession
{
    public const string MenuScene = "MainMenu";

    /// <summary>
    /// Level 1. Play starts here once a scene with this name is in the build.
    /// Level 2 opens only after the player finishes Level 1. There is no level select.
    /// </summary>
    public const string MoonPalaceScene = "MoonPalace";

    /// <summary>Level 2, the Blood Moon Trial. Reached from Level 1, not from the menu.</summary>
    public const string TrialScene = "SampleScene";

    /// <summary>Level 3.</summary>
    public const string RecoveryScene = "RecoveryRoom";

    const string LastSceneKey = "kejari.lastScene";

    /// <summary>
    /// Where Play starts a new game. Always the first level.
    /// Put a scene named <see cref="MoonPalaceScene"/> in the build and Play loads it.
    /// Until Moon Palace exists, Play opens <see cref="TrialScene"/> so the prototype can still be started.
    /// Later levels are not offered on the menu. The arena exit opens the Recovery Room only after the trial is won.
    /// </summary>
    public static string NewGameScene =>
        IsInBuild(MoonPalaceScene) ? MoonPalaceScene : TrialScene;

    public static bool HasContinue => TryGetContinueScene(out _);

    public static void BeginNewGame()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.ResetStory();

        Load(NewGameScene);
    }

    public static void Continue()
    {
        if (TryGetContinueScene(out string scene))
            Load(scene);
    }

    public static void ReturnToMenu()
    {
        Load(MenuScene);
    }

    public static void Quit()
    {
        Time.timeScale = 1f;
        OrayaMovement.GameplaySuspended = false;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public static bool TryGetContinueScene(out string scene)
    {
        scene = PlayerPrefs.GetString(LastSceneKey, "");
        if (!IsPlayableLevel(scene) || !IsInBuild(scene))
        {
            scene = null;
            return false;
        }

        return true;
    }

    public static bool IsInBuild(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        int count = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                return true;
        }

        return false;
    }

    public static bool IsPlayableLevel(string sceneName)
    {
        return sceneName == MoonPalaceScene
            || sceneName == TrialScene
            || sceneName == RecoveryScene;
    }

    public static void Load(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return;

        Time.timeScale = 1f;
        OrayaMovement.GameplaySuspended = false;
        if (SceneTransitionController.Exists)
            SceneTransitionController.Instance.SetReveal(1f, 0f);

        SceneManager.LoadScene(sceneName);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (GameSessionHost.Live != null)
            return;

        var host = new GameObject("GameSession");
        Object.DontDestroyOnLoad(host);
        host.AddComponent<GameSessionHost>();
    }
}

/// <summary>
/// Records the last playable scene. Level 2 pause input lives on <see cref="LevelPauseMenu"/>,
/// which is the only Escape handler for that scene.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(50)]
public class GameSessionHost : MonoBehaviour
{
    public static GameSessionHost Live { get; private set; }

    LevelPauseMenu pause;

    void Awake()
    {
        if (Live != null && Live != this)
        {
            Destroy(gameObject);
            return;
        }

        Live = this;
        pause = GetComponent<LevelPauseMenu>();
        if (pause == null)
            pause = gameObject.AddComponent<LevelPauseMenu>();

        SceneManager.sceneLoaded += OnSceneLoaded;
        Remember(SceneManager.GetActiveScene().name);
        GameSettings.ApplyVolume();
        GameSettings.ApplyLook(true);
    }

    void OnEnable()
    {
        if (Live == null)
            Live = this;
    }

    void OnDestroy()
    {
        if (Live == this)
            Live = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Live == null)
            Live = this;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame || pause == null)
            return;

        if (pause.IsOpen)
            pause.Close(true);
        else if (CanPause())
            pause.Open();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Remember(scene.name);
        if (pause != null && pause.IsOpen)
            pause.Close(false);

        Time.timeScale = 1f;
        OrayaMovement.GameplaySuspended = false;
        GameSettings.ApplyVolume();
        GameSettings.ApplyLook(true);
    }

    static void Remember(string sceneName)
    {
        if (!GameSession.IsPlayableLevel(sceneName) || !GameSession.IsInBuild(sceneName))
            return;

        PlayerPrefs.SetString("kejari.lastScene", sceneName);
        PlayerPrefs.Save();
    }

    bool CanPause()
    {
        if (SceneManager.GetActiveScene().name != GameSession.TrialScene)
            return false;

        return !SceneTransitionController.Exists || !SceneTransitionController.Instance.IsTransitioning;
    }
}
