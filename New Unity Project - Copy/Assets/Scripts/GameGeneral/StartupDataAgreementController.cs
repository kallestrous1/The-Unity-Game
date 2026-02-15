using UnityEngine;

public class StartupDataAgreementController : MonoBehaviour
{

    public GameObject agreementPanel;
    public GameObject mainButtons;

    void Start()
    {
        if (DataPersistenceManager.instance.gameData.askedDataSetting == false)
        {
            DataPersistenceManager.instance.gameData.askedDataSetting = true;
            DataPersistenceManager.instance.SaveGame();
            agreementPanel.SetActive(true);
            mainButtons.SetActive(false);
        }
        else
        {
            agreementPanel.SetActive(false);
            mainButtons.SetActive(true);
        }
    }


}
