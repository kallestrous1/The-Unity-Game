using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[System.Serializable]
public class GameData 
{
    private const string EDITOR_PLAYER_ID = "DEV_PLAYER_EDITOR";
    public string playerId;
    public string runId;
    public bool sessionStarted = false;

    public bool saveFileExists = false;
    public bool hasSeenIntro = false; 
    public bool askedDataSetting = false;
    public bool allowDataCollection = false;

    public int totalJumps;
    public  Vector2 playerSaveLocation;
    public  string playerSpawnScene = "Grandpa's Farm";
    public float playerMoney = 0f;

//    public List<ItemInGame> collectedItems;
    public List<int> itemsToDestroy;
    public SerializableDictionary<ItemObject, Vector2> activeItems;

    public SerializableDictionary<string, bool> stateChangingObjects;
    public SerializableDictionary<string, bool> itemStates;

    public float musicVolume = 1f;
    public float effectsVolume = 1f;

    public GameData()
    {
#if UNITY_EDITOR
        this.playerId = EDITOR_PLAYER_ID;
#else
        this.playerId = Guid.NewGuid().ToString();
#endif

#if UNITY_EDITOR
        this.runId = EDITOR_PLAYER_ID;
#else
        this.runId = Guid.NewGuid().ToString();
#endif
        this.saveFileExists = true;
        this.sessionStarted = false;
        this.totalJumps = 0;
        this.playerSaveLocation = new Vector2(2, -59);
        this.playerSpawnScene = "Grandpa's Farm";
        this.playerMoney = 0f;

      //  this.collectedItems = new List<ItemInGame>();
        this.itemsToDestroy = new List<int>();
        this.activeItems = new SerializableDictionary<ItemObject, Vector2>();
        
        this.stateChangingObjects = new SerializableDictionary<string, bool>();
        this.itemStates = new SerializableDictionary<string, bool>();
    }

}
