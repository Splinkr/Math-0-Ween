using System.Collections.Generic;
using UnityEngine;

public enum OperationType
{
    Addition,
    Subtraction,
    Multiplication,
    Division
}

public enum GradePhase
{
    // Note: These are now zero-based so that the default (0) is valid.
    // The comments indicate what school grade these represent.
    Second = 2,   // 2nd Grade (Add only, 1–9, result ≤ 10)
    Third = 3,    // 3rd Grade (Add/Sub, 1–9; add result ≤ 20; sub result ≥ 0)
    Fourth = 4,   // 4th Grade (Add/Sub/Multiplication)
    Fifth = 5     // 5th Grade (Add/Sub/Multiplication/Division; limits come from the profile)
}

public class MathOperationGenerator : MonoBehaviour
{
    private MatchRandomSession randomSession;
    public MatchRandomSession RandomSession => randomSession ?? (randomSession = MatchRandomSession.CreateConfigured());

    private void Awake()
    {
        _ = RandomSession;
    }

    // Explicit test/reproduction reset; does not reset gameplay state.
    public void InitializeRandom(ulong seed)
    {
        randomSession = new MatchRandomSession(seed);
        recentExpressions.Clear();
    }

    [Header("Settings")]
    [Tooltip("Legacy flag retained for compatibility. Gameplay blocks negatives until signed input is supported.")]
    public bool allowNegative = false;
    // Removed the operationTypeChoice—each grade defines its allowed operations.
    [Tooltip("Fallback grade only when GameSettings.Difficulty is invalid. The selected grade is captured once per match.")]
    public GradePhase gradePhase = GradePhase.Second;

    [SerializeField, Tooltip("Math profiles copied once per match. No config uses classic rules without numeric progression.")]
    private GameBalanceConfig balanceConfig;
    private MathGeneration.Profile mathProfile;
    private GradePhase capturedGrade;
    internal MatchRandomSession CapturedRandomSession => randomSession;
    internal GradePhase CapturedGrade => capturedGrade;
    internal string MathConfiguration => mathProfile?.Describe(capturedGrade);
    private readonly Queue<string> recentExpressions = new Queue<string>(3);

    private void InitializeMathProfile()
    {
        if (mathProfile != null)
            return;
        int selected = GameSettings.Difficulty;
        gradePhase = System.Enum.IsDefined(typeof(GradePhase), selected) ? (GradePhase)selected
            : System.Enum.IsDefined(typeof(GradePhase), gradePhase) ? gradePhase : GradePhase.Second;
        GameSettings.Difficulty = (int)gradePhase;
        capturedGrade = gradePhase;

        GradeMathProfile source = null;
        bool duplicate = false;
        if (balanceConfig != null && balanceConfig.mathProfiles != null)
            foreach (var candidate in balanceConfig.mathProfiles)
                if (candidate != null && candidate.grade == gradePhase)
                {
                    if (source != null) duplicate = true;
                    source = candidate;
                }
        string reason = duplicate ? "duplicate profiles for selected grade" : null;
        mathProfile = duplicate ? null : MathGeneration.PrepareProfile(source, gradePhase, out reason);
        if (mathProfile == null)
        {
            mathProfile = MathGeneration.ClassicProfile(gradePhase);
            Debug.LogWarning("Math profile fallback for " + gradePhase + ": " + reason + ". Using classic rules without progression.");
        }
        if (allowNegative || (source != null && source.allowNegative))
            Debug.LogWarning("Negative math is disabled in gameplay until signed input is supported.");
    }

    public struct OperationData
    {
        public string operationString;
        public int result;
        internal ScoreRules.Expression scoringExpression;
    }

    public OperationData GenerateOperation()
    {
        return GenerateOperation(Time.timeSinceLevelLoad);
    }

    // Explicit elapsed time is an internal seam for deterministic boundary tests.
    internal OperationData GenerateOperation(double elapsedSeconds)
    {
        InitializeMathProfile();
        var candidate = MathGeneration.Draw(RandomSession.Math, mathProfile.Pools(elapsedSeconds), recentExpressions.ToArray());
        if (recentExpressions.Count == 3) recentExpressions.Dequeue();
        recentExpressions.Enqueue(candidate.Expression);
        return new OperationData
        {
            operationString = candidate.Expression,
            result = candidate.Result,
            scoringExpression = new ScoreRules.Expression(candidate.First, candidate.Second, candidate.Result, candidate.Operation)
        };
    }

    // ────────────────────────────────────────────────────────────────
    // Grade-specific generators
    // ────────────────────────────────────────────────────────────────

    private void GenerateAddition(out int a, out int b, out int res)
    {
        MathGeneration.Addition(RandomSession.Math, ClassicMathRules.Addition(gradePhase), out a, out b, out res);
    }

    private void GenerateSubtraction(out int a, out int b, out int res)
    {
        MathGeneration.Subtraction(RandomSession.Math, ClassicMathRules.Subtraction(gradePhase), allowNegative, out a, out b, out res);
    }

    private void GenerateMultiplication(out int a, out int b, out int res)
    {
        MathGeneration.Multiplication(RandomSession.Math, ClassicMathRules.Multiplication(gradePhase), out a, out b, out res);
    }

    private void GenerateDivision(out int a, out int b, out int res)
    {
        MathGeneration.Division(RandomSession.Math, ClassicMathRules.Division(), out a, out b, out res);
    }
}
