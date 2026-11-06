using UnityEngine;

// Numeric state only: the facade owns clocks, coroutine scheduling and scene UI.
internal sealed class SpecialAbilityState
{
    private bool freezeActive = false;
    private float freezeTimer = 0f;
    private bool gravityReversed = false;
    private bool freezeAbilityReady = true;
    private bool gravityReverseAbilityReady = true;
    private int currentBatAttackBar = 0;

    internal bool FreezeActive { get => freezeActive; set => freezeActive = value; }
    internal float FreezeTimer => freezeTimer;
    internal bool GravityReversed { get => gravityReversed; set => gravityReversed = value; }
    internal bool FreezeAbilityReady { get => freezeAbilityReady; set => freezeAbilityReady = value; }
    internal bool GravityReverseAbilityReady { get => gravityReverseAbilityReady; set => gravityReverseAbilityReady = value; }
    internal int CurrentBatAttackBar => currentBatAttackBar;
    internal bool IsSpawningAllowed => !freezeActive && !gravityReversed && !collectiveActive;

    internal readonly struct CollectiveParameters
    {
        internal readonly float PostImpactDelay, Timeout;
        internal CollectiveParameters(GameBalanceConfig.AbilitySettings source, out bool valid)
        {
            float delay = source == null ? 2f : source.collectivePostImpactDelay;
            float timeout = source == null ? 10f : source.collectiveProjectileTimeout;
            bool validDelay = !float.IsNaN(delay) && !float.IsInfinity(delay) && delay >= 0f;
            bool validTimeout = !float.IsNaN(timeout) && !float.IsInfinity(timeout) && timeout > 0f;
            PostImpactDelay = validDelay ? delay : 2f;
            Timeout = validTimeout ? timeout : 10f;
            valid = validDelay && validTimeout;
        }
    }

    private bool collectiveActive;
    private int collectivePending;
    private double collectiveSafetyDeadline, collectiveReleaseDeadline;
    private float collectivePostImpactDelay;
    internal bool CollectiveActive => collectiveActive;
    internal int CollectivePending => collectivePending;
    internal bool CollectiveTimedOut(double now) => collectiveActive && collectivePending > 0 && now >= collectiveSafetyDeadline;

    internal bool BeginCollectiveAttack(int pending, double now, CollectiveParameters parameters)
    {
        if (collectiveActive || pending <= 0) return false;
        collectiveActive = true;
        collectivePending = pending;
        collectiveSafetyDeadline = now + parameters.Timeout;
        collectivePostImpactDelay = parameters.PostImpactDelay;
        return true;
    }

    // The facade resolves unique projectile tokens before calling this numeric transition.
    internal void ResolveCollectiveProjectile(double now)
    {
        if (!collectiveActive || collectivePending <= 0) return;
        if (--collectivePending == 0) collectiveReleaseDeadline = now + collectivePostImpactDelay;
    }

    internal void AdvanceCollectiveAttack(double now)
    {
        if (collectiveActive && collectivePending == 0 && now >= collectiveReleaseDeadline) ClearCollectiveAttack();
    }

    internal void ClearCollectiveAttack()
    {
        collectiveActive = false;
        collectivePending = 0;
        collectiveSafetyDeadline = collectiveReleaseDeadline = 0;
    }

    internal readonly struct AdrenalineParameters
    {
        internal readonly float Multiplier, Duration, Recovery, FadeIn, Intensity, Border;
        internal AdrenalineParameters(GameBalanceConfig.AdrenalineSettings source, out bool valid)
        {
            valid = true;
            Multiplier = Read(source == null ? 0.8f : source.speedMultiplier, 0f, 1f, 0.8f, ref valid);
            Duration = Read(source == null ? 3f : source.duration, 0f, float.MaxValue, 3f, ref valid);
            Recovery = Read(source == null ? 0.5f : source.recoveryDuration, 0f, float.MaxValue, 0.5f, ref valid);
            FadeIn = Read(source == null ? 0.15f : source.vignetteFadeIn, 0f, float.MaxValue, 0.15f, ref valid);
            Intensity = Read(source == null ? 0.2f : source.vignetteIntensity, 0f, 1f, 0.2f, ref valid);
            Border = Read(source == null ? 0.12f : source.vignetteBorder, 0f, 0.5f, 0.12f, ref valid);
        }
        private static float Read(float value, float min, float max, float fallback, ref bool valid)
        {
            if (!float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max) return value;
            valid = false;
            return fallback;
        }
    }

    private bool adrenalineActive;
    private double adrenalineElapsed;
    private float adrenalineStartOpacity;
    private AdrenalineParameters adrenaline;
    internal bool AdrenalineActive => adrenalineActive;
    private float AdrenalineRecovery => adrenalineElapsed < adrenaline.Duration ? 0f : adrenaline.Recovery <= 0f ? 1f
        : Mathf.SmoothStep(0f, 1f, (float)((adrenalineElapsed - adrenaline.Duration) / adrenaline.Recovery));
    internal float AdrenalineMultiplier => !adrenalineActive ? 1f
        : Mathf.Lerp(adrenaline.Multiplier, 1f, AdrenalineRecovery);
    internal float AdrenalineOpacity => !adrenalineActive ? 0f : Mathf.Lerp(adrenalineStartOpacity, adrenaline.Intensity,
        adrenaline.FadeIn <= 0f ? 1f : Mathf.SmoothStep(0f, 1f, (float)(adrenalineElapsed / adrenaline.FadeIn)))
        * (1f - AdrenalineRecovery);

    internal void BeginAdrenaline(AdrenalineParameters parameters)
    {
        adrenalineStartOpacity = AdrenalineOpacity;
        adrenaline = parameters;
        adrenalineElapsed = 0;
        adrenalineActive = (double)parameters.Duration + parameters.Recovery > 0;
    }

    internal void AdvanceAdrenaline(float deltaTime)
    {
        if (!adrenalineActive || deltaTime <= 0f || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime)) return;
        adrenalineElapsed += deltaTime;
        if (adrenalineElapsed >= (double)adrenaline.Duration + adrenaline.Recovery) ClearAdrenaline();
    }

    internal void ClearAdrenaline()
    {
        adrenalineActive = false;
        adrenalineElapsed = 0;
        adrenalineStartOpacity = 0;
    }

    internal bool TryConsumeFreeze()
    {
        if (!freezeAbilityReady) return false;
        freezeAbilityReady = false;
        return true;
    }

    internal bool TryConsumeGravityReverse()
    {
        if (!gravityReverseAbilityReady) return false;
        gravityReverseAbilityReady = false;
        return true;
    }

    internal void CompleteFreezeCooldown() { freezeAbilityReady = true; }
    internal void CompleteGravityReverseCooldown() { gravityReverseAbilityReady = true; }

    internal void BeginFreeze(float duration)
    {
        freezeTimer = freezeActive ? Mathf.Max(freezeTimer, duration) : duration;
        freezeActive = true;
    }

    internal bool AdvanceFreeze(float deltaTime)
    {
        freezeTimer -= deltaTime;
        if (freezeTimer <= 0f)
        {
            freezeActive = false;
            return true;
        }
        return false;
    }

    internal void IncreaseBatCharge(int amount, int maximum)
    {
        currentBatAttackBar += amount;
        if (currentBatAttackBar > maximum)
            currentBatAttackBar = maximum;
    }

    internal void ResetBatCharge()
    {
        currentBatAttackBar = 0;
    }
}
