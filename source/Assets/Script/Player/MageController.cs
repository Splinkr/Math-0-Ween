using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;  // Import the UI namespace for Slider

public class MageController : MonoBehaviour
{
    [Header("Animation Settings")]
    public Animator animator;                  // Reference to the mage's Animator.
    public float batAnimationDuration = 0.5f;    // Duration of the bat attack animation.

    [Header("Health Settings")]
    public int maxLives = 4;                   // Mage starts with a number of lives.
    private int currentLives;                  // Current lives remaining.
    public int CurrentLives { get { return currentLives; } }  // Public property to access current lives.
    public bool DeathMage { get; private set; } = false;  // Indicates whether the mage is dead.

    // Public slider to display mage's health (range 0 to 7).
    public Slider mageHealthSlider;

    [Header("Game Over Settings")]
    public OperationEnemySpawner enemySpawner; // Reference to the enemy spawner that holds the TriggerGameOver method.

    [Header("Death Animation Settings")]
    public float deathAnimationDuration = 1.0f;  // Duration of the death animation (in seconds).

    [Header("Audio Settings")]
    public AudioClip painSound;            // Assign your pain sound here via the Inspector.
    public AudioSource damageAudioSource;  // Assign an AudioSource component here to control volume, pitch, etc.

    [SerializeField, Tooltip("Initial maximum lives, applied once. No config keeps the legacy maxLives value; asset edits take effect next game.")]
    private GameBalanceConfig balanceConfig;
    private bool balanceInitialized;
    private int capturedMaxLives;
    internal string LifeConfiguration => balanceInitialized ? new MatchConfiguration().Add("maxLives", capturedMaxLives).Description : null;

    void Start()
    {
        InitializeBalance();
        currentLives = maxLives;
        // Initialize the slider if it's assigned.
        if (mageHealthSlider != null)
        {
            mageHealthSlider.minValue = 0;
            mageHealthSlider.maxValue = maxLives;
            mageHealthSlider.value = currentLives;
        }

        // Ensure the AudioSource is assigned. If not, try to get one from this GameObject.
        if (damageAudioSource == null)
        {
            damageAudioSource = GetComponent<AudioSource>();
            if (damageAudioSource == null)
            {
                Debug.LogWarning("MageController: No AudioSource assigned or found on the Mage object. Please add one to control volume and other audio settings.");
            }
        }
    }

    private void InitializeBalance()
    {
        if (balanceInitialized)
            return;
        balanceInitialized = true;
        if (balanceConfig != null)
            maxLives = balanceConfig.lives.maxLives;
        capturedMaxLives = maxLives;
    }

    //===========================================================================  
    // Bat Attack Animation Logic (for example, when the mage attacks)
    //===========================================================================  

    /// <summary>
    /// Triggers the bat attack animation using the "Bat" trigger.
    /// When finished, the onComplete callback (if provided) is invoked.
    /// </summary>
    public void PlayBatAttack(Action onComplete)
    {
        if (animator != null)
        {
            animator.SetTrigger("Bat");
            Debug.Log("MageController: Triggered 'Bat' animation.");
            StartCoroutine(WaitForBatAnimation(onComplete));
        }
        else
        {
            Debug.LogWarning("MageController: Animator not assigned!");
            onComplete?.Invoke();
        }
    }

    private IEnumerator WaitForBatAnimation(Action onComplete)
    {
        yield return new WaitForSeconds(batAnimationDuration);
        Debug.Log("MageController: Bat attack animation complete.");
        onComplete?.Invoke();
    }

    //===========================================================================  
    // Damage Handling Logic (called by the PumpkinReceiver or another part of your game when the mage is hit)
    //===========================================================================  

    /// <summary>
    /// Called when an enemy hit occurs. This decreases the mage's lives, plays a hit reaction,
    /// and if lives run out, triggers both the death animations and the game over sequence.
    /// </summary>
    public void HandleEnemyHitOnMage()
    {
        if (DeathMage || currentLives <= 0) return;
        currentLives--;
        if (enemySpawner != null) enemySpawner.NotifyLifeLost(currentLives <= 0);

        // Play the sound effect for taking damage.
        PlayDamageSound();

        // Update the slider value to reflect the new health.
        if (mageHealthSlider != null)
        {
            mageHealthSlider.value = currentLives;
        }

        // Reset the player's streak when a mage hit occurs.
        if (enemySpawner != null)
        {
            enemySpawner.ResetStreak();
        }
        else
        {
            Debug.LogWarning("OperationEnemySpawner reference not assigned! Cannot reset streak.");
        }

        if (currentLives > 0)
        {
            Debug.Log("Mage hit. Remaining lives: " + currentLives);
            if (animator != null)
            {
                animator.SetTrigger("HitMage");
            }
        }
        else
        {
            DeathMage = true;
            PointsManager.instance?.StopScoring();
            Debug.Log("Mage is dead.");

            // Trigger death animations for all PumpkinReceiver objects.
            PumpkinReceiver[] receivers = FindObjectsByType<PumpkinReceiver>(FindObjectsInactive.Exclude, FindObjectsSortMode.InstanceID);
            foreach (PumpkinReceiver receiver in receivers)
            {
                receiver.DestroyAfterAnimation();
                Debug.Log($"PumpkinReceiver '{receiver.gameObject.name}': Death animation triggered for destruction.");
            }

            // Trigger the mage's own death animation.
            if (animator != null)
            {
                animator.SetBool("DeathMage", true);
            }

            // Wait for the death animation to complete before triggering game over.
            StartCoroutine(GameOverAfterDeath());
        }
    }

    public void healMage()
    {
        if (currentLives < maxLives)
        {
            currentLives++;
            if (mageHealthSlider != null)
            {
                mageHealthSlider.value = currentLives;
            }
            Debug.Log("Mage healed. Current lives: " + currentLives);
        }
        else
        {
            Debug.Log("Mage is already at maximum health.");
        }
    }
    /// <summary>
    /// Plays the damage (pain) sound using the assigned AudioSource component.
    /// This allows you to control volume, pitch, and other properties via the AudioSource.
    /// </summary>
    private void PlayDamageSound()
    {
        if (damageAudioSource != null && painSound != null)
        {
            damageAudioSource.PlayOneShot(painSound);
        }
        else
        {
            Debug.LogWarning("MageController: Pain sound or damage AudioSource not assigned!");
        }
    }

    /// <summary>
    /// Waits for the death animation to complete before calling the game over routine.
    /// </summary>
    private IEnumerator GameOverAfterDeath()
    {
        yield return new WaitForSeconds(deathAnimationDuration);
        if (enemySpawner != null)
        {
            enemySpawner.TriggerGameOver();
        }
        else
        {
            Debug.LogWarning("MageController: enemySpawner not assigned!");
        }
    }
}
