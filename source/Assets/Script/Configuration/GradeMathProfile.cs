using System;
using UnityEngine;

/// <summary>Curriculum parameters, copied once by the generator for each match.</summary>
[CreateAssetMenu(fileName = "GradeMathProfile", menuName = "MathFall/Configuration/Grade Math Profile")]
public sealed class GradeMathProfile : ScriptableObject
{
    [Serializable]
    public sealed class OperationSettings
    {
        [Tooltip("Must be an operation allowed for this school grade. Zero weight disables it.")] public OperationType operation;
        [Min(0f), Tooltip("Relative weight; the initial allowed operations all have weight 1.")] public float weight = 1f;
        [Tooltip("Addition/subtraction/multiplication: first operand. Division: divisor.")] public int firstMin = 1;
        public int firstMax = 9;
        [Tooltip("Addition/subtraction/multiplication: second operand. Division: quotient; dividend is divisor × quotient.")] public int secondMin = 1;
        public int secondMax = 9;
        [Tooltip("Optional lower result restriction. Gameplay always requires a nonnegative result.")] public bool limitMinimumResult;
        public int minimumResult;
        public bool limitMaximumResult;
        public int maximumResult;
        [Tooltip("Division remains integer/exact. False does not enable fractional answers.")] public bool exactDivision;
    }

    [Serializable]
    public sealed class FutureProgressionSettings
    {
        [Tooltip("Continuously expand upper bounds after the configured scaled scene time.")] public bool enabled;
        [Min(0f), Tooltip("Scaled scene seconds before expansion begins; pauses do not count.")] public float startAfterSeconds = 180f;
        [Min(0f), Tooltip("Fraction of each INITIAL upper bound added per minute: 0.2 means 20%. Lower bounds stay fixed. Basic operands cap at 99; results/dividends at 9999.")] public float growthPerMinute = 0.2f;
    }

    [Header("Curriculum")]
    public GradePhase grade = GradePhase.Second;
    [Tooltip("Reserved negative policy. Gameplay blocks negative operands/results until signed input is supported.")] public bool allowNegative;
    [Tooltip("Operand ranges and optional result limits are the curricular limits.")]
    public OperationSettings[] operations = new OperationSettings[0];
    [Header("Numeric progression")]
    public FutureProgressionSettings futureProgression = new FutureProgressionSettings();
}
