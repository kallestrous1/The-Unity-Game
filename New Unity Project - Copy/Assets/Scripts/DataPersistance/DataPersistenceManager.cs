using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : MonoBehaviour
{
    [Header("File Storage Configuration")]

    [SerializeField] private string fileName = "saveData.json";

    public static DataPersistenceManager instance { get; private set; }
    public bool IsLoading { get; private set; }


    private FileDataHandler dataHandler;
    private List<IDataPersistence> dataPersistanceObjects;
    private readonly List<IDataPersistence> registeredObjects = new List<IDataPersistence>();
    public int RegisteredCount => registeredObjects.Count;
    public IEnumerable<IDataPersistence> GetRegisteredObjects()
    {
        return registeredObjects;
    }

    public GameData gameData;
    private bool saveRequested = false;
    public bool isNewGame = false;



    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Creating a second data persistence manager (not good)");
            Destroy(gameObject);
            return;
        }       
         
        instance = this;

        this.dataHandler = new FileDataHandler(Application.persistentDataPath, fileName);

    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        EventManager.OnItemStateChanged += UpdateItemState;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EventManager.OnItemStateChanged -= UpdateItemState;

    }
    public IEnumerable<IEntityPersistence> GetEntityPersistenceObjects()
    {
        return registeredObjects.OfType<IEntityPersistence>();
    }

    public IEnumerable<IDataPersistence> GetGlobalPersistenceObjects()
    {
        return registeredObjects.Where(o => !(o is IEntityPersistence));
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(LoadAfterSceneLoaded());
    }

    private IEnumerator LoadAfterSceneLoaded()
    {
        yield return new WaitUntil(() => registeredObjects.Count > 0);
        LoadGame();
    }

    public void NewGame()
    {
        Debug.Log("Starting new game");
        isNewGame = true;
        if(gameData != null)
        {
            GameData oldData = gameData;
            this.gameData = new GameData();
            gameData.askedDataSetting = oldData.askedDataSetting;
            gameData.allowDataCollection = oldData.allowDataCollection;
            gameData.hasSeenIntro = oldData.hasSeenIntro;
        }
        else
        {
            this.gameData = new GameData();
        }

        if (!gameData.sessionStarted)
        {
            gameData.sessionStarted = true;
            TelemetrySession.StartSession();
        }
        //   FindAllDataPersistenceObjects();
        foreach (var obj in registeredObjects.ToArray())
        {
            obj?.ResetData(gameData);
        }
        SaveGame();
        isNewGame = false;
       
    }

    public void PrepareForSceneChange()
    {
        if (IsLoading) return;

        Debug.Log("PrepareForSceneChange: forcing states save");
        SaveGame();
    }



    public void LoadGame(bool startup = false)
    {
        IsLoading = true;

        if (!dataHandler.HasValidSave())
        {
            Debug.Log("No data found, creating new game");
            IsLoading = false;
            NewGame();
            return;
        }

        this.gameData = dataHandler.Load();
        if (!gameData.sessionStarted)
        {
            gameData.sessionStarted = true;
            TelemetrySession.StartSession();
        }

        if (string.IsNullOrEmpty(gameData.playerId))
        {
#if UNITY_EDITOR
            gameData.playerId = "DEV_PLAYER_EDITOR";
#else
        gameData.playerId = Guid.NewGuid().ToString();
#endif
        }

        Debug.Log("loading game");



        var snapshot = registeredObjects.ToArray();

        foreach (var obj in snapshot)
        {
            if (obj != null)
                obj.LoadData(gameData); 
        }

        IsLoading = false;

        if (startup)
        {
            NewManager.manager.BootStrapAfterDataLoaded();
        }

    }

    public void SaveGame()
    {
        Debug.Log("Saving game");
        var snapshot = registeredObjects.ToArray();

        foreach (var obj in snapshot)
        {
            obj?.SaveData(gameData);
        }
        dataHandler.Save(gameData);
    }
    public static void Register(IDataPersistence obj)
    {
        if (instance == null) return;
        if (!instance.registeredObjects.Contains(obj))
        {
            instance.registeredObjects.Add(obj);
        }
            
    }

    public static void Unregister(IDataPersistence obj)
    {
        if (instance == null)
        {
            Debug.LogWarning("No Data Persistence Manager instance found when trying to unregister object: " + obj);
            return;
        }
        if (instance.IsLoading)
            return;
        instance.registeredObjects.Remove(obj);
    }

    private void LateUpdate()
    {
        if (saveRequested)
        {
            SaveGame();
            saveRequested = false;
        }
    }
    private void UpdateItemState(string itemID, bool state)
    {
        if (IsLoading)
        {
            return;
        }
        gameData.itemStates[itemID] = state;
        saveRequested = true;
    }

/*    private List<IDataPersistence> FindAllDataPersistenceObjects()
    {
        IEnumerable<IDataPersistence> dataPersistenceObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None).OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
     }*/


    private void OnApplicationQuit() =>  SaveGame();

}
