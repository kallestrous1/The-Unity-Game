using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New WeaponObject", menuName = "Inventory/Items/WeaponObject")]
public abstract class WeaponObject : ItemObject
{
    public GameObject swordForPlayer;
    public GameObject PlayerWeaponContainer;
    public Sprite weaponSprite;

    public int baseAttackDamage;
    public int upAttackDamage;
    public int downAttackDamage;

    public AudioClip swingSound;

    public GameObject hitParticleEffect;
    public GameObject baseSpellParticle;

    public float weaponForce = 10;
    public int baseSpellMagicCost = 0;

    public float lightAttackCommitTime = 0.4f;
    public float heavyAttackCommitTime = 0.8f;

    public void Awake()
    {
        type = ItemType.Weapon;                
    }


    public override void EquipItem()
    {
        base.EquipItem();
        Debug.Log("equipping item");
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<Animator>().runtimeAnimatorController = animations;

        PlayerWeaponContainer = GameObject.FindGameObjectWithTag("Weapon Container");
        PlayerWeaponContainer.GetComponentInChildren<SpriteRenderer>().sprite = weaponSprite;
        PlayerWeaponContainer.GetComponentInChildren<PlayerWeapon>().activeWeapon = this;
    }

    public override void UnequipItem()
    {
        base.UnequipItem();
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        player.GetComponent<Animator>().runtimeAnimatorController = baseAnimations;
        PlayerWeaponContainer = GameObject.FindGameObjectWithTag("Weapon Container");
        PlayerWeaponContainer.GetComponentInChildren<SpriteRenderer>().sprite = null;
        PlayerWeaponContainer.GetComponentInChildren<PlayerWeapon>().activeWeapon = PlayerWeaponContainer.GetComponentInChildren<PlayerWeapon>().defaultWeapon;
    }

    public virtual void CastBaseWeaponSpell() { }
}
