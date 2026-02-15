using UnityEngine;

public class PlayerSound : MonoBehaviour
{
    public AudioClip jumpSound;
    public AudioClip dashSound;
    public AudioClip extraJumpSound;
    public AudioClip footStepOne;
    public AudioClip footStepTwo;

    public AudioClip pickUpItemSound;

    public AudioClip chargeAttackSound;

    public void PlayChargeAttackSound()
    {
        AudioManager.Instance.Play(chargeAttackSound);
    }

    public void PlayPickUpItem()
    {
        AudioManager.Instance.Play(pickUpItemSound);
    }

    public void PlayFootStepOne()
    {
        if (footStepOne)
        {
            AudioManager.Instance.Play(footStepOne);
        }
    }

    public void PlayFootStepTwo()
    {
        if (footStepTwo)
        {
            AudioManager.Instance.Play(footStepTwo);
        }
    }

    public void PlayJumpSound()
    {
        if (jumpSound)
        {
            AudioManager.Instance.Play(jumpSound);
        }
    }

    public void PlayExtraJumpSound()
    {
        if (extraJumpSound)
        {
            AudioManager.Instance.Play(extraJumpSound);
        }
    }

    public void PlayDashSound()
    {
        if (dashSound)
        {
            AudioManager.Instance.Play(dashSound);
        }
    }


}
