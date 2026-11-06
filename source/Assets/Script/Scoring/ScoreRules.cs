using System;

// Pure scoring rules. The facade still owns streak and credit timing.
internal static class ScoreRules
{
    internal const string RulesVersion = "score-rules-v1";
    internal const int BasePoints = 10;
    internal const int MaximumStreakBonus = 15;
    internal const double MaximumPressure = 2;
    internal const double MaximumComplexity = 1.2;
    internal const int GoldenMultiplier = 4;

    internal readonly struct Parameters
    {
        internal const int DefaultPressurePointsToMaximum = 80;
        internal const double DefaultAdvancedOperationBonus = 0.10;
        internal const double DefaultExtraDigitBonus = 0.02;
        internal readonly int PressurePointsToMaximum;
        internal readonly double AdvancedOperationBonus, ExtraDigitBonus;
        private readonly bool initialized;

        internal Parameters(int pressurePointsToMaximum, double advancedOperationBonus, double extraDigitBonus)
        {
            PressurePointsToMaximum = pressurePointsToMaximum > 0 ? pressurePointsToMaximum : DefaultPressurePointsToMaximum;
            AdvancedOperationBonus = ValidBonus(advancedOperationBonus) ? advancedOperationBonus : DefaultAdvancedOperationBonus;
            ExtraDigitBonus = ValidBonus(extraDigitBonus) ? extraDigitBonus : DefaultExtraDigitBonus;
            initialized = true;
        }

        internal static Parameters Default => new Parameters(DefaultPressurePointsToMaximum,
            DefaultAdvancedOperationBonus, DefaultExtraDigitBonus);
        internal Parameters OrDefault => initialized ? this : Default;
        private static bool ValidBonus(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }

    // Nonserialized metadata: default/missing values must not look like a generated 0+0=0.
    internal readonly struct Expression
    {
        internal readonly int First, Second, Result;
        internal readonly OperationType Operation;
        private readonly bool hasValue;

        internal Expression(int first, int second, int result, OperationType operation)
        {
            First = first; Second = second; Result = result; Operation = operation;
            hasValue = true;
        }

        internal bool IsValid
        {
            get
            {
                if (!hasValue || First < 0 || Second < 0 || Result < 0 || Result > MathGeneration.MaximumResult
                    || Second > MathGeneration.MaximumOperand) return false;
                if (Operation == OperationType.Division)
                    return First <= MathGeneration.MaximumResult && Second != 0 && Result <= MathGeneration.MaximumOperand
                        && First == (long)Second * Result;
                if (First > MathGeneration.MaximumOperand) return false;
                switch (Operation)
                {
                    case OperationType.Addition: return (long)First + Second == Result;
                    case OperationType.Subtraction: return (long)First - Second == Result;
                    case OperationType.Multiplication: return (long)First * Second == Result;
                    default: return false;
                }
            }
        }
    }

    internal static double Pressure(int arcadeLevel, int initialArcadeLevel, Parameters parameters)
    {
        parameters = parameters.OrDefault;
        long gained = Math.Max(0, arcadeLevel) - (long)Math.Max(0, initialArcadeLevel);
        return 1 + Math.Min(MaximumPressure - 1, Math.Max(0, gained) / (double)parameters.PressurePointsToMaximum);
    }

    internal static double Complexity(Expression expression, Parameters parameters)
    {
        if (!expression.IsValid) return 1;
        parameters = parameters.OrDefault;
        int digits = Digits(expression.First) + Digits(expression.Second) + Digits(expression.Result);
        double operationBonus = expression.Operation == OperationType.Multiplication || expression.Operation == OperationType.Division
            ? parameters.AdvancedOperationBonus : 0;
        return Math.Min(MaximumComplexity, 1 + operationBonus + parameters.ExtraDigitBonus * Math.Max(0, digits - 3));
    }

    private static int Digits(int value)
    {
        int count = 1;
        while (value >= 10) { value /= 10; count++; }
        return count;
    }

    // Streak is already updated by the caller. No mutation, intermediate rounding or RNG.
    internal static int Calculate(int updatedStreak, int arcadeLevel, int initialArcadeLevel,
        Expression expression, bool golden, bool eliminatedByZ, Parameters parameters)
    {
        if (eliminatedByZ) return 0;
        int basis = BasePoints + Math.Min(Math.Max(0, updatedStreak), MaximumStreakBonus);
        double reward = basis * Pressure(arcadeLevel, initialArcadeLevel, parameters)
            * Complexity(expression, parameters) * (golden ? GoldenMultiplier : 1);
        return (int)Math.Round(reward, MidpointRounding.ToEven);
    }

    internal static void Increment(ref int streak)
    {
        streak = streak < 0 ? 1 : streak < int.MaxValue ? streak + 1 : int.MaxValue;
    }

    // Historical compatibility seam. Production uses Increment + Calculate since phase 8B.
    internal static int Reward(ref int streak, int pointsPerEnemy, int difficulty,
        out int baseReward, out float multiplier)
    {
        streak++;
        baseReward = pointsPerEnemy + streak;
        multiplier = 1f + difficulty / 1000f;
        return MathAnswerEvaluator.ApplyMultiplier(baseReward, multiplier);
    }

    internal static void Reset(ref int streak)
    {
        streak = 0;
    }
}
