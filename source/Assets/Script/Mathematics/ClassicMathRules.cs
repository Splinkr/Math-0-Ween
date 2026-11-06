using System.Collections.Generic;
using UnityEngine;

// Safe classic curriculum fallback when no valid configured profile is available.
internal static class ClassicMathRules
{
    internal readonly struct OperandRules
    {
        internal readonly int Min;
        internal readonly int Max;
        internal readonly int MaximumResult;

        internal OperandRules(int min, int max, int maximumResult = int.MaxValue)
        {
            Min = min;
            Max = max;
            MaximumResult = maximumResult;
        }
    }

    internal static List<OperationType> AllowedOperations(GradePhase grade)
    {
        var operations = new List<OperationType>();
        switch (grade)
        {
            case GradePhase.Second:
                operations.Add(OperationType.Addition);
                break;
            case GradePhase.Third:
                operations.Add(OperationType.Addition);
                operations.Add(OperationType.Subtraction);
                break;
            case GradePhase.Fourth:
                operations.Add(OperationType.Addition);
                operations.Add(OperationType.Subtraction);
                operations.Add(OperationType.Multiplication);
                break;
            case GradePhase.Fifth:
                operations.Add(OperationType.Addition);
                operations.Add(OperationType.Subtraction);
                operations.Add(OperationType.Multiplication);
                operations.Add(OperationType.Division);
                break;
            default:
                Debug.LogWarning("Unexpected gradePhase, defaulting to Addition only.");
                operations.Add(OperationType.Addition);
                break;
        }
        return operations;
    }

    internal static OperandRules Addition(GradePhase grade)
    {
        switch (grade)
        {
            case GradePhase.Second: return new OperandRules(1, 9, 10);
            case GradePhase.Third: return new OperandRules(1, 9, 20);
            case GradePhase.Fourth: return new OperandRules(1, 20);
            case GradePhase.Fifth: return new OperandRules(1, 30);
            default: return new OperandRules(1, 1);
        }
    }

    internal static OperandRules Subtraction(GradePhase grade)
    {
        switch (grade)
        {
            case GradePhase.Third: return new OperandRules(1, 9);
            case GradePhase.Fourth: return new OperandRules(1, 20);
            case GradePhase.Fifth: return new OperandRules(1, 30);
            default: return new OperandRules(1, 1);
        }
    }

    internal static OperandRules Multiplication(GradePhase grade)
    {
        return new OperandRules(1, grade == GradePhase.Fourth || grade == GradePhase.Fifth ? 9 : 1);
    }

    internal static OperandRules Division() => new OperandRules(1, 9);
}
