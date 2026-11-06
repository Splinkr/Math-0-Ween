using System.Collections;
using System;
using UnityEngine;

public class BatProjectile : MonoBehaviour
{
    // The target transform (the enemy) that the bat moves toward.
    public Transform target;
    // The speed at which the bat moves.
    public float speed = 10f;
    // When the bat is within this distance from the target, it triggers the explosion.
    public float destroyThreshold = 0.2f;
    // Duration of the explosion/death animation.
    public float deathAnimationDuration = 0.5f;

    // Flag to prevent multiple explosion triggers.
    private bool isExploding = false;
    private bool explosionStarted;
    // Cache reference to the Animator on the bat.
    private Animator animator;

    internal enum Completion { Impact, TargetLost, Cancelled }
    private Action<BatProjectile, Completion> completion;
    private bool completionSent;
    private Action<BatProjectile, Transform> impact;
    private bool impactSent;

    internal void TrackImpact(Action<BatProjectile, Transform> callback)
    {
        // Contact is never replayed for a late subscriber.
        if (!completionSent && !explosionStarted && isActiveAndEnabled) impact = callback;
    }

    internal void TrackCompletion(Action<BatProjectile, Completion> callback)
    {
        if (completionSent) { callback?.Invoke(this, Completion.Cancelled); return; }
        completion = callback;
        if (!isActiveAndEnabled) Complete(Completion.Cancelled);
    }

    private void Complete(Completion result)
    {
        if (completionSent) return;
        completionSent = true;
        impact = null;
        var callback = completion;
        completion = null;
        callback?.Invoke(this, result);
    }

    internal void CancelCollectiveProjectile()
    {
        CancelProjectile();
    }

    internal void CancelProjectile()
    {
        StopAllCoroutines();
        target = null;
        Complete(Completion.Cancelled);
        if (this != null) Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (completion != null || impact != null) { StopAllCoroutines(); target = null; Complete(Completion.Cancelled); }
    }

    private void OnDestroy() { Complete(Completion.Cancelled); }


    [Header("Audio Settings")]
    public AudioClip explosionSound;            // Assign your pain sound here via the Inspector.
    public AudioSource batAudioSource;  // Assign an AudioSource component here to control volume, pitch, etc.


    void Start()
    {
        // Cache the Animator attached to the bat.
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // If the explosion sequence has started, skip movement.
        if (isExploding)
            return;

        // If there is no target, destroy the projectile.
        if (target == null)
        {
            Debug.Log("Target lost. Destroying projectile at: " + transform.position);
            if (this == null || completionSent) return;
            Complete(Completion.TargetLost);
            if (this != null) Destroy(gameObject);
            return;
        }

        if (Time.timeScale == 0f)
            return;

        // Move the bat toward the enemy.
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);

        // Check if the bat has arrived at the target.
        if (Vector3.Distance(transform.position, target.position) <= destroyThreshold)
        {
            isExploding = true;
            StartCoroutine(ExplodeAndRemoveQuestion());
        }
    }

    IEnumerator ExplodeAndRemoveQuestion()
    {
        if (explosionStarted || completionSent) yield break;
        explosionStarted = true;
        isExploding = true;
        // Public target can change while the explosion plays. Only the contacted enemy belongs to this impact.
        Transform impactTarget = target;
        if (impactTarget == null)
        {
            Complete(Completion.TargetLost);
            if (this != null) Destroy(gameObject);
            yield break;
        }
        // Award individual points at actual contact, before animation/log callbacks can end the match.
        if (!impactSent)
        {
            impactSent = true;
            var callback = impact;
            impact = null;
            callback?.Invoke(this, impactTarget);
        }
        if (this == null || completionSent) yield break;
        // Trigger the explosion/death animation.
        if (animator != null)
        {

            animator.SetTrigger("Death");
            if (this == null || completionSent) yield break;
            // Play the explosion sound if assigned.
            if (batAudioSource != null && explosionSound != null)
            {
                batAudioSource.PlayOneShot(explosionSound);
            }
            Debug.Log("Explosion triggered at position: " + transform.position);
        }
        else
        {
            Debug.LogWarning("No Animator found on the bat object!");
        }

        if (this == null || completionSent) yield break;
        // Wait for the explosion animation to finish.
        yield return new WaitForSeconds(deathAnimationDuration);

        if (this == null || completionSent) yield break;
        // Now, deactivate or remove the target enemy.
        bool impacted = false;
        if (impactTarget != null)
        {
            Debug.Log("Enemy destroyed at position: " + impactTarget.position);
            if (this == null || completionSent) yield break;
            if (impactTarget != null)
            {
                Destroy(impactTarget.gameObject);
                impacted = true;
            }
        }
        Complete(impacted ? Completion.Impact : Completion.TargetLost);

        // Destroy the projectile.
        if (this != null) Destroy(gameObject);
    }
}
