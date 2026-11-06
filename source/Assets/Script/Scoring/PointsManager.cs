using UnityEngine;
using TMPro;

public class PointsManager : MonoBehaviour
{
    public static PointsManager instance;

    // This UI reference belongs to the current scene.
    public TMP_Text pointsText;

    [Tooltip("Legacy serialized field. Passive scoring is disabled.")] public int pointsToAdd = 1;
    [Tooltip("Legacy serialized field. Passive scoring is disabled.")] public float updateInterval = 1f;

    [SerializeField, Tooltip("Grouped feedback duration is copied once per scene instance. Legacy passive values remain inert.")]
    private GameBalanceConfig balanceConfig;
    private bool balanceInitialized;

    private int currentPoints = 0;
    private bool isScoring = false;
    private int scoringGeneration;
    private TextMeshProUGUI scoreFeedback;
    private int groupedFeedbackPoints;
    private float feedbackRemaining, feedbackDuration = 1f;
    private bool feedbackPaused;
    private double matchStarted;
    private OperationEnemySpawner matchOwner;
    internal void BindMatchOwner(OperationEnemySpawner owner) { if (isScoring) matchOwner = owner; }
    public int CurrentPoints => currentPoints;
    internal int ScoringGeneration => scoringGeneration;
    internal bool IsScoring => isScoring;
    internal event System.Action<int> PointsCredited;

    private void Awake()
    {
        instance = this;
    }

    private void OnDisable()
    {
        matchOwner = null;
        isScoring = false;
        ClearScoreFeedback();
        CancelInvoke(nameof(AddPointsOnInterval));
    }

    private void OnDestroy()
    {
        matchOwner = null;
        isScoring = false;
        ClearScoreFeedback();
        if (scoreFeedback != null) Destroy(scoreFeedback.gameObject);
        CancelInvoke(nameof(AddPointsOnInterval));
        if (instance == this)
            instance = null;
    }

    private void Start()
    {
        UpdatePointsText();
    }

    public void AddPoints(int amount)
    {
        Credit(amount, scoringGeneration);
    }

    // Returns only the amount actually accepted by this still-open scoring session.
    internal int Credit(int amount, int expectedGeneration)
    {
        if (!isScoring || !isActiveAndEnabled || expectedGeneration != scoringGeneration || amount <= 0)
            return 0;
        int credited = (int)System.Math.Min(amount, (long)int.MaxValue - currentPoints);
        if (credited == 0) return 0;
        currentPoints += credited;
        UpdatePointsText();
        ShowScoreFeedback(credited);
        PointsCredited?.Invoke(credited);
        return credited;
    }

    private void Update()
    {
        AdvanceScoreFeedback(Time.deltaTime);
    }

    internal void AdvanceScoreFeedback(float deltaTime)
    {
        if (feedbackPaused || feedbackRemaining <= 0 || float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime <= 0) return;
        feedbackRemaining = Mathf.Max(0, feedbackRemaining - deltaTime);
        if (feedbackRemaining == 0) ClearScoreFeedback();
    }

    private void ShowScoreFeedback(int credited)
    {
        if (!(pointsText is TextMeshProUGUI) || pointsText.canvas == null) { ClearScoreFeedback(); return; }
        if (scoreFeedback == null || scoreFeedback.transform.parent != pointsText.transform)
        {
            if (scoreFeedback != null) { scoreFeedback.gameObject.SetActive(false); Destroy(scoreFeedback.gameObject); }
            var label = new GameObject("Grouped score feedback", typeof(RectTransform), typeof(TextMeshProUGUI));
            label.transform.SetParent(pointsText.transform, false);
            scoreFeedback = label.GetComponent<TextMeshProUGUI>();
            scoreFeedback.raycastTarget = false;
            scoreFeedback.richText = false;
            scoreFeedback.textWrappingMode = TextWrappingModes.NoWrap;
            scoreFeedback.alignment = TextAlignmentOptions.MidlineLeft;
            scoreFeedback.color = new Color(1f, 0.86f, 0.35f, 1f);
        }
        groupedFeedbackPoints = feedbackRemaining > 0 ? (int)System.Math.Min(int.MaxValue, (long)groupedFeedbackPoints + credited) : credited;
        feedbackRemaining = feedbackDuration;
        scoreFeedback.font = pointsText.font;
        scoreFeedback.fontSharedMaterial = pointsText.fontSharedMaterial;
        scoreFeedback.fontSize = pointsText.fontSize * 0.65f;
        scoreFeedback.fontStyle = pointsText.fontStyle;
        scoreFeedback.fontWeight = pointsText.fontWeight;
        scoreFeedback.text = "+" + groupedFeedbackPoints;
        // Use rendered glyph bounds, including the total's existing negative margins.
        pointsText.ForceMeshUpdate();
        var bounds = pointsText.textBounds;
        var rect = scoreFeedback.rectTransform;
        rect.anchorMin = rect.anchorMax = pointsText.rectTransform.pivot;
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(Mathf.Max(64, pointsText.rectTransform.rect.width), pointsText.fontSize * 1.5f);
        rect.anchoredPosition = new Vector2(bounds.max.x + pointsText.fontSize * 0.15f, bounds.center.y);
        scoreFeedback.gameObject.SetActive(!feedbackPaused);
    }

    internal void SetScoreFeedbackPaused(bool paused)
    {
        feedbackPaused = paused;
        if (scoreFeedback != null) scoreFeedback.gameObject.SetActive(!paused && isScoring && feedbackRemaining > 0);
    }

    internal void ClearScoreFeedback()
    {
        groupedFeedbackPoints = 0;
        feedbackRemaining = 0;
        if (scoreFeedback != null) { scoreFeedback.text = ""; scoreFeedback.gameObject.SetActive(false); }
    }

    private void AddPointsOnInterval()
    {
        // Compatibility only. No caller, timer or late Invoke may grant passive points.
    }

    private void UpdatePointsText()
    {
        if (pointsText != null)
            pointsText.text = currentPoints.ToString();
    }

    public void ResetPoints()
    {
        InitializeBalance();
        ClearScoreFeedback();
        feedbackPaused = false;
        unchecked { scoringGeneration++; }
        currentPoints = 0;
        GameSettings.LastScore = 0;
        GameSettings.LastMatch = null;
        matchOwner = null;
        matchStarted = Time.timeSinceLevelLoadAsDouble;
        isScoring = true;
        UpdatePointsText();
        CancelInvoke(nameof(AddPointsOnInterval));
    }

    private void InitializeBalance()
    {
        if (balanceInitialized)
            return;
        balanceInitialized = true;
        if (balanceConfig == null || balanceConfig.scoring == null)
            return;
        float duration = balanceConfig.scoring.feedbackDuration;
        feedbackDuration = !float.IsNaN(duration) && !float.IsInfinity(duration) && duration > 0 ? duration : 1f;
        pointsToAdd = balanceConfig.scoring.pointsToAdd;
        updateInterval = balanceConfig.scoring.updateInterval;
    }

    public void StopScoring()
    {
        ClearScoreFeedback();
        if (!isScoring)
            return;

        isScoring = false;
        CancelInvoke(nameof(AddPointsOnInterval));
        GameSettings.LastScore = currentPoints;
        GameSettings.LastMatch = matchOwner == null ? null : matchOwner.CompleteMatch(
            System.Math.Max(0, Time.timeSinceLevelLoadAsDouble - matchStarted), feedbackDuration);
    }
}
