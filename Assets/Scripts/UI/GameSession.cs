using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-level continue for the prototype. Remembers the last legitimately reached scene in
/// PlayerPrefs. Level 2 and the Recovery Room are stored only after Level 1 is completed.
/// It does not restore position, health, or story flags from a previous application launch.
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

    const string Level1CompleteKey = "kejari.level1Complete";

    /// <summary>
    /// Where Play starts a new game. Always Moon Palace. Level 2 is not on the menu.
    /// </summary>
    public static string NewGameScene => MoonPalaceScene;

    /// <summary>
    /// True after <see cref="CompleteLevel1"/> has saved the unlock.
    /// Checkpoint restart in Level 2 does not clear this.
    /// </summary>
    public static bool IsLevel1Complete => PlayerPrefs.GetInt(Level1CompleteKey, 0) == 1;

    public static bool HasContinue => TryGetContinueScene(out _);

    public static void BeginNewGame()
    {
        if (StoryStateManager.Exists)
            StoryStateManager.Instance.ResetStory();
        if (ObjectiveManager.Exists)
            ObjectiveManager.Instance.ResetProgress();

        PlayerPrefs.SetInt(Level1CompleteKey, 0);
        PlayerPrefs.DeleteKey(LastSceneKey);
        PlayerPrefs.Save();
        Load(MoonPalaceScene);
    }

    /// <summary>
    /// Saves the Level 1 unlock and moves the player to the Blood Moon Trial.
    /// Call this from the future Level 1 completion event. This method does not decide
    /// when Level 1 is finished, and nothing calls it on entering Moon Palace.
    /// </summary>
    public static void CompleteLevel1()
    {
        PlayerPrefs.SetInt(Level1CompleteKey, 1);
        PlayerPrefs.SetString(LastSceneKey, TrialScene);
        PlayerPrefs.Save();

        if (SceneManager.GetActiveScene().name == TrialScene)
            return;

        SceneTransitionController.Ensure().TransitionTo(TrialScene);
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
        scene = LegitimateContinueScene(PlayerPrefs.GetString(LastSceneKey, ""));
        if (string.IsNullOrEmpty(scene) || !IsInBuild(scene))
        {
            scene = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Moon Palace is always a valid continue target. Level 2 and the Recovery Room
    /// are valid only after Level 1 has been completed and saved.
    /// </summary>
    static string LegitimateContinueScene(string sceneName)
    {
        if (sceneName == MoonPalaceScene)
            return MoonPalaceScene;

        if ((sceneName == TrialScene || sceneName == RecoveryScene) && IsLevel1Complete)
            return sceneName;

        return null;
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
            SceneTransitionController.Instance.Cancel();

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

        if (sceneName != GameSession.MoonPalaceScene && !GameSession.IsLevel1Complete)
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
