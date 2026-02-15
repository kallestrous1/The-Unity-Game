using System.Collections;
using UnityEngine;

public class KnockBack : MonoBehaviour
{

    public float knockbackTime = 0.4f;
    public float extraInvincibilityTime = 0.2f;
    public float hitDirectionForce = 25f;
    public float constForce = 10f;
    public float freezeTime = 0.02f;
    

    public AnimationCurve knockbackForceCurve;

    private Rigidbody2D rb;
    private Collider2D playerCol;
    private Animator ani;

    private Coroutine knockbackCoroutine;

    public bool isBeingKnockedBack { get; private set; }
    public bool isInvincible { get; private set; }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerCol = GetComponent<Collider2D>();
        ani = GetComponent<Animator>();
    }

    public IEnumerator KnockbackAction(Vector2 hitDirection)
    {
        Debug.Log("Knockback started");
        isBeingKnockedBack = true;
        isInvincible = true;
        Physics2D.IgnoreLayerCollision(9, 10, true);
        ani.SetTrigger("Hurt");

        Vector2 _hitForce;
        Vector2 _constantForce;
        Vector2 _knockbackForce;
        Vector2 _combinedForce;
        float _time = 0f;

        _constantForce = Vector2.up * constForce;

        if (hitDirection.x > 0)
        {
            hitDirection = Vector2.right;
        }
        else if (hitDirection.x < 0)
        {
            hitDirection = Vector2.left;
        }

        float _elapsedTime = 0f;
        yield return new WaitForSeconds(freezeTime);
        while (_elapsedTime < knockbackTime)
        {
            _elapsedTime += Time.fixedDeltaTime;
            _time += Time.fixedDeltaTime;

            _hitForce = hitDirection * hitDirectionForce;

            _knockbackForce = _hitForce + _constantForce;

            
            _combinedForce = _knockbackForce * knockbackForceCurve.Evaluate(_time);
            

            rb.AddForce(_combinedForce, ForceMode2D.Impulse);

            yield return new WaitForFixedUpdate();
        }
        _elapsedTime = 0f;
        StopPlayerKnockback();
        while ( _elapsedTime < extraInvincibilityTime)
        {
            _elapsedTime += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }
        StopPlayerInvincibility();
    }

    public void StartPlayerKnockback(Vector2 hitDirection)
    {
        if (!isBeingKnockedBack && !isInvincible)
        {
            knockbackCoroutine = StartCoroutine(KnockbackAction(hitDirection));
        }
        else
        {
            Debug.Log("Player is already being knocked back");
        }
       
    }

    public void StopPlayerKnockback()
    {
        isBeingKnockedBack = false;       
    }

    public void StartPlayerInvincibility()
    {
        isInvincible = true;
        Physics2D.IgnoreLayerCollision(9, 10, true);
    }

    public void StopPlayerInvincibility()
    {
        isInvincible = false;
        Physics2D.IgnoreLayerCollision(9, 10, false);
    }


}
