using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

//public enum GameState { Play, Paused }

public class NewManager : DataPersistenceBehaviour
{
    #region Singleton
    public static NewManager manager { get; private set; }
    private void Awake()
    {
        if (manager != null && manager != this)
        {
            Destroy(gameObject);
            return;
        }

        manager = this;
        DontDestroyOnLoad(gameObject);

        CachePlayerReference();
        DataPersistenceManager.instance.LoadGame(true);
    }
    #endregion

    #region Serialized Settings
    [Header("UI")]
    [SerializeField] public GameObject loadingScreenPanel;
    [SerializeField] public Slider loadingBar;
    [SerializeField] public GameObject endGamePanel;

    [Header("Spawn Settings")]
    [SerializeField] public Vector2 defaultPlayerLocation { get; set; } = new Vector2(2, -59);
    [SerializeField] public string defaultPlayerScene { get; set; } = "Grandpa's Farm";

    [Header("Shop Data")]
    [SerializeField] public InventoryObject[] shops;
    #endregion

    #region Runtime state
    public static bool gameStarted = false;
    private GameObject player;
    private bool respawnHandled = false;
    #endregion

    public void BootStrapAfterDataLoaded()
    {
        if (!gameStarted)
        {
            IntroVideoPlayer.instance.PlayLogoSequence();
        }
    }

    private void CachePlayerReference()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player");
    }

    #region Public API

    public void TriggerEndGame()
    {
        endGamePanel.SetActive(true);
    }

    public void NewGame()
    {
        DataPersistenceManager.instance.NewGame();
        StartCoroutine(ResetStoriesDelayed());
        GameStateManager.instance.ChangeState(GameState.movingScene);
            
        defaultPlayerLocation = new Vector2(2, -59);

        TelemetryManager.instance.LogEvent("Session", "new_game_started");

        // Load base scene first and then the player spawn scene
        StartCoroutine(SceneTransition("Base Scene", false, true, previousSceneToUnload: "Menu"));
        StartCoroutine(SceneTransition("Grandpa's Farm", false, true));
    }

    /// <summary>
    /// Public entry to move to a scene. previousScene will be unloaded after the new scene finishes loading.
    /// </summary>
    public void MoveToScene(string newScene, string previousScene, bool upBoost)
    {
        GameStateManager.instance.ChangeState(GameState.movingScene);
        DataPersistenceManager.instance.SaveGame();
        // Pass the previous scene so we unload it after the transition finishes.
        StartCoroutine(SceneTransition(newScene, upBoost, false, previousScene));
    }

    public void AddScene(string newScene, bool upBoost)
    {
        GameStateManager.instance.ChangeState(GameState.movingScene);
        DataPersistenceManager.instance.SaveGame();
        StartCoroutine(SceneTransition(newScene, upBoost, false, previousSceneToUnload: null)); // keep previous
    }

    public void UnloadScenePublic(string sceneName)
    {
        StartCoroutine(UnloadScene(sceneName));
    }

    #endregion

    #region Scene Loading

    /// <summary>
    /// Loads a scene additively, waits until it is activated, then safely waits for the player and finalizes.
    /// Optionally unloads previousScene after finish.
    /// </summary>
    private IEnumerator SceneTransition(string newScene, bool upBoost, bool newGame = false, string previousSceneToUnload = null)
    {
        TelemetryManager.instance.LogEvent("Session", $"Attempting_SceneTransition_to:_{newScene}_from:_{previousSceneToUnload}_newGame:_{newGame}",
            new SceneTransitionPayload
            {
                fromScene = previousSceneToUnload,
                toScene = newScene,
                newGame = newGame
            });
        CachePlayerReference();
        if (loadingScreenPanel) loadingScreenPanel.SetActive(true);

        //save object data before scene change
        DataPersistenceManager.instance.PrepareForSceneChange();

        //code to manage reloading the same scene
        #region reloading same scene
        string activeSceneName = SceneManager.GetActiveScene().name;

        bool isReloadingSameScene = (newScene == activeSceneName);
        if (isReloadingSameScene)
        {
            Debug.Log($"Reloading scene: {newScene}");
            TelemetryManager.instance.LogEvent("Session", $"Reloading scene:_{newScene}");
            // cache player data before unload
            CachePlayerReference();

            if (loadingScreenPanel) loadingScreenPanel.SetActive(true);

            AsyncOperation unload = SceneManager.UnloadSceneAsync(activeSceneName);
            if (unload != null)
            {
                while (!unload.isDone)
                    yield return null;
            }

            // allow one frame for cleanup
            yield return null;
        }
        #endregion

        // begin async load of the scene
        AsyncOperation loadScene = SceneManager.LoadSceneAsync(newScene, LoadSceneMode.Additive);
        if (loadScene == null)
        {
            Debug.LogError($"Failed to start loading scene: {newScene}");
            TelemetryManager.instance.LogError($"Failed to start loading scene:_{newScene}");
            yield break;
        }

        loadScene.allowSceneActivation = false;

        // update loading bar while loading
        while (!loadScene.isDone)
        {
            float progress = Mathf.Clamp01(loadScene.progress); // Normalize progress to 0-1
            if (loadingBar) loadingBar.value = progress;

            if (loadScene.progress >= 0.9f)
            {
                // Optionally add a delay or fade-out effect here
                loadScene.allowSceneActivation = true;
            }

            yield return null; // Wait for next frame
        }

        // new scene is now active (loaded additively)
        Scene loadedScene = SceneManager.GetSceneByName(newScene);
        if (!loadedScene.IsValid())
        {
            Debug.LogError($"Loaded scene is not valid: {newScene}");
            TelemetryManager.instance.LogError($"Loaded scene is not valid:_{newScene}");
        }
        else
        {
            Debug.Log("setting active scene to " + newScene);
            SceneManager.SetActiveScene(loadedScene);
        }

        // Allow one frame for scene root objects to initialize
        yield return null;

        // Wait for player to exist and finalize placement, with timeout to avoid infinite loops
        yield return StartCoroutine(HandlePlayerAfterSceneLoad_Coroutine(newScene, upBoost, newGame));

        // Unload previous scene only after new scene is loaded & player handled (if requested)
        Debug.Log("previousSceneToUnload: " + previousSceneToUnload);
        if (!string.IsNullOrEmpty(previousSceneToUnload) && !isReloadingSameScene)
        {
            // only attempt unload if scene exists and is loaded
            Scene prev = SceneManager.GetSceneByName(previousSceneToUnload);
            Debug.Log("unloading scene " + previousSceneToUnload);
            if (prev.IsValid() && prev.isLoaded)
            {
                Debug.Log("Unloading scene: " + previousSceneToUnload);
                yield return UnloadScene(previousSceneToUnload);
            }
            else
            {
                Debug.LogWarning($"Previous scene to unload is not valid or not loaded: {previousSceneToUnload}");
            }
        }

        if (loadingScreenPanel) loadingScreenPanel.SetActive(false);

        GameStateManager.instance.ChangeState(GameState.Play);
    }


    private IEnumerator HandlePlayerAfterSceneLoad_Coroutine(string sceneName, bool launchUp, bool isNewGame)
    {
        // Wait for player to be available, but avoid infinite wait by using a timeout.
        const float maxWaitSeconds = 5f;
        float waited = 0f;

        while (player == null && waited < maxWaitSeconds)
        {
            CachePlayerReference();
            if (player != null) break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (player == null)
        {
            // As a last resort, try finding by tag across scenes
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player == null)
        {
            Debug.LogWarning($"Player object not found after loading scene '{sceneName}' (waited {waited:F2}s). Aborting player-specific steps.");
            yield break;
        }

        // Temporarily make player static while we place/load data
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.bodyType = RigidbodyType2D.Static;

        // Launch up if requested
        var playerController = player.GetComponent<PlayerController>();
        if (launchUp && playerController != null)
        {
            playerController.Jump();
        }

        if (isNewGame)
        {
            // Reset data when player components are ready
            StartCoroutine(ResetNewGameDataWhenReady());
        }
        else
        {
            // Delay a frame to let objects initialize before load
            yield return null;
            DataPersistenceManager.instance.LoadGame();
        }

        if (sceneName == "Base Scene" && !respawnHandled)
        {
            respawnHandled = true;
            StartCoroutine(SetPlayerToSavedLocationDelayed());
        }
        else if (sceneName != "Base Scene")
        {
            respawnHandled = false;
        }

        if (body != null) body.bodyType = RigidbodyType2D.Dynamic;
    }

    private IEnumerator SetPlayerToSavedLocationDelayed()
    {
        yield return new WaitForSeconds(1f);
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            pc?.SetToSavedLocation();
        }
    }

    private IEnumerator UnloadScene(string sceneName)
    {
        Scene s = SceneManager.GetSceneByName(sceneName);
        if (!s.IsValid() || !s.isLoaded)
        {
            yield break;
        }

        AsyncOperation unload = SceneManager.UnloadSceneAsync(sceneName);
        if (unload == null)
        {
            Debug.LogWarning($"UnloadSceneAsync returned null for '{sceneName}'");
            yield break;
        }

        while (!unload.isDone)
            yield return null;
    }
    #endregion

    #region Game Reset & save/load

    IEnumerator ResetStoriesDelayed()
    {
        yield return new WaitForSeconds(2.5f);
        DialogueManager.instance.ResetAllStories();
    }

    private IEnumerator ResetNewGameDataWhenReady()
    {
        PlayerInventory inv = null;

        // Wait until PlayerInventory appears in scene (but safe timeout)
        const float maxWait = 5f;
        float waited = 0f;
        while (inv == null && waited < maxWait)
        {
            inv = FindAnyObjectByType<PlayerInventory>();
            if (inv != null) break;
            waited += Time.deltaTime;
            yield return null;
        }

        if (inv == null)
        {
            Debug.LogWarning("PlayerInventory not found when resetting new-game data.");
            yield break;
        }

        inv.inventory.Clear();
        inv.equipment.Clear();
        inv.inventory.Save();
        inv.equipment.Save();

        foreach (var shop in shops)
        {
            shop.Reset();
            shop.Save();
        }
    }

    public override void LoadData(GameData data)
    {
        defaultPlayerLocation = data.playerSaveLocation;
        defaultPlayerScene = data.playerSpawnScene;
    }

    public override void SaveData(GameData data)
    {
        data.playerSaveLocation = defaultPlayerLocation;
        data.playerSpawnScene = defaultPlayerScene;
    }

    public override void ResetData(GameData data)
    {
        defaultPlayerLocation = new Vector2(2, -59);
        defaultPlayerScene = "Grandpa's Farm";
        SaveData(data);
    }

    private void OnApplicationQuit()
    {
        DataPersistenceManager.instance.SaveGame();
    }

    #endregion
}
