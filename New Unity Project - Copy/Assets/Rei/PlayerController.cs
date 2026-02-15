using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : DataPersistenceBehaviour
{

    public int totalJumps = 0;

    Rigidbody2D rb;
    Animator ani;
    public float speed;
    public float jumpHeight;
    public float lowJumpMultiplier = 2f;
    float xInput;

    bool isGrounded = false;
    public Transform isGroundedChecker;
    public float checkGroundRadius;
    public LayerMask groundLayer;

    public int JUMPCOUNT;
    public int jumpCount;
    bool jumpRequest = false;
    bool boost;

    public float coyoteTime = 0.2f;
    public float coyoteTimer;
    bool coyoteJump;

    public float jumpBufferTime;
    float jumpBufferCounter;

    float rotationX;
    public float dashTime;
    public float dashAttackTime;
    bool isDashing;
    bool dashRequest;
    public float dashPower;
    public float dashAttackSpeed;
    public float DASHCOUNT;
    float dashCount;

    public float recoilMagnitude = 500;

    bool flipped = false;

    PlayerHealth playerHealth;
    PlayerAttacks playerAttacks;
    PlayerSound playerSound;

    public AudioClip dashSound;

    public GameObject doubleJumpEffect;

    private KnockBack knockback;

    private bool checkBoost;
    private bool isFrozen;



    void Start()
    {

        rb = this.gameObject.GetComponent<Rigidbody2D>();
        ani = this.gameObject.GetComponent<Animator>();
        playerHealth = this.gameObject.GetComponent<PlayerHealth>();
        playerAttacks = this.gameObject.GetComponent<PlayerAttacks>();
        knockback = this.gameObject.GetComponent<KnockBack>();
        this.playerSound = this.gameObject.GetComponent<PlayerSound>();
        this.transform.position = NewManager.manager.defaultPlayerLocation;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
       InputManager.Instance.OnJumpPressed += HandleJumpPressed;
        InputManager.Instance.OnJumpReleased += HandleJumpReleased;

        InputManager.Instance.OnDashPressed += HandleDashPressed;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        InputManager.Instance.OnJumpPressed -= HandleJumpPressed;
        InputManager.Instance.OnJumpReleased -= HandleJumpReleased;

        InputManager.Instance.OnDashPressed -= HandleDashPressed;
    }


    #region InputHandlers
    private void HandleJumpPressed()
    {
        jumpRequest = true;
        checkBoost = true;
    }

    private void HandleDashPressed()
    {
        if (!isDashing)
        {
            dashRequest = true;
        }
    }

    private void HandleJumpReleased()
    {
        checkBoost = false;
        boost = false;
    }

    #endregion

    private void Update()
    {
   /*     if (GameStateManager.instance.gameState != GameState.Play)
        {
            ani.SetBool("IsDashing", false);
            rb.linearVelocity = new Vector2(0, 0);
            return;
        }*/

        if (knockback.isBeingKnockedBack) return;

        if(!playerAttacks.isAttacking && isFrozen)
        {
            UnfreezeMidAir();
        }

        CheckIfGrounded();
        xInput = InputManager.Instance.Move;
        ani.SetBool("IsDashing", isDashing);

        if (checkBoost)
        {
            if (rb.linearVelocityY >0 && !isDashing && !isGrounded)
            {
                boost = true;
            }
            else
            {
                boost = false;
            }
        }
    }

    void FixedUpdate()
    {
        if (DialogueManager.getInstance().dialogueIsPlaying) return;

        if (knockback.isBeingKnockedBack) return;

        XMovement();

        if (isGrounded == true)
        {
            ani.SetFloat("MoveX", Mathf.Abs(xInput));
            ani.SetFloat("MoveY", 0);
            dashCount = DASHCOUNT;
            jumpCount = JUMPCOUNT;
            coyoteTimer = coyoteTime;
        }
        else
        {
            if (rb.linearVelocity.y > 0)
            {
                ani.SetFloat("MoveY", 1);
                ani.SetFloat("MoveX", 0);
            }
            else
            {
                ani.SetFloat("MoveY", 0);
                ani.SetFloat("MoveX", 0);
            }
            coyoteTimer -= Time.deltaTime;
        }

        if (jumpRequest)
        {
            jumpRequest = false;
            Jump();
            coyoteTimer = 0;
            jumpRequest = false;
        }
        if (boost)
        {
            rb.AddForce(new Vector2(0, lowJumpMultiplier), ForceMode2D.Impulse);
        }
        if (dashRequest)
        {
            dashRequest = false;
            StartCoroutine(Dash(dashPower, dashTime));
        }
    }

    #region XMovementandRotation
    void XMovement()
    {

        if (xInput > 0)
        {
            if (!flipped)
            {
                flipped = true;
                Vector2 currentPosition = this.transform.position;
                this.transform.position = new Vector2(currentPosition.x + 1, currentPosition.y);
                this.transform.Rotate(0f, 180f, 0f);
            }
            rotationX = 1;
        }
        else if (xInput < 0)
        {
            if (flipped)
            {

                flipped = false;

                Vector2 currentPosition = this.transform.position;
                this.transform.position = new Vector2(currentPosition.x - 1, currentPosition.y);
                this.transform.Rotate(0f, -180f, 0f);
            }
            rotationX = -1;
        }
        if (!isDashing)
        {
            if (isGrounded)
            {
                rb.AddForce(new Vector2(xInput * speed, 0), ForceMode2D.Impulse);
            }
            else
            {
                rb.AddForce(new Vector2(xInput * speed * 0.5f, 0), ForceMode2D.Impulse);
            }
        }
    }
    #endregion

    #region Dash

    //start player dash regardless of dashcount
    public void StartPlayerDash()
    {
        dashCount++;
        StartCoroutine(Dash(dashAttackSpeed, dashAttackTime));
    }

    //dash movement handler
    IEnumerator Dash(float speed, float time)
    {
        if (dashCount > 0)
        {
            dashCount--;
            playerSound.PlayDashSound();
            isDashing = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(rotationX * speed, 0), ForceMode2D.Impulse);
            float gravity = rb.gravityScale;
            rb.gravityScale = 0;
            yield return new WaitForSeconds(time);
            isDashing = false;
            rb.gravityScale = gravity;
        }
    }
    #endregion

    #region Jump
    public void Jump()
    {
        if ((isGrounded || coyoteTimer > 0) && isDashing == false)
        {
            playerSound.PlayJumpSound();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpHeight), ForceMode2D.Impulse);
            totalJumps++;
        }
        else if (jumpCount > 0 && isDashing == false)
        {
            jumpCount--;
            if (doubleJumpEffect)
            {
                Instantiate(doubleJumpEffect, isGroundedChecker.position, Quaternion.identity);
            }
            ani.SetTrigger("DoubleJump");
            playerSound.PlayExtraJumpSound();
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0);
            rb.AddForce(new Vector2(0, jumpHeight), ForceMode2D.Impulse);
        }
    }
    #endregion

    #region CheckIfGrounded
    void CheckIfGrounded()
    {
        Collider2D collider = Physics2D.OverlapCircle(isGroundedChecker.position, checkGroundRadius, groundLayer);
        if (collider != null)
        {
            isGrounded = true;
        }
        else
        {
            isGrounded = false;
        }
    }
    #endregion

    public void FreezeMidAir()
    {
        isFrozen = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
    }

    public void UnfreezeMidAir()
    {
        isFrozen = false;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    public void SetToSavedLocation()
    {
        this.transform.position = NewManager.manager.defaultPlayerLocation;
    }

    public override void LoadData(GameData data)
    {
        this.totalJumps = data.totalJumps;
    }

    public override void SaveData(GameData data)
    {
        data.totalJumps = this.totalJumps;
    }

    public override void ResetData(GameData data)
    {
        data.totalJumps = 0;
    }

}