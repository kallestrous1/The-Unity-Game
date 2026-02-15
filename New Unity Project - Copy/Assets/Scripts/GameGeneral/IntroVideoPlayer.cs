using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{

    [ContextMenu("Reset Intro and pretend there's no save file")]
    void ResetIntro()
    {
        DataPersistenceManager.instance.gameData.hasSeenIntro = false;
        DataPersistenceManager.instance.gameData.saveFileExists = false;
        DataPersistenceManager.instance.gameData.askedDataSetting = false;
        DataPersistenceManager.instance.SaveGame();
    }

    public static IntroVideoPlayer instance;

    [SerializeField] private string IntroSceneName;
    [SerializeField] private string logoSceneName;
    [SerializeField] private string mainMenuSceneName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void PlayIntroSequence()
    {
        StartCoroutine(PlayIntroSequenceCoroutine());
    }

    public void PlayLogoSequence()
    {
        StartCoroutine(PlayLogoSequenceCoroutine());
    }

    public IEnumerator PlayLogoSequenceCoroutine()
    {
        SceneManager.LoadScene(logoSceneName, LoadSceneMode.Additive);
        yield return new WaitForSeconds(3.0f);
        FinishLogo();
    }

    public IEnumerator PlayIntroSequenceCoroutine()
    {
        SceneManager.LoadScene(IntroSceneName, LoadSceneMode.Additive);
        yield return new WaitForSeconds(3.0f); 
        FinishIntro();
    }

    public void SkipIntro()
    {
        FinishIntro();
    }

    public void FinishLogo()
    {
        if(!DataPersistenceManager.instance.gameData.hasSeenIntro)
        {
            PlayIntroSequence();
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Additive);
        }
            SceneManager.UnloadSceneAsync(logoSceneName);
    }

    public void FinishIntro()
    {
        var data = DataPersistenceManager.instance.gameData;
        data.hasSeenIntro = true;

        DataPersistenceManager.instance.SaveGame();
        NewManager.gameStarted = true;
        SceneManager.LoadScene(mainMenuSceneName, LoadSceneMode.Additive);
        SceneManager.UnloadSceneAsync(IntroSceneName);
    }


}
