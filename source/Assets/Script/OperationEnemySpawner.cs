using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // For the bat attack slider

public class OperationEnemySpawner : MonoBehaviour
{
    // ********************************************
    // NEW: Define enum for special enemy types
    public enum SpecialEnemyType { None, DarkRed, Golden, Blue, Silver }
    // ********************************************

    #region Prefab & Display Settings
    [Header("Prefab & Display Settings")]
    public GameObject operationPrefab;    // Enemy prefab with child TMP_Text
    #endregion

    #region Spawn & Movement Settings
    [Header("Spawn & Movement Settings")]
    public float spawnY = 1f;
    public float targetY = -11f;
    public float fallSpeed = 2f;
    public float spawnInterval = 2f; // seconds

    [Header("X Position Settings")]
    public float[] spawnXPositions = { 0.5f, 2.5f, 4.5f, 6.5f };
    #endregion

    #region Operation Generator & Input Display
    [Header("Operation Generator")]
    public MathOperationGenerator operationGenerator;   // Reference to MathOperationGenerator

    [Header("Player Input Display Settings")]
    public TMP_Text playerInputDisplay;   // UI text for player's input
    #endregion

    #region Projectile & Mage Settings
    [Header("Answer Projectile Settings")]
    public GameObject answerProjectilePrefab;             // Projectile prefab
    public Transform projectileSpawnPoint;                // Projectile spawn point

    [Header("Mage Settings")]
    public MageController mageAnimationController;        // For playing attack animation
    public MageController mageController;                 // For reading current lives during difficulty scaling
    #endregion

    #region Game Control Settings
    [Header("Pause Settings")]
    public GameObject pausePanel;
    private bool isGamePaused = false;
    [SerializeField, Min(0f)] private float resumeCountdownDuration = 3f;
    [SerializeField] private TMP_Text resumeCountdownText;
    private Coroutine resumeRoutine;

    [Header("Points Settings")]
    [Tooltip("Legacy serialized value. Phase 8 uses the fixed base in ScoreRules.")]
    public int pointsPerEnemy = 10;
    public PointsManager pointsManager;
    #endregion

    #region Double Spawn Settings
    [Header("Double Spawn Settings")]
    public int difficultyPointsForDoubleSpawn = 20; // Every 20 difficulty points increases the extra spawn chance.
    public float doubleSpawnChanceIncrement = 0.1f;   // Increases chance by 10% per 20 points.
    #endregion

    #region Audio Settings
    [Header("Audio Settings")]
    public AudioClip batSound;            // Assign your sound clip via the Inspector.
    public AudioSource batAudioSource;    // Assign an AudioSource (for volume, pitch, etc.)
    #endregion

    #region Internal Data Structures

    [System.Serializable]
    public struct EnemyOperation
    {
        public GameObject enemy;
        public int result;
        // NEW: Special enemy type – special enemies will have a value other than None.
        public SpecialEnemyType specialType;
        internal ScoreRules.Expression scoringExpression;
    }
    public List<EnemyOperation> enemyOperations = new List<EnemyOperation>();

    private float spawnTimer = 0f;
    private string currentPlayerInput = "";
    private float difficultyTimer = 0f;
    #endregion

    #region Difficulty Settings
    [Header("Difficulty Settings")]
    public float difficultyUpdateInterval = 10f;
    private int lastMageLives;
    public int difficultyLevel = 0; // This integer increases over time.
    private int currentStreak = 0;

    // Base adjustments per difficulty update.
    public float fallSpeedIncreaseRate = 0.2f;
    public float maxFallSpeed = 10f;
    public float spawnIntervalDecreaseRate = 0.1f;
    public float minSpawnInterval = 0.5f;
    #endregion

    #region Special Abilities Settings
    [Header("Special Abilities Settings")]
    // Freeze mode settings (activated with a key like X)
    public float freezeDuration = 5f;  // Duration of freeze in seconds.

    // Gravity Reverse settings (activated with a key like C)
    public float gravityReverseDuration = 5f;   // Duration for reversed gravity.
    public float reversedTargetY = 8f;          // Target Y position when enemies rise.

    #endregion

    #region Game Over & Misc Settings
    // Game Over flag.
    private bool gameOverTriggered = false;

    // Tracking for difficulty increases and forced spawn cooldown.
    private float lastDifficultyIncreaseTime = 0f;
    private float forcedSpawnCooldown = 1f;
    private float lastForcedSpawnTime = -999f;  // Force to be ready on start.
    #endregion

    #region Bat Attack Bar Settings
    [Header("Bat Attack Bar Settings")]
    public Slider batAttackSlider;   // UI slider for bat attack
    public int batAttackBarMax = 5;    // Maximum points required for bat attack
    #endregion

    #region Special Abilities UI / Cooldown Settings
    [Header("Special Abilities UI / Cooldown Settings")]
    public Image freezeAbilityImage;                   // Freeze power-up image indicator
    public Image freezeKeyIndicatorImage;                // NEW: Freeze key indicator image (for key 'X')
    public TextMeshProUGUI freezeAbilityCounterText;     // Countdown for freeze ability
    public float freezeAbilityCooldown = 20f;            // Cooldown duration for freeze

    public Image gravityReverseAbilityImage;           // Gravity reverse power-up image indicator
    public Image gravityReverseKeyIndicatorImage;        // NEW: Gravity reverse key indicator image (for key 'C')
    public TextMeshProUGUI gravityReverseCounterText;    // Countdown for gravity reverse ability
    public float gravityReverseCooldown = 20f;           // Cooldown duration for gravity reverse

    // NEW: Bat Attack fields (icon and key indicator)
    public Image batAttackAbilityIconImage;   // Image for the Bat Attack ability icon
    public Image batAttackKeyIndicatorImage;    // Image for the Bat Attack key indicator (e.g., key 'Z')

    #endregion

    #region Special Enemy Spawn Settings
    [Header("Special Enemy Spawn Settings")]
    public float specialEnemyChance = 0.1f; // 10% chance to spawn a special enemy
    public float specialEnemyLifetime = 4f;     // How long the special enemy stays alive before disappearing

    // NEW: Extra configurable fields for special enemy effects
    [Header("Special Enemy Effects Settings")]
    [Tooltip("Legacy serialized value. Phase 8 uses the fixed 4x multiplier in ScoreRules.")]
    public float goldenPointsMultiplier = 2.0f;
    public float blueFreezeDuration = 5f;          // Custom freeze duration for blue enemy
    #endregion

    #region Border Settings
    [Header("Border Settings")]
    public Color borderColor = Color.black;     // Border color for all enemies
    public float borderWidth = 0.4f;            // Border width for all enemies
    #endregion

    [SerializeField, Tooltip("Applied once per scene instance. When assigned, overrides legacy balance values above; edits take effect next game.")]
    private GameBalanceConfig balanceConfig;
    private bool balanceInitialized;
    private string initialBalanceDescription;
    private ScoreRules.Parameters scoringParameters = ScoreRules.Parameters.Default;
    private sealed class AnswerCredit
    {
        internal Transform Target;
        internal PointsManager Manager;
        internal BatProjectile Projectile;
        internal int Amount, Generation, ScoreGeneration;
        internal bool Launched, Resolved;
    }
    private readonly Dictionary<Transform, AnswerCredit> answerCredits = new Dictionary<Transform, AnswerCredit>();
    private SpecialAbilityState.AdrenalineParameters adrenalineParameters;
    private AdrenalineVignette adrenalineVignette;
    private SpecialAbilityState.CollectiveParameters collectiveParameters;
    private sealed class CollectiveProjectile
    {
        internal Transform Target;
        internal BatProjectile Projectile;
        internal bool Confirmed, Launched;
    }
    private readonly Dictionary<int, CollectiveProjectile> collectiveProjectiles = new Dictionary<int, CollectiveProjectile>();
    private int collectiveGeneration;
    private DifficultyProgression.Curve difficultyCurve = DifficultyProgression.Curve.Unaccelerated;
    private DifficultyProgression.State difficultyState;
    private double difficultyIntervalStart;
    private SpawnDirector.CapacitySettings spawnCapacity = SpawnDirector.CapacitySettings.Default;
    private int initialSpawnDifficulty, spawnGeneration;
    private bool creatingEnemy;
    private float visualSpacingMargin = 0.1f, lastCreatedSpawnX;

    private readonly EnemyRegistry enemyRegistry = new EnemyRegistry();
    private readonly EnemyTextPresentation textPresentation = new EnemyTextPresentation();
    private readonly SpecialAbilityState abilityState = new SpecialAbilityState();
    private SpawnDirector spawnDirector;
    private MatchRandomSession fallbackRandomSession;
    public MatchRandomSession RandomSession => operationGenerator != null
        ? operationGenerator.RandomSession
        : fallbackRandomSession ?? (fallbackRandomSession = MatchRandomSession.CreateConfigured());

    private IRandomSource ReadGameplayRandom() => RandomSession.Gameplay;
    private int ReadEnemyCap() => spawnCapacity.Limit(difficultyLevel, initialSpawnDifficulty);
    private bool CanCreateEnemies() => this != null && gameObject.activeInHierarchy && !isGamePaused
        && !gameOverTriggered && resumeRoutine == null && abilityState.IsSpawningAllowed;
    private bool TrySpawnNormal(float x) => TryCreateEnemy(x, false);
    private bool TrySpawnSpecial(float x) => TryCreateEnemy(x, true);

    private SpawnDirector GetSpawnDirector()
    {
        if (spawnDirector == null)
            spawnDirector = new SpawnDirector(enemyRegistry, abilityState, ReadSpawnParameters, TrySpawnNormal, TrySpawnSpecial, ReadGameplayRandom,
                () => !creatingEnemy && CanCreateEnemies(), readCreatedX: () => lastCreatedSpawnX, useVisualPlacement: true);
        return spawnDirector;
    }

    private SpawnDirector.Parameters ReadSpawnParameters()
    {
        return new SpawnDirector.Parameters(spawnY, spawnInterval, spawnXPositions, difficultyLevel,
            difficultyPointsForDoubleSpawn, doubleSpawnChanceIncrement, specialEnemyChance, ReadEnemyCap(), spawnCapacity.EmptyDelay);
    }

    private DifficultyProgression.Parameters ReadDifficultyParameters()
    {
        return new DifficultyProgression.Parameters(fallSpeedIncreaseRate, maxFallSpeed, spawnIntervalDecreaseRate, minSpawnInterval);
    }

    void Start()
    {
        if (!isGamePaused)
            Time.timeScale = 1f;
        spawnTimer = 0f;
        difficultyTimer = 0f;
        difficultyIntervalStart = Time.timeSinceLevelLoadAsDouble;

        if (playerInputDisplay != null)
            playerInputDisplay.text = "";

        if (mageController != null)
            lastMageLives = mageController.CurrentLives;
        else
            Debug.LogWarning("MageController reference missing for difficulty scaling!");


        if (batAttackSlider != null)
        {
            batAttackSlider.minValue = 0;
            batAttackSlider.maxValue = batAttackBarMax;
            batAttackSlider.value = abilityState.CurrentBatAttackBar;
        }
        if (batAttackAbilityIconImage != null)
            batAttackAbilityIconImage.gameObject.SetActive(false);
        if (batAttackKeyIndicatorImage != null)
            batAttackKeyIndicatorImage.gameObject.SetActive(false);

    }

    void Update()
    {
        if (gameOverTriggered || resumeRoutine != null)
            return;

        // --- PAUSE TOGGLE ---
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!isGamePaused)
                FreezeGame();
            else
                UnfreezeGame();
            return;
        }

        if (isGamePaused)
            return;

        UpdateCollectiveAttack(Time.timeAsDouble);

        // SPECIAL ABILITY KEY CHECKS
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (abilityState.CurrentBatAttackBar >= batAttackBarMax)
            {
                ExecuteBatAttackAll();
            }
            else
            {
                Debug.Log("Bat Attack not available: Bar not full (" + abilityState.CurrentBatAttackBar + "/" + batAttackBarMax + ").");
            }
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            ActivateFreezeMode();
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            ActivateGravityReverse();
        }

        HandlePlayerInput(Input.inputString, Input.GetKeyDown(KeyCode.Backspace), Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space));

        // Update timers.
        AdvanceAdrenaline(Time.deltaTime);
        spawnTimer += Time.deltaTime;
        difficultyTimer = (float)System.Math.Max(0, Time.timeSinceLevelLoadAsDouble - difficultyIntervalStart);

        // Normal enemy spawn (only if allowed)
        GetSpawnDirector().TryNormalSpawn(ref spawnTimer);

        // Freeze mode update (skip movement/spawn if active)
        if (abilityState.FreezeActive)
        {
            if (abilityState.AdvanceFreeze(Time.deltaTime))
            {
                Debug.Log("Freeze mode ended.");
                // Reset enemy text color to their original colors
                foreach (GameObject enemy in enemyRegistry.ActiveEnemies)
                {
                    if (enemy != null)
                    {
                        TMP_Text tmp = enemy.GetComponentInChildren<TMP_Text>();
                        if (tmp != null && !enemyRegistry.IsAnswered(enemy))
                        {
                            textPresentation.RestoreColor(tmp, GetOriginalEnemyColor(enemy));
                        }
                    }
                }
            }
            return; // Skip spawn and movement while frozen.
        }

        // Forced-Spawning (only if spawning is allowed)
        GetSpawnDirector().TryForcedSpawn(ref spawnTimer, ref lastForcedSpawnTime, forcedSpawnCooldown);

        enemyRegistry.MoveActive(abilityState.GravityReversed, fallSpeed * abilityState.AdrenalineMultiplier, reversedTargetY, targetY, Time.deltaTime);

        // Handle difficulty update.
        PerformDifficultyUpdate();
    }

    private void HandlePlayerInput(string inputThisFrame, bool backspacePressed, bool submitPressed)
    {
        if (isGamePaused || gameOverTriggered)
            return;

        if (!string.IsNullOrEmpty(inputThisFrame))
        {
            foreach (char c in inputThisFrame)
            {
                if (char.IsDigit(c))
                {
                    currentPlayerInput += c;
                    if (playerInputDisplay != null)
                        playerInputDisplay.text = currentPlayerInput;
                }
            }
        }
        if (backspacePressed && currentPlayerInput.Length > 0)
        {
            currentPlayerInput = currentPlayerInput.Substring(0, currentPlayerInput.Length - 1);
            if (playerInputDisplay != null)
                playerInputDisplay.text = currentPlayerInput;
        }
        if (submitPressed)
        {
            ProcessPlayerInput();
        }
    }

    /// <summary>
    /// Helper method to get the color for a specific special enemy type (or white for normal enemies).
    /// </summary>
    private Color GetColorForSpecialType(SpecialEnemyType specialType)
    {
        return textPresentation.GetColorForSpecialType(specialType);
    }

    /// <summary>
    /// Applies a border to the given TMP_Text component.
    /// </summary>
    private void ApplyBorderToText(TMP_Text tmp)
    {
        textPresentation.ApplyBorderToText(tmp, borderColor, borderWidth, isGamePaused);
    }

    private void LateUpdate()
    {
        if (answerCredits.Count != 0)
            foreach (AnswerCredit credit in new List<AnswerCredit>(answerCredits.Values))
                if (!IsCurrentAnswerCredit(credit) || credit.Target == null)
                    CancelAnswerCredit(credit);
        textPresentation.PruneDestroyed();
        enemyRegistry.PruneDestroyed(enemyOperations);
    }

    private void SetOperationVisibility(bool hide)
    {
        textPresentation.SetOperationVisibility(hide);
    }

    private void OnDestroy()
    {
        pointsManager?.ClearScoreFeedback();
        CancelAnswerCredits();
        CancelCollectiveAttack();
        ClearAdrenaline();
        CancelPauseTransition();
        textPresentation.Dispose();
        enemyRegistry.ClearConfirmationTracking();
    }

    // Only MageController's effective life-loss path calls this; streak resets are independent.
    internal void NotifyLifeLost(bool fatal)
    {
        if (fatal)
        {
            pointsManager?.StopScoring();
            CancelAnswerCredits();
            ClearAdrenaline();
            return;
        }
        if (!isActiveAndEnabled || gameOverTriggered) return;
        abilityState.BeginAdrenaline(adrenalineParameters);
        PresentAdrenaline();
    }

    private void AdvanceAdrenaline(float deltaTime)
    {
        abilityState.AdvanceAdrenaline(deltaTime);
        PresentAdrenaline();
    }

    private void PresentAdrenaline()
    {
        if (adrenalineVignette == null && abilityState.AdrenalineActive && adrenalineParameters.Intensity > 0f
            && adrenalineParameters.Border > 0f)
            adrenalineVignette = AdrenalineVignette.Create(transform, adrenalineParameters.Border);
        if (adrenalineVignette != null) adrenalineVignette.SetOpacity(abilityState.AdrenalineOpacity);
    }

    private void ClearAdrenaline()
    {
        abilityState.ClearAdrenaline();
        if (adrenalineVignette != null) adrenalineVignette.SetOpacity(0f);
    }

    /// <summary>
    /// Returns true if enemy spawning is allowed (i.e. not in freeze mode or gravity reversal).
    /// </summary>
    private bool IsSpawningAllowed()
    {
        return abilityState.IsSpawningAllowed;
    }

    // Helper: Check vertical spacing when moving enemy upward.
    private bool CanMoveUp(GameObject currentEnemy, float newY)
    {
        return enemyRegistry.CanMoveUp(currentEnemy, newY);
    }

    // Helper: Only allow spawning if no enemy is within 0.6 units (vertically) of spawnY.
    private bool CanSpawnEnemy()
    {
        return enemyRegistry.CanSpawnEnemy(spawnY);
    }

    public int GetDifficulty()
    {
        return difficultyLevel;
    }

    void PerformDifficultyUpdate()
    {
        UpdateDifficulty(Time.timeSinceLevelLoadAsDouble);
    }

    // Explicit scene time also allows deterministic integration tests without advancing frames.
    internal void UpdateDifficulty(double sceneSeconds)
    {
        if (gameOverTriggered || isGamePaused || resumeRoutine != null || abilityState.FreezeActive) return;
        if (!DifficultyProgression.TryBatch(difficultyCurve, difficultyIntervalStart, sceneSeconds,
            difficultyUpdateInterval, out double end, out double growth)) return;
        difficultyIntervalStart = end;
        difficultyTimer = (float)System.Math.Max(0, sceneSeconds - end);
        lastDifficultyIncreaseTime = (float)(Time.timeAsDouble - difficultyTimer);
        Debug.Log("Difficulty timer reached threshold. Updating difficulty.");
        DifficultyProgression.Advance(growth, ref difficultyState, ref difficultyLevel,
            ref fallSpeed, ref spawnInterval, ReadDifficultyParameters);
    }

    void ExecuteBatAttackAll()
    {
        if (isGamePaused || gameOverTriggered || resumeRoutine != null || abilityState.CollectiveActive
            || (mageController != null && mageController.DeathMage))
            return;

        if (abilityState.CurrentBatAttackBar < batAttackBarMax)
        {
            Debug.Log("Bat attack not charged.");
            return;
        }
        List<Transform> targets = enemyRegistry.CollectCollectiveTargets(enemyOperations);
        if (targets.Count == 0) return;

        int generation = ++collectiveGeneration;
        abilityState.BeginCollectiveAttack(targets.Count, Time.timeAsDouble, collectiveParameters);
        for (int i = 0; i < targets.Count; i++)
            collectiveProjectiles.Add(i, new CollectiveProjectile { Target = targets[i] });
        for (int i = 0; i < targets.Count; i++)
        {
            CollectiveProjectile pending = collectiveProjectiles[i];
            if (pending.Target != null && TryConfirmHit(pending.Target.gameObject))
            {
                pending.Confirmed = true;
                enemyRegistry.RemoveEnemy(pending.Target.gameObject, enemyOperations);
            }
            else ResolveCollectiveProjectile(generation, i, Time.timeAsDouble);
        }
        ResetBatAttackBar();
        if (!IsCurrentCollectiveAttack(generation)) return;
        Debug.Log("Special Bat Attack All activated.");
        if (!IsCurrentCollectiveAttack(generation)) return;
        if (mageAnimationController != null)
            mageAnimationController.PlayBatAttack(() => LaunchCollectiveProjectiles(generation));
        else LaunchCollectiveProjectiles(generation);
    }

    private bool IsCurrentCollectiveAttack(int generation) => this != null && gameObject.activeInHierarchy
        && !gameOverTriggered && generation == collectiveGeneration && abilityState.CollectiveActive;

    private void LaunchCollectiveProjectiles(int generation)
    {
        if (!IsCurrentCollectiveAttack(generation)) return;
        foreach (int token in new List<int>(collectiveProjectiles.Keys))
        {
            if (!IsCurrentCollectiveAttack(generation)) return;
            if (!collectiveProjectiles.TryGetValue(token, out CollectiveProjectile pending) || pending.Launched) continue;
            pending.Launched = true;
            if (pending.Target == null || answerProjectilePrefab == null || projectileSpawnPoint == null)
            {
                ResolveCollectiveProjectile(generation, token, Time.timeAsDouble);
                continue;
            }
            GameObject projectile = Instantiate(answerProjectilePrefab, projectileSpawnPoint.position, Quaternion.identity);
            if (!IsCurrentCollectiveAttack(generation) || !collectiveProjectiles.ContainsKey(token))
            {
                if (projectile != null) Destroy(projectile);
                continue;
            }
            BatProjectile bat = projectile == null ? null : projectile.GetComponent<BatProjectile>();
            if (bat == null)
            {
                if (projectile != null) Destroy(projectile);
                ResolveCollectiveProjectile(generation, token, Time.timeAsDouble);
                continue;
            }
            pending.Projectile = bat;
            bat.target = pending.Target;
            bat.TrackCompletion((_, result) => ResolveCollectiveProjectile(generation, token, Time.timeAsDouble));
            if (IsCurrentCollectiveAttack(generation) && collectiveProjectiles.ContainsKey(token)
                && batAudioSource != null && batSound != null) batAudioSource.PlayOneShot(batSound);
        }
    }

    private void ResolveCollectiveProjectile(int generation, int token, double now)
    {
        if (!IsCurrentCollectiveAttack(generation) || !collectiveProjectiles.TryGetValue(token, out CollectiveProjectile pending)) return;
        collectiveProjectiles.Remove(token);
        abilityState.ResolveCollectiveProjectile(now);
        CleanCollectiveProjectile(pending);
    }

    private void CleanCollectiveProjectile(CollectiveProjectile pending)
    {
        if (pending.Projectile != null) pending.Projectile.CancelCollectiveProjectile();
        if (pending.Confirmed && pending.Target != null)
        {
            enemyRegistry.RemoveEnemy(pending.Target.gameObject, enemyOperations);
            Destroy(pending.Target.gameObject);
        }
    }

    internal void UpdateCollectiveAttack(double now)
    {
        if (isGamePaused || resumeRoutine != null || gameOverTriggered || !abilityState.CollectiveActive) return;
        int generation = collectiveGeneration;
        bool expired = abilityState.CollectiveTimedOut(now);
        foreach (int token in new List<int>(collectiveProjectiles.Keys))
        {
            if (collectiveProjectiles.TryGetValue(token, out CollectiveProjectile pending) && (expired || pending.Target == null))
                ResolveCollectiveProjectile(generation, token, now);
        }
        abilityState.AdvanceCollectiveAttack(now);
    }

    private void CancelCollectiveAttack()
    {
        unchecked { collectiveGeneration++; }
        var pending = new List<CollectiveProjectile>(collectiveProjectiles.Values);
        collectiveProjectiles.Clear();
        abilityState.ClearCollectiveAttack();
        foreach (CollectiveProjectile projectile in pending) CleanCollectiveProjectile(projectile);
    }

    private bool TryConfirmHit(GameObject enemy)
    {

        if (!enemyRegistry.TryConfirm(enemy))
            return false;
        textPresentation.MarkHit(enemy);
        return true;
    }

    private Color GetOriginalEnemyColor(GameObject enemy)
    {
        return GetColorForSpecialType(enemyRegistry.OriginalType(enemyOperations, enemy));
    }

    private void ApplyFreezeAppearance()
    {
        foreach (GameObject enemy in enemyRegistry.ActiveEnemies)
        {
            if (enemy != null && !enemyRegistry.IsAnswered(enemy))
            {
                TMP_Text tmp = enemy.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    textPresentation.ApplyFreezeColor(tmp);
                    ApplyBorderToText(tmp);
                }
            }
        }
    }

    void ActivateFreezeMode()
    {
        if (isGamePaused || gameOverTriggered)
            return;

        if (!abilityState.TryConsumeFreeze())
        {
            Debug.Log("Freeze ability is on cooldown.");
            return;
        }

        if (freezeAbilityImage != null)
            freezeAbilityImage.gameObject.SetActive(false);
        // NEW: Disable key indicator image along with the main ability indicator.
        if (freezeKeyIndicatorImage != null)
            freezeKeyIndicatorImage.gameObject.SetActive(false);

        if (freezeAbilityCounterText != null)
        {
            freezeAbilityCounterText.gameObject.SetActive(true);
            freezeAbilityCounterText.text = freezeAbilityCooldown.ToString("0");
        }

        Debug.Log("Freeze Mode activated.");
        BeginFreeze(freezeDuration);

        StartCoroutine(FreezeAbilityCooldownCoroutine());
    }

    private IEnumerator FreezeAbilityCooldownCoroutine()
    {
        float remaining = freezeAbilityCooldown;
        while (remaining > 0)
        {
            if (freezeAbilityCounterText != null)
                freezeAbilityCounterText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        if (freezeAbilityCounterText != null)
        {
            freezeAbilityCounterText.text = "";
            freezeAbilityCounterText.gameObject.SetActive(false);
        }
        abilityState.CompleteFreezeCooldown();
        if (freezeAbilityImage != null)
            freezeAbilityImage.gameObject.SetActive(true);
        // NEW: Also re-enable the key indicator image.
        if (freezeKeyIndicatorImage != null)
            freezeKeyIndicatorImage.gameObject.SetActive(true);

        Debug.Log("Freeze ability is ready.");
    }

    void ActivateGravityReverse()
    {
        if (isGamePaused || gameOverTriggered)
            return;

        if (!abilityState.TryConsumeGravityReverse())
        {
            Debug.Log("Gravity Reverse ability is on cooldown.");
            return;
        }

        if (gravityReverseAbilityImage != null)
            gravityReverseAbilityImage.gameObject.SetActive(false);
        // NEW: Disable key indicator image for gravity reverse.
        if (gravityReverseKeyIndicatorImage != null)
            gravityReverseKeyIndicatorImage.gameObject.SetActive(false);

        if (gravityReverseCounterText != null)
        {
            gravityReverseCounterText.gameObject.SetActive(true);
            gravityReverseCounterText.text = gravityReverseCooldown.ToString("0");
        }

        abilityState.GravityReversed = true;
        Debug.Log("Gravity Reverse activated: enemies will rise for " + gravityReverseDuration + " seconds.");

        StartCoroutine(GravityReverseRoutine());
        StartCoroutine(GravityReverseCooldownCoroutine());
    }

    private IEnumerator GravityReverseCooldownCoroutine()
    {
        float remaining = gravityReverseCooldown;
        while (remaining > 0)
        {
            if (gravityReverseCounterText != null)
                gravityReverseCounterText.text = Mathf.CeilToInt(remaining).ToString();
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }
        if (gravityReverseCounterText != null)
        {
            gravityReverseCounterText.text = "";
            gravityReverseCounterText.gameObject.SetActive(false);
        }
        abilityState.CompleteGravityReverseCooldown();
        if (gravityReverseAbilityImage != null)
            gravityReverseAbilityImage.gameObject.SetActive(true);
        // NEW: Also re-enable the gravity reverse key indicator image.
        if (gravityReverseKeyIndicatorImage != null)
            gravityReverseKeyIndicatorImage.gameObject.SetActive(true);

        Debug.Log("Gravity Reverse ability is ready.");
    }

    private IEnumerator GravityReverseRoutine()
    {
        yield return new WaitForSeconds(gravityReverseDuration);
        abilityState.GravityReversed = false;
        Debug.Log("Gravity Reverse ended: enemies falling normally again.");
    }

    private void BeginFreeze(float duration)
    {
        abilityState.BeginFreeze(duration);
        ApplyFreezeAppearance();
    }

    // NEW: A version of freeze activation for blue special enemy.
    void ActivateFreezeModeWithDuration(float customDuration)
    {
        Debug.Log("Blue Special Enemy triggered Freeze Mode for " + customDuration + " seconds.");
        // Bypass ability cooldown check since this is triggered via enemy effect.
        BeginFreeze(customDuration);
    }

    /// <summary>
    /// SPECIAL ENEMY SPAWN AND EFFECTS
    /// </summary>

    // Modified SpawnEnemy: checks vertical spacing.
    void SpawnEnemy()
    {
        GetSpawnDirector().SpawnWave();
    }

    // NEW: Randomly choose a special enemy type.
    private SpecialEnemyType ChooseRandomSpecialEnemyType()
    {
        return GetSpawnDirector().ChooseSpecialType();
    }

    void SpawnSpecialEnemyAt(float spawnX)
    {
        if (spawnDirector != null && spawnDirector.IsWaveInProgress) return;
        TryCreateEnemy(spawnX, true);
    }

    bool CanSpawnSpecialEnemyAt(float spawnX)
    {
        return enemyRegistry.CanSpawnSpecialEnemyAt(spawnX, spawnY);
    }
    IEnumerator FadeAndDestroy(GameObject enemy, float duration)
    {
        if (enemy == null)
            yield break;
        float elapsed = 0f;
        TMP_Text tmp = enemy.GetComponentInChildren<TMP_Text>();
        Color originalColor = GetOriginalEnemyColor(enemy);
        if (tmp != null)
            originalColor.a = tmp.color.a;

        while (elapsed < duration)
        {
            if (enemy == null || enemyRegistry.IsAnswered(enemy))
                yield break;
            if (isGamePaused)
            {
                yield return null;
                continue;
            }
            elapsed += Time.deltaTime;
            if (tmp != null)
            {
                tmp.color = textPresentation.FadeColor(originalColor, abilityState.FreezeActive, elapsed, duration);
            }
            yield return null;
        }

        if (enemy != null && !enemyRegistry.IsAnswered(enemy))
        {
            enemyRegistry.RemoveEnemy(enemy, enemyOperations);
            Destroy(enemy);
        }
    }

    void SpawnEnemyAt(float spawnX)
    {
        if (spawnDirector != null && spawnDirector.IsWaveInProgress) return;
        TryCreateEnemy(spawnX, false);
    }

    private bool TryCreateEnemy(float spawnX, bool special)
    {
        if (creatingEnemy || !CanCreateEnemies() || !enemyRegistry.TryReserveSpawnSlot(ReadEnemyCap())) return false;
        creatingEnemy = true;
        int generation = spawnGeneration;
        GameObject enemy = null;
        bool completed = false;
        try
        {
            if (operationPrefab == null || operationGenerator == null)
            {
                Debug.LogWarning("Enemy creation skipped: prefab or operation generator missing.");
                return false;
            }
            if (operationPrefab.GetComponentInChildren<TMP_Text>() == null)
            {
                Debug.LogWarning("Enemy creation skipped: prefab has no TMP_Text.");
                return false;
            }
            enemy = Instantiate(operationPrefab, new Vector3(spawnX, spawnY, 0f), Quaternion.identity);
            if (enemy == null || generation != spawnGeneration || !CanCreateEnemies() || !enemyRegistry.CanCommitSpawnSlot(ReadEnemyCap())) return false;
            Debug.Log((special ? "Spawned SPECIAL enemy: " : "Spawned enemy: ") + enemy.name);
            if (enemy == null || generation != spawnGeneration || !CanCreateEnemies() || !enemyRegistry.CanCommitSpawnSlot(ReadEnemyCap())) return false;
            TMP_Text text = enemy.GetComponentInChildren<TMP_Text>();
            if (text == null || operationGenerator == null) return false;
            var operation = operationGenerator.GenerateOperation();
            if (!special) Debug.Log("Generated Operation: " + operation.operationString + ", Result: " + operation.result);
            if (enemy == null || text == null || generation != spawnGeneration || !CanCreateEnemies() || !enemyRegistry.CanCommitSpawnSlot(ReadEnemyCap())) return false;
            text.text = operation.operationString;
            SpecialEnemyType type = special ? ChooseRandomSpecialEnemyType() : SpecialEnemyType.None;
            text.color = special ? GetColorForSpecialType(type) : Color.white;
            ApplyBorderToText(text);
            if (!special) Debug.Log("Updated TMP_Text to: \"" + text.text + "\" (child: " + text.gameObject.name + ")");
            if (enemy == null || text == null || !textPresentation.TryPlaceAtSpawn(enemy, text, spawnX, spawnY,
                spawnXPositions, Camera.main, visualSpacingMargin)) return false;
            if (enemy == null || generation != spawnGeneration || !CanCreateEnemies()
                || enemyRegistry.IsAnswered(enemy) || !enemyRegistry.CanCommitSpawnSlot(ReadEnemyCap())) return false;
            enemyRegistry.AddActive(enemy);
            enemyOperations.Add(new EnemyOperation { enemy = enemy, result = operation.result, specialType = type,
                scoringExpression = operation.scoringExpression });
            StartCoroutine(special ? FadeAndDestroy(enemy, specialEnemyLifetime) : DestroyEnemyAfterTime(enemy, 50f));
            lastCreatedSpawnX = enemy.transform.position.x;
            completed = true;
            return true;
        }
        catch (MissingReferenceException error)
        {
            Debug.LogWarning("Enemy creation interrupted: " + error.Message);
            return false;
        }
        catch (UnityException error)
        {
            Debug.LogWarning("Enemy creation failed: " + error.Message);
            return false;
        }
        finally
        {
            if (!completed && enemy != null)
            {
                textPresentation.Forget(enemy.GetComponentInChildren<TMP_Text>(true));
                enemyRegistry.RemoveEnemy(enemy, enemyOperations);
                Destroy(enemy);
            }
            enemyRegistry.ReleaseSpawnSlot();
            creatingEnemy = false;
        }
    }

    void ProcessPlayerInput()
    {
        if (isGamePaused || !CanAcceptAnswer(spawnGeneration))
            return;

        string inputText = currentPlayerInput;
        Debug.Log("Processing player input: " + inputText);
        int inputNumber;
        if (MathAnswerEvaluator.TryParse(inputText, out inputNumber))
        {
            int candidateIndex = enemyRegistry.FindCandidateIndex(enemyOperations, inputNumber);
            bool candidateFound = candidateIndex >= 0;
            if (candidateFound)
            {
                int generation = spawnGeneration;
                EnemyOperation operation = enemyOperations[candidateIndex];
                Debug.Log("Correct answer for enemy with result: " + operation.result);
                if (!CanAcceptAnswer(generation) || operation.enemy == null || !TryConfirmHit(operation.enemy)) return;
                if (!CanAcceptAnswer(generation) || operation.enemy == null) return;

                ScoreRules.Increment(ref currentStreak);
                var credit = new AnswerCredit {
                    Target = operation.enemy.transform, Manager = pointsManager, Generation = generation,
                    ScoreGeneration = pointsManager.ScoringGeneration,
                    Amount = ScoreRules.Calculate(currentStreak, GetDifficulty(), initialSpawnDifficulty,
                        operation.scoringExpression, operation.specialType == SpecialEnemyType.Golden, false, scoringParameters)
                };
                answerCredits.Add(credit.Target, credit);
                // Keep the original target identity; launch callbacks may reset/replace the public list.
                enemyRegistry.RemoveEnemy(operation.enemy, enemyOperations);

                if (mageAnimationController != null)
                {
                    mageAnimationController.PlayBatAttack(() => {
                        if (this != null && IsCurrentAnswerCredit(credit)) SpawnProjectileWithTarget(credit.Target);
                    });
                }
                else
                {
                    Debug.LogWarning("MageAnimationController not assigned. Spawning projectile immediately.");
                    SpawnProjectileWithTarget(credit.Target);
                }

                if (!CanAcceptAnswer(generation)) return;
                SpecialEnemyEffects.ApplyRewards(operation.specialType,
                    () => { if (CanAcceptAnswer(generation)) HealMage(); },
                    duration => { if (CanAcceptAnswer(generation)) ActivateFreezeModeWithDuration(duration); },
                    amount => { if (CanAcceptAnswer(generation)) IncreaseBatAttackBar(amount); }, ReadBlueFreezeDuration);
                Debug.Log("Current streak: " + currentStreak + " | Frozen impact reward: " + credit.Amount);
            }
        }
        else
        {
            Debug.Log("Invalid player input: " + inputText);
        }

        currentPlayerInput = "";
        if (playerInputDisplay != null)
            playerInputDisplay.text = "";
    }

    public void ResetStreak()
    {
        Debug.Log("Life lost. Resetting streak.");
        ScoreRules.Reset(ref currentStreak);
    }

    // --- Bat Attack Bar Helper Methods ---
    private void IncreaseBatAttackBar(int amount)
    {
        abilityState.IncreaseBatCharge(amount, batAttackBarMax);
        if (batAttackSlider != null)
            batAttackSlider.value = abilityState.CurrentBatAttackBar;
        Debug.Log("Bat Attack Bar increased: " + abilityState.CurrentBatAttackBar + "/" + batAttackBarMax);

        // NEW: Show both images when the bar is full.
        bool barFull = abilityState.CurrentBatAttackBar >= batAttackBarMax;
        if (batAttackAbilityIconImage != null)
            batAttackAbilityIconImage.gameObject.SetActive(barFull);
        if (batAttackKeyIndicatorImage != null)
            batAttackKeyIndicatorImage.gameObject.SetActive(barFull);
    }

    private void ResetBatAttackBar()
    {
        abilityState.ResetBatCharge();
        if (batAttackSlider != null)
            batAttackSlider.value = abilityState.CurrentBatAttackBar;

        // NEW: Hide the Bat Attack ability icon and key indicator on reset.
        if (batAttackAbilityIconImage != null)
            batAttackAbilityIconImage.gameObject.SetActive(false);
        if (batAttackKeyIndicatorImage != null)
            batAttackKeyIndicatorImage.gameObject.SetActive(false);

        Debug.Log("Bat Attack Bar reset to 0.");
    }

    void SpawnProjectileWithTarget(Transform targetTransform)
    {
        if (targetTransform == null || !CanAcceptAnswer(spawnGeneration)) return;
        answerCredits.TryGetValue(targetTransform, out AnswerCredit credit);
        // The legacy physical launcher may be used without an answer; that path never registers points.
        if (credit != null)
        {
            if (!IsCurrentAnswerCredit(credit)) { CancelAnswerCredit(credit); return; }
            if (credit.Launched) return;
            credit.Launched = true;
        }

        if (answerProjectilePrefab != null && projectileSpawnPoint != null)
        {
            int generation = spawnGeneration;
            Vector3 launchPosition = projectileSpawnPoint.position;
            GameObject projectile = Instantiate(answerProjectilePrefab, launchPosition, Quaternion.identity);
            if (!CanAcceptAnswer(generation) || (credit != null && !IsCurrentAnswerCredit(credit)) || targetTransform == null || projectile == null)
            {
                if (projectile != null) Destroy(projectile);
                CancelAnswerCredit(credit);
                return;
            }
            BatProjectile projScript = projectile.GetComponent<BatProjectile>();
            if (projScript != null)
            {
                if (credit != null) credit.Projectile = projScript;
                Debug.Log("Bat projectile spawned at: " + launchPosition + " | Target position: " + targetTransform.position);
                if (!CanAcceptAnswer(generation) || (credit != null && !IsCurrentAnswerCredit(credit)) || targetTransform == null || projScript == null)
                {
                    if (projectile != null) Destroy(projectile);
                    CancelAnswerCredit(credit);
                    return;
                }
                projScript.target = targetTransform;
                if (credit != null)
                {
                    projScript.TrackImpact((bat, contacted) => CreditAnswerImpact(credit, bat, contacted));
                    projScript.TrackCompletion((_, result) => CompleteAnswerProjectile(credit));
                }
                if (batAudioSource != null && batSound != null) batAudioSource.PlayOneShot(batSound);
            }
            else
            {
                Debug.LogWarning("AnswerProjectile script not found on the projectile prefab!");
                if (projectile != null) Destroy(projectile);
                CancelAnswerCredit(credit);
                if (credit == null && targetTransform != null) Destroy(targetTransform.gameObject);
            }
        }
        else
        {
            Debug.LogWarning("AnswerProjectile prefab or projectile spawn point not assigned!");
            CancelAnswerCredit(credit);
            if (credit == null && targetTransform != null) Destroy(targetTransform.gameObject);
        }
    }

    IEnumerator AddPointsDelayed(int amount, float delay)
    {
        // Compatibility seam only: points are credited exclusively by CreditAnswerImpact.
        yield break;
    }

    private bool CanAcceptAnswer(int generation) => this != null && gameObject.activeInHierarchy
        && generation == spawnGeneration && !gameOverTriggered && pointsManager != null && pointsManager.IsScoring
        && (mageController == null || !mageController.DeathMage);

    private bool IsCurrentAnswerCredit(AnswerCredit credit) => !credit.Resolved && CanAcceptAnswer(credit.Generation)
        && credit.Manager == pointsManager && credit.Manager.ScoringGeneration == credit.ScoreGeneration
        && answerCredits.TryGetValue(credit.Target, out AnswerCredit current) && ReferenceEquals(current, credit);

    private void CreditAnswerImpact(AnswerCredit credit, BatProjectile bat, Transform contacted)
    {
        if (!IsCurrentAnswerCredit(credit) || bat != credit.Projectile || contacted == null || contacted != credit.Target)
        {
            CancelAnswerCredit(credit);
            return;
        }
        // Settle before touching the manager or invoking any log/UI observer.
        credit.Resolved = true;
        answerCredits.Remove(credit.Target);
        int accepted = credit.Manager.Credit(credit.Amount, credit.ScoreGeneration);
        Debug.Log("Points credited on bat contact: " + accepted);
    }

    private void CancelAnswerCredit(AnswerCredit credit)
    {
        if (credit == null || credit.Resolved) return;
        credit.Resolved = true;
        if (answerCredits.TryGetValue(credit.Target, out AnswerCredit current) && ReferenceEquals(current, credit))
            answerCredits.Remove(credit.Target);
        if (credit.Projectile != null) credit.Projectile.CancelProjectile();
        if (credit.Target != null)
        {
            enemyRegistry.RemoveEnemy(credit.Target.gameObject, enemyOperations);
            Destroy(credit.Target.gameObject);
        }
    }

    private void CompleteAnswerProjectile(AnswerCredit credit)
    {
        if (!credit.Resolved) { CancelAnswerCredit(credit); return; }
        // Contact already settled the score. Interrupted explosions still clean only their own target.
        if (credit.Target != null)
        {
            enemyRegistry.RemoveEnemy(credit.Target.gameObject, enemyOperations);
            Destroy(credit.Target.gameObject);
        }
    }

    private void CancelAnswerCredits()
    {
        foreach (AnswerCredit credit in new List<AnswerCredit>(answerCredits.Values)) CancelAnswerCredit(credit);
    }

    IEnumerator DestroyEnemyAfterTime(GameObject enemy, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (enemy != null && !enemyRegistry.IsAnswered(enemy))
        {
            Debug.Log("Enemy lifetime expired, destroying enemy: " + enemy.name);
            enemyRegistry.RemoveEnemy(enemy, enemyOperations);
            Destroy(enemy);
        }
    }

    /// <summary>
    /// Gameplay control.
    /// </summary>
    public void LoadGameplayScene()
    {
        Debug.Log("Loading Gameplay Scene...");
        SceneManager.LoadScene("Gameplay");
    }

    public void LoadMenuScene()
    {
        Debug.Log("Loading Menu Scene...");
        SceneManager.LoadScene("Menu");
    }

    public void QuitApplication()
    {
        Debug.Log("Quitting Application...");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void FreezeGame()
    {
        if (isGamePaused || gameOverTriggered)
            return;
        isGamePaused = true;
        pointsManager?.SetScoreFeedbackPaused(true);
        SetOperationVisibility(true);
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        Debug.Log("Game is paused.");
    }

    public void UnfreezeGame()
    {
        if (!isGamePaused || resumeRoutine != null || gameOverTriggered)
            return;
        if (pausePanel != null)
            pausePanel.SetActive(false);

        float duration = Mathf.Max(0f, resumeCountdownDuration);
        if (duration == 0f)
        {
            CompleteResume();
            return;
        }
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(true);
        resumeRoutine = StartCoroutine(ResumeCountdown(duration));
    }

    private IEnumerator ResumeCountdown(float duration)
    {
        double end = Time.realtimeSinceStartupAsDouble + duration;
        int displayed = -1;
        while (Time.realtimeSinceStartupAsDouble < end)
        {
            int remaining = Mathf.CeilToInt((float)(end - Time.realtimeSinceStartupAsDouble));
            if (remaining != displayed && resumeCountdownText != null)
                resumeCountdownText.SetText("{0}", remaining);
            displayed = remaining;
            yield return null;
        }
        resumeRoutine = null;
        CompleteResume();
    }

    private void CompleteResume()
    {
        SetOperationVisibility(false);
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);
        isGamePaused = false;
        pointsManager?.SetScoreFeedbackPaused(false);
        Time.timeScale = 1f;
        Debug.Log("Game resumed.");
    }

    private void CancelPauseTransition()
    {
        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }
        SetOperationVisibility(false);
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (isGamePaused)
            Time.timeScale = 1f;
        isGamePaused = false;
        pointsManager?.SetScoreFeedbackPaused(false);
    }

    private void OnDisable()
    {
        pointsManager?.ClearScoreFeedback();
        CancelAnswerCredits();
        CancelCollectiveAttack();
        ClearAdrenaline();
        CancelPauseTransition();
    }

    private void OnEnable()
    {
        InitializeBalance();
        ResetSpawnerState();
    }

    private void InitializeBalance()
    {
        if (balanceInitialized)
            return;
        balanceInitialized = true;
        var scoring = balanceConfig == null ? null : balanceConfig.scoring;
        scoringParameters = scoring == null ? ScoreRules.Parameters.Default : new ScoreRules.Parameters(
            scoring.pressurePointsToMaximum, scoring.advancedOperationBonus, scoring.extraDigitBonus);
        collectiveParameters = new SpecialAbilityState.CollectiveParameters(balanceConfig == null ? null : balanceConfig.abilities, out bool validCollective);
        if (!validCollective) Debug.LogWarning("Invalid collective attack settings; using defaults for invalid fields.");
        adrenalineParameters = new SpecialAbilityState.AdrenalineParameters(balanceConfig == null ? null : balanceConfig.adrenaline, out bool validAdrenaline);
        if (!validAdrenaline) Debug.LogWarning("Invalid adrenaline settings; using defaults for invalid fields.");
        initialSpawnDifficulty = difficultyLevel;
        if (balanceConfig == null)
        {
            initialBalanceDescription = DescribeInitialBalance();
            return;
        }

        // These fields are session-local working values, never writes into the asset.
        spawnY = balanceConfig.spawn.spawnY;
        targetY = balanceConfig.spawn.targetY;
        fallSpeed = balanceConfig.spawn.initialFallSpeed;
        spawnInterval = balanceConfig.spawn.initialSpawnInterval;
        spawnXPositions = (float[])balanceConfig.spawn.spawnXPositions.Clone();
        difficultyPointsForDoubleSpawn = balanceConfig.spawn.difficultyPointsForDoubleSpawn;
        doubleSpawnChanceIncrement = balanceConfig.spawn.doubleSpawnChanceIncrement;
        difficultyLevel = balanceConfig.difficulty.initialDifficultyLevel;
        initialSpawnDifficulty = difficultyLevel;
        spawnCapacity = SpawnDirector.CapacitySettings.Read(balanceConfig.spawn, out bool validCap, out bool validDelay);
        float margin = balanceConfig.spawn.visualSpacingMargin;
        if (float.IsNaN(margin) || float.IsInfinity(margin) || margin < 0f)
            Debug.LogWarning("Invalid visual spacing margin; using 0.1 world units.");
        else visualSpacingMargin = margin;
        if (!validCap) Debug.LogWarning("Invalid enemy capacity settings; using 4 initial, 8 maximum, 20 arcade points per slot.");
        if (!validDelay) Debug.LogWarning("Invalid empty-screen spawn delay; using 0.75 seconds.");
        difficultyUpdateInterval = balanceConfig.difficulty.difficultyUpdateInterval;
        fallSpeedIncreaseRate = balanceConfig.difficulty.fallSpeedIncreaseRate;
        maxFallSpeed = balanceConfig.difficulty.maxFallSpeed;
        spawnIntervalDecreaseRate = balanceConfig.difficulty.spawnIntervalDecreaseRate;
        minSpawnInterval = balanceConfig.difficulty.minSpawnInterval;
        difficultyCurve = DifficultyProgression.ReadCurve(balanceConfig.difficulty, out bool validCurve);
        if (!validCurve)
            Debug.LogWarning("Invalid arcade acceleration settings; using defaults (180 s, +0.25/min, maximum 2).");
        freezeDuration = balanceConfig.abilities.freezeDuration;
        freezeAbilityCooldown = balanceConfig.abilities.freezeAbilityCooldown;
        gravityReverseDuration = balanceConfig.abilities.gravityReverseDuration;
        gravityReverseCooldown = balanceConfig.abilities.gravityReverseCooldown;
        reversedTargetY = balanceConfig.abilities.reversedTargetY;
        batAttackBarMax = balanceConfig.abilities.batAttackBarMax;
        specialEnemyChance = balanceConfig.specialEnemies.specialEnemyChance;
        specialEnemyLifetime = balanceConfig.specialEnemies.specialEnemyLifetime;
        goldenPointsMultiplier = balanceConfig.specialEnemies.goldenPointsMultiplier;
        blueFreezeDuration = balanceConfig.specialEnemies.blueFreezeDuration;
        pointsPerEnemy = balanceConfig.scoring.pointsPerEnemy;
        initialBalanceDescription = DescribeInitialBalance();
    }

    private string DescribeInitialBalance()
    {
        var score = scoringParameters.OrDefault;
        var description = new MatchConfiguration()
            .Add("spawnY", spawnY).Add("targetY", targetY).Add("speed", fallSpeed).Add("interval", spawnInterval)
            .Add("positions", spawnXPositions == null ? -1 : spawnXPositions.Length);
        if (spawnXPositions != null) foreach (float x in spawnXPositions) description.Add("x", x);
        description.Add("doubleDivisor", difficultyPointsForDoubleSpawn).Add("doubleIncrement", doubleSpawnChanceIncrement)
            .Add("capacityInitial", spawnCapacity.Initial).Add("capacityMaximum", spawnCapacity.Maximum)
            .Add("capacityPoints", spawnCapacity.PointsPerSlot).Add("emptyDelay", spawnCapacity.EmptyDelay).Add("spacing", visualSpacingMargin)
            .Add("initialDifficulty", initialSpawnDifficulty).Add("difficultyPeriod", DifficultyProgression.EffectivePeriod(difficultyUpdateInterval))
            .Add("speedIncrease", fallSpeedIncreaseRate).Add("speedMaximum", maxFallSpeed)
            .Add("intervalDecrease", spawnIntervalDecreaseRate).Add("intervalMinimum", minSpawnInterval);
        bool accelerating = difficultyCurve.RatePerMinute > 0 && difficultyCurve.Maximum > 1;
        description.Add("accelerating", accelerating);
        if (accelerating) description.Add("arcadeStart", difficultyCurve.Start).Add("arcadeRate", difficultyCurve.RatePerMinute).Add("arcadeMaximum", difficultyCurve.Maximum);
        return description.Add("freezeDuration", freezeDuration).Add("freezeCooldown", freezeAbilityCooldown)
            .Add("reverseDuration", gravityReverseDuration).Add("reverseCooldown", gravityReverseCooldown).Add("reverseY", reversedTargetY)
            .Add("zCharge", batAttackBarMax).Add("zDelay", collectiveParameters.PostImpactDelay).Add("zTimeout", collectiveParameters.Timeout)
            .Add("specialChance", specialEnemyChance).Add("specialLifetime", specialEnemyLifetime).Add("blueFreeze", blueFreezeDuration)
            .Add("adrenalineMultiplier", adrenalineParameters.Multiplier).Add("adrenalineDuration", adrenalineParameters.Duration)
            .Add("adrenalineRecovery", adrenalineParameters.Recovery).Add("adrenalineFade", adrenalineParameters.FadeIn)
            .Add("adrenalineIntensity", adrenalineParameters.Intensity).Add("adrenalineBorder", adrenalineParameters.Border)
            .Add("pressureDivisor", score.PressurePointsToMaximum).Add("operatorBonus", score.AdvancedOperationBonus).Add("digitBonus", score.ExtraDigitBonus).Description;
    }

    internal CompletedMatch CompleteMatch(double duration, float feedbackDuration)
    {
        // Do not initialize a generator or RNG solely to manufacture metadata.
        if (operationGenerator == null || operationGenerator.CapturedRandomSession == null) return null;
        string math = operationGenerator.MathConfiguration;
        if (math == null) return null;
        var session = operationGenerator.CapturedRandomSession;
        var metadata = new MatchMetadata {
            schemaVersion = 1,
            seed = session.RootSeed.ToString(System.Globalization.CultureInfo.InvariantCulture),
            rngVersion = session.RngVersion,
            mathRulesVersion = MathGeneration.RulesVersion,
            scoringRulesVersion = ScoreRules.RulesVersion,
            balanceVersion = MatchMetadata.BalanceVersion,
            durationSeconds = duration,
            gameVersion = Application.version,
            configurationSignature = MatchConfiguration.Signature(initialBalanceDescription, math,
                mageController == null ? null : mageController.LifeConfiguration,
                new MatchConfiguration().Add("feedbackDuration", feedbackDuration).Description)
        };
        return new CompletedMatch(operationGenerator.CapturedGrade, metadata);
    }

    public void ResetSpawnerState()
    {
        CancelAnswerCredits();
        CancelCollectiveAttack();
        ClearAdrenaline();
        unchecked { spawnGeneration++; }
        spawnDirector?.ResetSchedule();
        CancelPauseTransition();
        spawnTimer = 0f;
        difficultyTimer = 0f;
        difficultyIntervalStart = Time.timeSinceLevelLoadAsDouble;
        currentPlayerInput = "";
        ScoreRules.Reset(ref currentStreak);
        isGamePaused = false;
        abilityState.FreezeActive = false;
        pointsManager?.ResetPoints();
        pointsManager?.BindMatchOwner(this);
        enemyRegistry.ClearActive();
        enemyOperations.Clear();
        gameOverTriggered = false;

        if (playerInputDisplay != null)
            playerInputDisplay.text = "";
        Debug.Log("OperationEnemySpawner state has been reset.");
    }

    public void TriggerGameOver()
    {
        if (gameOverTriggered)
            return;
        gameOverTriggered = true;
        pointsManager?.StopScoring();
        CancelAnswerCredits();
        ClearAdrenaline();
        unchecked { spawnGeneration++; }
        spawnDirector?.ResetSchedule();
        CancelPauseTransition();
        CancelCollectiveAttack();
        Debug.Log("Game Over triggered.");
        isGamePaused = true;
        foreach (GameObject enemy in new List<GameObject>(enemyRegistry.ActiveEnemies))
        {
            if (enemy != null)
                Destroy(enemy);
        }
        foreach (GameObject enemy in enemyRegistry.AnsweredEnemies)
        {
            if (enemy != null && !enemyRegistry.IsActive(enemy))
                Destroy(enemy);
        }
        enemyRegistry.ClearActive();
        enemyOperations.Clear();
        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(1f);
        SceneManager.LoadScene("Score");
    }

    private List<EnemyOperation> ReadEnemyOperations() { return enemyOperations; }
    private float ReadGoldenMultiplier() { return goldenPointsMultiplier; }
    private float ReadBlueFreezeDuration() { return blueFreezeDuration; }
    private void HealMage() { if (mageController != null) mageController.healMage(); }
}
