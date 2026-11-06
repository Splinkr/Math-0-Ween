using System;
using UnityEngine;

/// <summary>Initial parameters only. Consumers copy them once per scene instance.</summary>
[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "MathFall/Configuration/Game Balance")]
public sealed class GameBalanceConfig : ScriptableObject
{
    [Serializable]
    public sealed class SpawnSettings
    {
        [Tooltip("Enemy creation height.")] public float spawnY = 3f;
        [Tooltip("Normal falling destination.")] public float targetY = -11f;
        [Min(0f), Tooltip("Initial speed; progression belongs to the spawner instance.")] public float initialFallSpeed = 0.1f;
        [Min(0.01f), Tooltip("Initial normal spawn interval in scaled seconds.")] public float initialSpawnInterval = 4f;
        [Tooltip("Available columns. Must contain at least one position.")] public float[] spawnXPositions = { -1.5f, 0.5f, 2.5f, 4.5f };
        [Min(1), Tooltip("Divisor in the existing continuous double-spawn probability formula.")] public int difficultyPointsForDoubleSpawn = 1;
        [Range(0f, 1f), Tooltip("Probability increment per divisor unit; not a stepped threshold.")] public float doubleSpawnChanceIncrement = 0.01f;
        [Range(1, 8), Tooltip("Initial shared capacity for normal and special active enemies.")] public int initialEnemyCap = 4;
        [Range(1, 8), Tooltip("Maximum shared enemy capacity. Never greater than eight.")] public int maximumEnemyCap = 8;
        [Min(1), Tooltip("Arcade points gained since match initialization per additional enemy slot.")] public int difficultyPointsPerEnemyCapIncrease = 20;
        [Range(0.5f, 1f), Tooltip("Scaled seconds until the single empty-screen retry; normal spawn may occur sooner.")] public float emptyScreenSpawnDelay = 0.75f;
        [Min(0f), Tooltip("Minimum extra world-space gap between rendered expressions at spawn only. Does not affect movement.")] public float visualSpacingMargin = 0.1f;
    }

    [Serializable]
    public sealed class DifficultySettings
    {
        [Min(0), Tooltip("Initial arcade level, unrelated to the selected school grade.")] public int initialDifficultyLevel;
        [Min(0.01f), Tooltip("Scaled seconds per logical +2 update. Effective minimum is one second; overdue intervals are recovered without dropping remainder.")] public float difficultyUpdateInterval = 20f;
        [Min(0f), Tooltip("Speed increase per difficulty point.")] public float fallSpeedIncreaseRate = 0.03f;
        [Min(0f), Tooltip("Existing upper speed limit; not a level cap.")] public float maxFallSpeed = 10f;
        [Min(0f), Tooltip("Spawn interval reduction per difficulty point.")] public float spawnIntervalDecreaseRate = 0.07f;
        [Min(0.01f), Tooltip("Existing lower spawn interval limit.")] public float minSpawnInterval = 0.5f;
        [Min(0f), Tooltip("Scaled scene seconds before the arcade growth rate begins accelerating.")] public float accelerationStartSeconds = 180f;
        [Min(0f), Tooltip("Growth-rate multiplier added per minute after acceleration starts; 0.25 means +25% per minute.")] public float rateIncreasePerMinute = 0.25f;
        [Min(1f), Tooltip("Maximum growth-rate multiplier, not a cap on accumulated difficulty. One disables acceleration.")] public float maximumRateMultiplier = 2f;
    }

    [Serializable]
    public sealed class LifeSettings
    {
        [Min(1), Tooltip("Initial and maximum mage lives.")] public int maxLives = 7;
    }

    [Serializable]
    public sealed class AdrenalineSettings
    {
        [Range(0f, 1f), Tooltip("Temporary movement multiplier; never changes base speed.")] public float speedMultiplier = 0.8f;
        [Min(0f), Tooltip("Scaled seconds at the full reduction, before recovery.")] public float duration = 3f;
        [Min(0f), Tooltip("Scaled seconds for smooth recovery to normal speed.")] public float recoveryDuration = 0.5f;
        [Min(0f), Tooltip("Scaled seconds for the vignette to fade in.")] public float vignetteFadeIn = 0.15f;
        [Range(0f, 1f), Tooltip("Maximum red opacity at the outer screen edge.")] public float vignetteIntensity = 0.2f;
        [Range(0f, 0.5f), Tooltip("Fraction of each screen dimension used by each vignette border.")] public float vignetteBorder = 0.12f;
    }

    [Serializable]
    public sealed class AbilitySettings
    {
        [Min(0f), Tooltip("Freeze duration in scaled seconds.")] public float freezeDuration = 7f;
        [Min(0f), Tooltip("Freeze cooldown, starting on activation.")] public float freezeAbilityCooldown = 30f;
        [Min(0f), Tooltip("Reverse gravity duration in scaled seconds.")] public float gravityReverseDuration = 5f;
        [Min(0f), Tooltip("Reverse gravity cooldown, starting on activation.")] public float gravityReverseCooldown = 30f;
        [Tooltip("Destination height during reverse gravity.")] public float reversedTargetY = 8f;
        [Min(1), Tooltip("Charge required for the collective bat attack.")] public int batAttackBarMax = 10;
        [Min(0f), Tooltip("Scaled spawn-block seconds after the last collective impact or cancellation.")] public float collectivePostImpactDelay = 2f;
        [Min(0.01f), Tooltip("Maximum scaled seconds from Z activation to resolve pending launches/projectiles.")] public float collectiveProjectileTimeout = 10f;
    }

    [Serializable]
    public sealed class SpecialEnemySettings
    {
        [Range(0f, 1f), Tooltip("Additional special spawn attempt probability, still subject to spacing.")] public float specialEnemyChance = 0.13f;
        [Min(0.01f), Tooltip("Unanswered special lifetime and fade duration.")] public float specialEnemyLifetime = 10f;
        [Min(0f), Tooltip("Legacy serialized field. Phase 8 uses the fixed 4x multiplier in ScoreRules.")] public float goldenPointsMultiplier = 4f;
        [Min(0f), Tooltip("Blue Freeze duration; existing maximum-of-remaining rule is preserved.")] public float blueFreezeDuration = 1f;
    }

    [Serializable]
    public sealed class ScoringSettings
    {
        [Min(0), Tooltip("Legacy serialized field. Phase 8 uses the fixed base 10 in ScoreRules.")] public int pointsPerEnemy = 10;
        [Min(0), Tooltip("Legacy serialized field. Passive scoring is disabled.")] public int pointsToAdd = 5;
        [Min(0.01f), Tooltip("Legacy serialized field. Passive scoring is disabled.")] public float updateInterval = 1f;
        [Min(1), Tooltip("Arcade levels gained from the initial level to reach the 2x scoring limit.")] public int pressurePointsToMaximum = 80;
        [Min(0f), Tooltip("Phase 8 formula: complexity bonus for multiplication/division, with final complexity capped at 1.2x.")] public double advancedOperationBonus = 0.10;
        [Min(0f), Tooltip("Phase 8 formula: complexity per displayed digit beyond the three basic digits, capped at 1.2x overall.")] public double extraDigitBonus = 0.02;
        [Min(0.01f), Tooltip("Scaled seconds to display grouped credited points after the last impact. Pausing preserves the remaining duration.")] public float feedbackDuration = 1f;
    }

    [Header("Spawn and movement")] public SpawnSettings spawn = new SpawnSettings();
    [Header("Difficulty progression")] public DifficultySettings difficulty = new DifficultySettings();
    [Header("Lives")] public LifeSettings lives = new LifeSettings();
    [Header("Adrenaline")] public AdrenalineSettings adrenaline = new AdrenalineSettings();
    [Header("Abilities")] public AbilitySettings abilities = new AbilitySettings();
    [Header("Special enemies")] public SpecialEnemySettings specialEnemies = new SpecialEnemySettings();
    [Header("Scoring")] public ScoringSettings scoring = new ScoringSettings();
    [Header("Math profiles")]
    [Tooltip("Profiles are matched by grade (2–5), copied once per match. Edits apply next match.")]
    public GradeMathProfile[] mathProfiles = new GradeMathProfile[4];
}
