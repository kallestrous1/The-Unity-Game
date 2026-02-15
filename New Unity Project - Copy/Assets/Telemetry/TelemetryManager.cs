using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class TelemetryManager : MonoBehaviour
    
{
    public static TelemetryManager instance;

    private string telemetryEndpoint = "https://mitya-telemetry.duckdns.org/events";
    private string APIKey = "kort_onder_de_douche";

    List<TelemetryEvent> eventQueue = new List<TelemetryEvent>();

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            StartCoroutine(FlushEvents());
            DontDestroyOnLoad(gameObject);
            TelemetrySession.StartSession();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LogEvent(string type, string data, object payloadObj = null)
    {
        string payloadJson = null;

        if (payloadObj != null)
        {
            payloadJson = JsonUtility.ToJson(payloadObj);
        }

        eventQueue.Add(new TelemetryEvent
            {
                eventType = type,
                eventData = data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                playerId = DataPersistenceManager.instance.gameData.playerId,
            sessionId = TelemetrySession.SessionId,
            runId = DataPersistenceManager.instance.gameData.runId,
                    payload = payloadJson
        });
       
    }

    public void LogError(string data, object payloadObj = null)
    {
        string payloadJson = null;

        if (payloadObj != null)
        {
            payloadJson = JsonUtility.ToJson(payloadObj);
        }

        eventQueue.Add(new TelemetryEvent
        {
            eventType = "error",
            eventData = data,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            playerId = DataPersistenceManager.instance.gameData.playerId,
            sessionId = TelemetrySession.SessionId,
            runId = DataPersistenceManager.instance.gameData.runId,
            payload = payloadJson
        });

    }


    public void SendEventImmediate(TelemetryEvent evt)
    {
        StartCoroutine(SendEvent(evt));
    }
    IEnumerator FlushEvents()
    {
        while (true)
        {
            // Wait before flushing
            yield return new WaitForSeconds(5f);

            if(DataPersistenceManager.instance.gameData.allowDataCollection == false)
            {
                Debug.Log("Data collection disabled, skipping telemetry flush.");
                continue;
            }             

            // Nothing to send
            if (eventQueue.Count == 0)
                continue;

            // Copy queue so we don't block new events
            List<TelemetryEvent> batch = new List<TelemetryEvent>(eventQueue);
            eventQueue.Clear();

            string json = JsonUtility.ToJson(new TelemetryEventBatch(batch));
            byte[] body = Encoding.UTF8.GetBytes(json);
            Debug.Log(json);

            UnityWebRequest req = new UnityWebRequest(
                telemetryEndpoint,
                "POST"
            );

            req.uploadHandler = new UploadHandlerRaw(body);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.SetRequestHeader("X-API-Key", APIKey);

            yield return req.SendWebRequest();
            Debug.Log("POST done. result=" + req.result + " code=" + req.responseCode);
            Debug.Log("Response body=" + req.downloadHandler.text);
            Debug.Log("Server Fingerprint: " + req.GetResponseHeader("X-Telemetry-Server"));

            // If it failed, put events back in the queue
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Telemetry send failed");
                Debug.LogError("Result: " + req.result);
                Debug.LogError("Error: " + req.error);
                Debug.LogError("Response Code: " + req.responseCode);

                eventQueue.InsertRange(0, batch);
            }
            else
            {
                Debug.Log("Telemetry sent successfully");
            }
        }
    }

    private void OnApplicationQuit()
    {
        DataPersistenceManager.instance.gameData.sessionStarted = false;
        TelemetrySession.EndSession();
        StartCoroutine(FlushEventsOnquit());

    }

    IEnumerator  FlushEventsOnquit()
    {
        if(eventQueue.Count == 0)
            yield break;

        List<TelemetryEvent> batch = new List<TelemetryEvent>(eventQueue);
        eventQueue.Clear();

        string json = JsonUtility.ToJson(new TelemetryEventBatch(batch));
        byte[] body = Encoding.UTF8.GetBytes(json);
        Debug.Log(json);

        UnityWebRequest req = new UnityWebRequest(
            telemetryEndpoint,
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("X-API-Key", APIKey);

        yield return req.SendWebRequest();

        // If it failed, put events back in the queue
        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Telemetry send failed");
            Debug.LogError("Result: " + req.result);
            Debug.LogError("Error: " + req.error);
            Debug.LogError("Response Code: " + req.responseCode);
            Debug.LogError("Response Body: " + req.downloadHandler.text);

            eventQueue.InsertRange(0, batch);
        }
        else
        {
            Debug.Log("Telemetry sent successfully");
        }

    }

    IEnumerator SendEvent(TelemetryEvent evt)
    {
        string json = JsonUtility.ToJson(evt);
        byte[] body = System.Text.Encoding.UTF8.GetBytes(json);

        UnityWebRequest req = new UnityWebRequest(
            telemetryEndpoint,
            "POST"
        );

        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");

        yield return req.SendWebRequest();
    }

}


public static class TelemetrySession
{
    public static string SessionId { get; private set; }

    public static void StartSession()
    {
        SessionId = System.Guid.NewGuid().ToString();
        TelemetryManager.instance.LogEvent("Session", "session_start");
    }

    public static void EndSession()
    {
        TelemetryManager.instance.LogEvent("Session", "session_end",
            new SessionEndPayload
            {
                cause = "application_quit",
            });
        SessionId = null;
    }
}


[System.Serializable]
public class TelemetryEventBatch
{
    public List<TelemetryEvent> events;

    public TelemetryEventBatch(List<TelemetryEvent> events)
    {
        this.events = events;
    }
}

