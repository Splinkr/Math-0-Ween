using System.Collections;
using UnityEngine;

public class PumpkinReceiver : MonoBehaviour
{
    [Header("Mage Reference")]
    // Assign the mage’s controller via the Inspector.
    public MageController mageController;

    [Header("Animation Settings")]
    // Reference to this object's Animator (assign via the Inspector).
    public Animator animator;

    [Header("Death Animation Settings")]
    // Duration of the death animation (in seconds).
    public float deathAnimationDuration = 1.0f;

    // Counter for the number of invincible hits remaining.
    private int invincibleHitCount = 0;

    /// <summary>
    /// Handles collisions with falling operation objects.
    /// If the pumpkin is invincible, it decrements the counter.
    /// Otherwise, it calls the mage’s damage function.
    /// Note: Destruction after mage life is 0 is now handled externally.
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Operation"))
        {
            // Destroy the falling operation object immediately.
            Destroy(other.gameObject);

            if (invincibleHitCount > 0)
            {
                invincibleHitCount--;
                Debug.Log("Pumpkin is invincible. Remaining hits: " + invincibleHitCount);
            }
            else
            {
                Debug.Log("Operation hit: Mage receives damage.");
                if (mageController != null)
                {
                    mageController.HandleEnemyHitOnMage();
                }
                else
                {
                    Debug.LogWarning("MageController reference is missing!");
                }
            }
        }
    }

    /// <summary>
    /// Plays the death animation and destroys this GameObject after the animation completes.
    /// This method is intended to be called from another script (e.g., the Mage script when mage life reaches 0).
    /// </summary>
    public void DestroyAfterAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("Death", true);
            Debug.Log("Death animation triggered externally; will destroy object after " + deathAnimationDuration + " seconds.");
            StartCoroutine(WaitForDeathAndDisappear());
        }
        else
        {
            Debug.LogWarning("Animator reference is missing; destroying object immediately.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Waits for the duration of the death animation and then destroys this GameObject.
    /// </summary>
    private IEnumerator WaitForDeathAndDisappear()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }

    /// <summary>
    /// Activates invincibility, allowing the pumpkin to absorb the specified number of hits without affecting the mage.
    /// </summary>
    /// <param name="hits">The number of hits during which the pumpkin remains invincible.</param>
    public void ActivateInvincibility(int hits)
    {
        invincibleHitCount = hits;
        Debug.Log("Pumpkin invincibility activated for " + hits + " hit(s).");
    }
}