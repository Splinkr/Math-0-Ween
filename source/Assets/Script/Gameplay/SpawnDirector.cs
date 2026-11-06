using System;
using UnityEngine;
using SpecialEnemyType = OperationEnemySpawner.SpecialEnemyType;

// One director per facade. Owns only wave admission and the single retry schedule.
internal sealed class SpawnDirector
{
    internal readonly struct CapacitySettings
    {
        internal readonly int Initial, Maximum, PointsPerSlot;
        internal readonly double EmptyDelay;
        internal CapacitySettings(int initial, int maximum, int pointsPerSlot, double emptyDelay)
        {
            Initial = initial; Maximum = maximum; PointsPerSlot = pointsPerSlot; EmptyDelay = emptyDelay;
        }
        internal static CapacitySettings Default => new CapacitySettings(4, 8, 20, 0.75);
        internal int Limit(int currentDifficulty, int initialDifficulty)
        {
            long progress = Math.Max(0L, (long)currentDifficulty - initialDifficulty);
            return (int)Math.Min(Maximum, Initial + progress / PointsPerSlot);
        }
        internal static CapacitySettings Read(GameBalanceConfig.SpawnSettings source, out bool validCap, out bool validDelay)
        {
            validCap = source != null && source.initialEnemyCap >= 1 && source.initialEnemyCap <= source.maximumEnemyCap
                && source.maximumEnemyCap <= 8 && source.difficultyPointsPerEnemyCapIncrease > 0;
            validDelay = source != null && !float.IsNaN(source.emptyScreenSpawnDelay) && !float.IsInfinity(source.emptyScreenSpawnDelay)
                && source.emptyScreenSpawnDelay >= 0.5f && source.emptyScreenSpawnDelay <= 1f;
            return new CapacitySettings(validCap ? source.initialEnemyCap : 4, validCap ? source.maximumEnemyCap : 8,
                validCap ? source.difficultyPointsPerEnemyCapIncrease : 20, validDelay ? source.emptyScreenSpawnDelay : 0.75);
        }
    }

    internal readonly struct Parameters
    {
        internal readonly float SpawnY, SpawnInterval, DoubleChanceIncrement, SpecialChance;
        internal readonly int Difficulty, PointsForDouble, EnemyCap;
        internal readonly float[] Positions;
        internal readonly double EmptyDelay;
        internal Parameters(float spawnY, float spawnInterval, float[] positions, int difficulty,
            int pointsForDouble, float doubleChanceIncrement, float specialChance, int enemyCap = 8, double emptyDelay = 0.75)
        {
            SpawnY = spawnY; SpawnInterval = spawnInterval; Positions = positions; Difficulty = difficulty;
            PointsForDouble = pointsForDouble; DoubleChanceIncrement = doubleChanceIncrement; SpecialChance = specialChance;
            EnemyCap = enemyCap; EmptyDelay = emptyDelay;
        }
    }

    internal enum WaveResult { Blocked, Created, Failed }
    private readonly EnemyRegistry registry;
    private readonly SpecialAbilityState abilities;
    private readonly Func<Parameters> readParameters;
    private readonly Func<float, bool> createNormal, createSpecial;
    private readonly Func<IRandomSource> readRandom;
    private readonly Func<bool> sessionAllowsSpawn;
    private readonly Func<double> readTime;
    private readonly Func<int> readFrame;
    private readonly Func<float> readCreatedX;
    private readonly bool useVisualPlacement;
    private bool waveInProgress;
    private double? emptyDeadline, retryAfter;
    private int lastAutomaticFrame = -1, scheduleVersion;
    internal double? EmptyDeadline => emptyDeadline;
    internal bool IsWaveInProgress => waveInProgress;

    internal SpawnDirector(EnemyRegistry registry, SpecialAbilityState abilities, Func<Parameters> readParameters,
        Func<float, bool> createNormal, Func<float, bool> createSpecial, Func<IRandomSource> readRandom,
        Func<bool> sessionAllowsSpawn = null, Func<double> readTime = null, Func<int> readFrame = null,
        Func<float> readCreatedX = null, bool useVisualPlacement = false)
    {
        this.registry = registry; this.abilities = abilities; this.readParameters = readParameters;
        this.createNormal = createNormal; this.createSpecial = createSpecial; this.readRandom = readRandom;
        this.sessionAllowsSpawn = sessionAllowsSpawn ?? (() => true);
        this.readTime = readTime ?? (() => Time.timeAsDouble);
        this.readFrame = readFrame ?? (() => Time.frameCount);
        this.readCreatedX = readCreatedX;
        this.useVisualPlacement = useVisualPlacement;
    }

    // Preserve the existing internal fixture constructor. Production uses explicit success callbacks.
    internal SpawnDirector(EnemyRegistry registry, SpecialAbilityState abilities, Func<Parameters> readParameters,
        Action<float> createNormal, Action<float> createSpecial, Func<IRandomSource> readRandom)
        : this(registry, abilities, readParameters, x => { createNormal(x); return true; },
            x => { createSpecial(x); return true; }, readRandom) { }

    internal void ResetSchedule()
    {
        emptyDeadline = retryAfter = null;
        unchecked { scheduleVersion++; }
        // Never release an in-flight wave or permit a second automatic attempt in its frame.
    }

    private bool Allowed => abilities.IsSpawningAllowed && sessionAllowsSpawn();
    private bool Capacity => registry.HasSpawnCapacity(readParameters().EnemyCap);
    // Production measures the generated text in the creation callback. Legacy fixtures keep their geometry.
    private bool Space => useVisualPlacement || registry.CanSpawnEnemy(readParameters().SpawnY);
    private void ObserveEmpty(double now)
    {
        if (registry.ActiveCount > 0) emptyDeadline = null;
        else if (!emptyDeadline.HasValue) emptyDeadline = now + readParameters().EmptyDelay;
    }
    private bool AutomaticAllowed(double now) => Allowed && !waveInProgress && lastAutomaticFrame != readFrame()
        && (!retryAfter.HasValue || now >= retryAfter.Value);

    private WaveResult AutomaticAttempt(double now, string announcement = null)
    {
        lastAutomaticFrame = readFrame();
        int version = scheduleVersion;
        WaveResult result = WaveResult.Failed;
        try { result = SpawnWave(announcement); return result; }
        finally
        {
            if (version == scheduleVersion)
            {
                if (result == WaveResult.Failed) retryAfter = now + readParameters().EmptyDelay;
                if (registry.ActiveCount > 0) emptyDeadline = null;
                else emptyDeadline = now + readParameters().EmptyDelay;
            }
        }
    }

    internal void TryNormalSpawn(ref float timer)
    {
        double now = readTime(); ObserveEmpty(now);
        if (timer < readParameters().SpawnInterval || !AutomaticAllowed(now)) return;
        if (Capacity && Space) AutomaticAttempt(now);
        timer = 0f;
    }

    internal void TryForcedSpawn(ref float timer, ref float lastForcedTime, float cooldown)
    {
        double now = readTime(); ObserveEmpty(now);
        if (!AutomaticAllowed(now)) return;
        bool empty = registry.ActiveCount == 0;
        if (empty ? now < emptyDeadline.Value : registry.HasEnemyBelow(3f) || now - lastForcedTime < cooldown) return;
        if (!Capacity || !Space)
        {
            if (empty) emptyDeadline = now + readParameters().EmptyDelay;
            return;
        }
        AutomaticAttempt(now, empty ? "Empty-screen spawn attempt." : "Forced spawn triggered: no enemy below y=3; spawning enemy.");
        lastForcedTime = (float)now;
        timer = 0f;
    }

    internal WaveResult SpawnWave() => SpawnWave(null);

    private WaveResult SpawnWave(string announcement)
    {
        if (waveInProgress || !Allowed || !Capacity || !Space) return WaveResult.Blocked;
        waveInProgress = true;
        try
        {
            int version = scheduleVersion;
            if (announcement != null) Debug.Log(announcement);
            if (version != scheduleVersion || !Allowed || !Capacity || !Space) return WaveResult.Blocked;
            float[] positions = readParameters().Positions;
            if (positions == null || positions.Length == 0) return WaveResult.Failed;
            float spawnX = positions[readRandom().Range(0, positions.Length)];
            if (!createNormal(spawnX)) return WaveResult.Failed;
            if (readCreatedX != null) spawnX = readCreatedX();
            Debug.Log("Spawned enemy at x=" + spawnX + " (vertical spacing verified).");
            if (version != scheduleVersion || !Allowed || !Capacity) return WaveResult.Created;

            // Read each stage after callbacks; never precompute a wave or reserve a special slot.
            Parameters parameters = readParameters();
            float extraSpawnChance = Mathf.Clamp((parameters.Difficulty / (float)parameters.PointsForDouble) * parameters.DoubleChanceIncrement, 0f, 1f);
            if (readRandom().Value < extraSpawnChance)
            {
                float spawnX2 = spawnX;
                positions = readParameters().Positions;
                if (positions == null || positions.Length == 0) return WaveResult.Failed;
                if (positions.Length > 1)
                    for (int attempts = 0; spawnX2 == spawnX && attempts < 10; attempts++)
                        spawnX2 = positions[readRandom().Range(0, positions.Length)];
                if (!createNormal(spawnX2)) return WaveResult.Failed;
                if (readCreatedX != null) spawnX2 = readCreatedX();
                Debug.Log("Double spawn triggered, enemy spawned at x=" + spawnX2);
            }
            if (version != scheduleVersion || !Allowed || !Capacity) return WaveResult.Created;
            if (readRandom().Value < readParameters().SpecialChance)
            {
                positions = readParameters().Positions;
                if (positions == null || positions.Length == 0) return WaveResult.Failed;
                float specialX = positions[readRandom().Range(0, positions.Length)];
                if (useVisualPlacement || registry.CanSpawnSpecialEnemyAt(specialX, readParameters().SpawnY))
                {
                    if (!createSpecial(specialX)) return WaveResult.Failed;
                    if (readCreatedX != null) specialX = readCreatedX();
                    Debug.Log("Special enemy spawned at x=" + specialX);
                }
            }
            return WaveResult.Created;
        }
        finally { waveInProgress = false; }
    }

    internal SpecialEnemyType ChooseSpecialType()
    {
        int rand = readRandom().Range(0, 4);
        switch (rand)
        {
            case 0: return SpecialEnemyType.DarkRed;
            case 1: return SpecialEnemyType.Golden;
            case 2: return SpecialEnemyType.Blue;
            case 3: return SpecialEnemyType.Silver;
            default: return SpecialEnemyType.DarkRed;
        }
    }
}
