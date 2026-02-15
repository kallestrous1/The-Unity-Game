using UnityEngine;

public class DataAgreementAllowanceButton : BasicButton
{

    public GameObject dataAgreementPanel;
    public GameObject settingsButtons;
    [SerializeField] public bool allowDataAgreement = false;

    public override void OnClicked()
    {
        base.OnClicked();
        DataPersistenceManager.instance.gameData.allowDataCollection = allowDataAgreement;
        Debug.Log("Data collection allowed: " + allowDataAgreement);
        settingsButtons.SetActive(true);
        dataAgreementPanel.SetActive(false);
    }

}
