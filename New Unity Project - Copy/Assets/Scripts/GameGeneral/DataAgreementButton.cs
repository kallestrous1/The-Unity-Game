using UnityEngine;

public class DataAgreementButton : BasicButton
{

    public GameObject dataAgreementPanel;
    public GameObject settingsButtons;

    public override void OnClicked()
        {
            base.OnClicked();
            dataAgreementPanel.SetActive(true);
            settingsButtons.SetActive(false);
    }
}
