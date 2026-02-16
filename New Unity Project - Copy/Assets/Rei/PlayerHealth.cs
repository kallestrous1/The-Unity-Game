using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class PlayerHealth : MonoBehaviour
{


    [Header("Health Settings")]
    [SerializeField] private int startingHealth = 15;
    [SerializeField] public int maxHealth = 20;
    [SerializeField] private float invincibilityDuration = 1f;
    [SerializeField] public int regenRate = 0; // health per second
    [SerializeField] private float regenDelay = 3f;
    [SerializeField] private int health;


    [Header("UI")]
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Slider maxHealthSlider;
    [SerializeField] private RectTransform maxHealthRectTransform;
    private Vector2 maxHealthSliderInitialSize;
    private float initialWidth;

    [Header("Displays")]
    [SerializeField] private GameObject bloodDisplay;
    [SerializeField] private GameObject deathDisplay;

    [Header("Audio")]
    [SerializeField] private AudioClip playerDamageSound;
    [SerializeField] private AudioClip playerDeathSound;

    private float invincibilityTimer;
    private bool died = false;
    private bool recovering = false;
    private KnockBack knockback;

    public int CurrentHealth => health;
    public int MaxHealth => maxHealth;
    public bool IsInvincible => invincibilityTimer > 0;

    public event System.Action<int, int> OnHealthChanged;

    // Start is called before the first frame update
    void Start()
    {
        health = maxHealth;
        knockback = GetComponent<KnockBack>();
        if (maxHealthRectTransform != null)
        {
            maxHealthSliderInitialSize = maxHealthRectTransform.sizeDelta;
            initialWidth = maxHealthRectTransform.rect.width;
        }
        StartCoroutine(RegenerateHealth());
        UpdateUI();
        /* health = maxHealth;
         knockback = this.gameObject.GetComponent<KnockBack>();
         healthSlider.maxValue = maxHealth;
         healthSlider.value = health;
         maxHealthRectTransform = maxHealthSlider.GetComponent<RectTransform>();
         maxHealthSliderInitialSize = maxHealthRectTransform.sizeDelta;*/
    }
    private void Update()
    {
        if (invincibilityTimer > 0)
        {
            invincibilityTimer -= Time.deltaTime;
            recovering = true;
        }
        else
        {
            recovering = false;
        }
    }

    private IEnumerator RegenerateHealth()
    {
        while (true) {
            yield return new WaitForSeconds(regenDelay);
            if (regenRate != 0)
            {
                if (health < maxHealth)
                {
                    changeHealth(regenRate);
                }
            }
        }
    }

    public void ResetHealth()
    {
        died = false;
        health = maxHealth;
        UpdateUI();
    }
    public void changeMaxHealth(int change)
    {
        maxHealth += change;
        health = Mathf.Min(health, maxHealth);
        UpdateUI();
       /* maxHealthSlider.maxValue = maxHealth;
        maxHealthSlider.value = maxHealth;
        UpdateSliderSize(maxHealth);*/
    }

    public void changeHealth(int amount, string cause = "unknown")
    {

        if (IsInvincible && amount < 0) return;

        health += amount;
        health = Mathf.Clamp(health, 0, maxHealth);
        UpdateUI();

        if (amount < 0) invincibilityTimer = invincibilityDuration;

        if (health <= 0 && !died)
        {
            died = true;
            AudioManager.Instance.Play(playerDeathSound);
            StartCoroutine(DeathRoutine(cause));
        }
        else if (amount < 0)
        {
            AudioManager.Instance.Play(playerDamageSound);
            TelemetryManager.instance.LogEvent(
                "PlayerDamaged",
                "Player took damage. " +
                "Scene:_" + SceneManager.GetActiveScene().name 
                + ", Location: " + this.gameObject.transform.position,
                new PlayerDamagedPayload {
                    source = cause,
                    sceneName = SceneManager.GetActiveScene().name,
                    damagePosition = this.gameObject.transform.position
                });
            StartCoroutine(BloodFlash());
        }

        OnHealthChanged?.Invoke(health, maxHealth);
    }

    private IEnumerator DeathRoutine(string cause)
    {
        deathDisplay.SetActive(true);

        TelemetryManager.instance.LogEvent(
            "Death", 
            "Player died. " +
            "Scene:_" + SceneManager.GetActiveScene().name 
            + ", Location: " + this.gameObject.transform.position,
            new PlayerDeathPayload { causeOfDeath = cause,
            sceneName = SceneManager.GetActiveScene().name,
            deathPosition = this.gameObject.transform.position
            });
        yield return new WaitForSeconds(1f);
        deathDisplay.SetActive(false);
        PlayerRespawnManager.Instance.RespawnPlayer();
    }

    private IEnumerator BloodFlash()
    {
        bloodDisplay.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        bloodDisplay.SetActive(false);
    }

    public int getHealth()
    {
        return health;
    }

    private void UpdateUI()
    {
        if (healthSlider)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = health;
        }

        if (maxHealthSlider)
            maxHealthSlider.maxValue = maxHealth;

        UpdateMaxHealthUI();
    }

    public void UpdateMaxHealthUI()
    {
        if (maxHealthRectTransform == null) return;

        float scaleFactor = (float)maxHealth / startingHealth;
        /*   Vector2 newSize = new Vector2(
               maxHealthSliderInitialSize.x * scaleFactor,
               maxHealthSliderInitialSize.y
           );

           maxHealthRectTransform.sizeDelta = newSize;*/
        float newWidth = initialWidth * scaleFactor;

        maxHealthRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
    }
}
