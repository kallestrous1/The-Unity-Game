using UnityEngine;

[CreateAssetMenu(fileName = "RegenCharmOne", menuName = "Inventory/Charms/RegenCharmOne")]

public class RegenCharmOne : CharmObject
{
    PlayerHealth playerHealth;
    public override void EquipItem()
    {
        base.EquipItem();
        playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        playerHealth.regenRate += 1;
    }

    public override void UnequipItem()
    {
        base.UnequipItem();
        if(playerHealth == null)
        {
            playerHealth = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerHealth>();
        }
        playerHealth.regenRate -= 1;
    }
}
