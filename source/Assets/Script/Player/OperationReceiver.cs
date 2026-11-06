using System.Collections;
using UnityEngine;

public class OperationReceiver : MonoBehaviour
{
    [Header("Shield Settings")]
    // How many hits the shield can absorb before it breaks.
    public int requiredHits = 2;
    // Current number of hits absorbed.
    private int currentHits = 0;

    [Header("Mage Reference")]
    // Assign the mage’s controller (or damage receiver) via the Inspector.
    public MageController mageController;

    [Header("Animation Settings")]
    // Reference to this object's Animator (assign via the Inspector).
    public Animator animator;

    [Header("Death Animation Settings")]
    // Duration of the death animation (in seconds). Adjust as needed.
    public float deathAnimationDuration = 1.0f;

    // This 2D trigger will detect falling operation objects.
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check for the falling operation by tag.
        if (other.CompareTag("Operation"))
        {
            // If the shield is still active, it absorbs the hit.
            if (currentHits < requiredHits)
            {
                currentHits++;
                Debug.Log($"Shield absorbed hit {currentHits}/{requiredHits}.");
                // Destroy the fallen operation object.
                Destroy(other.gameObject);

                // Optionally, you can play a shield-hit animation or other feedback here.
                if (currentHits >= requiredHits)
                {
                    Debug.Log("Shield is now broken; further hits will damage the mage.");
                }
            }
            else
            {
                // Shield is broken: collisions now hurt the mage.
                Debug.Log("Broken shield: Operation now damages the mage.");

                // Call the mage's damage function.
                if (mageController != null)
                {
                    mageController.HandleEnemyHitOnMage();
                }
                else
                {
                    Debug.LogWarning("MageController reference is missing on the OperationReceiver!");
                }
                // In all cases, destroy the falling operation.
                Destroy(other.gameObject);
            }
        }
    }

    /// <summary>
    /// Triggers the Death animation by setting the "Death" boolean parameter in the Animator.
    /// After the animation completes (based on deathAnimationDuration), the object is destroyed.
    /// </summary>
    /// <returns>
    /// True if the animation was successfully triggered; otherwise, false (if Animator reference is missing).
    /// </returns>
    public bool TriggerDeathAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("Death", true);
            Debug.Log("OperationReceiver: Death animation triggered.");
            StartCoroutine(WaitForDeathAndDisappear());
            return true;
        }
        else
        {
            Debug.LogWarning("OperationReceiver: Animator reference is missing.");
            return false;
        }
    }

    /// <summary>
    /// Waits for the death animation to finish (using deathAnimationDuration) 
    /// and then destroys this GameObject.
    /// </summary>
    private IEnumerator WaitForDeathAndDisappear()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        Destroy(gameObject);
    }
}