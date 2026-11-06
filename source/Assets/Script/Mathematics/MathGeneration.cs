using System;
using System.Collections.Generic;

// Match-local profile sampling and legacy arithmetic helpers, using only Math RNG.
internal static class MathGeneration
{
    internal const string RulesVersion = "math-rules-v1";
    internal const int MaximumOperand = 99;
    internal const int MaximumResult = 9999;

    internal readonly struct Candidate
    {
        internal readonly int First, Second, Result;
        internal readonly OperationType Operation;
        internal readonly string Expression;
        internal Candidate(int first, int second, int result, OperationType operation)
        {
            First = first; Second = second; Result = result; Operation = operation;
            string symbol = operation == OperationType.Addition ? "+" : operation == OperationType.Subtraction ? "-"
                : operation == OperationType.Multiplication ? "×" : "÷";
            Expression = Format(first, symbol, second);
        }
    }

    internal sealed class Pool
    {
        internal readonly OperationType Operation;
        internal readonly double Weight;
        internal readonly List<Candidate> Candidates = new List<Candidate>();
        internal Pool(OperationType operation, double weight) { Operation = operation; Weight = weight; }
    }

    // Plain, match-local snapshot and bounded cache; never holds a mutable asset reference.
    internal sealed class Profile
    {
        private readonly GradeMathProfile.OperationSettings[] rules;
        private readonly bool progression;
        private readonly double start, rate;
        private int[] cachedBounds;
        private Pool[] cachedPools;

        internal Profile(GradeMathProfile.OperationSettings[] rules, bool progression, double start, double rate)
        {
            this.rules = rules; this.progression = progression; this.start = start; this.rate = rate;
        }

        internal string Describe(GradePhase grade)
        {
            bool advancing = progression && rate > 0;
            var description = new MatchConfiguration().Add("grade", (int)grade).Add("progression", advancing);
            if (advancing) description.Add("start", start).Add("rate", rate);
            foreach (var rule in rules)
            {
                if (rule.weight == 0) continue;
                description.Add("operator", (int)rule.operation).Add("weight", rule.weight)
                    .Add("firstMin", Math.Max(rule.operation == OperationType.Division ? 1 : 0, rule.firstMin)).Add("firstMax", rule.firstMax)
                    .Add("secondMin", Math.Max(0, rule.secondMin)).Add("secondMax", rule.secondMax)
                    .Add("resultMin", rule.limitMinimumResult ? Math.Max(0, rule.minimumResult) : 0)
                    .Add("resultMax", rule.limitMaximumResult ? rule.maximumResult : MaximumResult);
            }
            return description.Description;
        }

        internal Pool[] Pools(double seconds)
        {
            double minutes = progression && !double.IsNaN(seconds) ? Math.Max(0, seconds - start) / 60 : 0;
            var bounds = new int[rules.Length * 3];
            bool same = cachedBounds != null;
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                bounds[i * 3] = Expand(rule.firstMax, minutes, MaximumOperand);
                bounds[i * 3 + 1] = Expand(rule.secondMax, minutes, MaximumOperand);
                bounds[i * 3 + 2] = rule.limitMaximumResult ? Expand(rule.maximumResult, minutes, MaximumResult) : MaximumResult;
            }
            if (same)
                for (int i = 0; i < bounds.Length; i++)
                    if (bounds[i] != cachedBounds[i]) { same = false; break; }
            if (same) return cachedPools;

            var pools = new List<Pool>();
            for (int i = 0; i < rules.Length; i++)
            {
                var rule = rules[i];
                if (rule.weight == 0) continue;
                var pool = new Pool(rule.operation, rule.weight);
                int minimum = rule.limitMinimumResult ? Math.Max(0, rule.minimumResult) : 0;
                for (int first = Math.Max(0, rule.firstMin); first <= bounds[i * 3]; first++)
                    for (int second = Math.Max(0, rule.secondMin); second <= bounds[i * 3 + 1]; second++)
                    {
                        if (rule.operation == OperationType.Division && first == 0) continue;
                        // Bounded inputs make every arithmetic operation safe before casting.
                        long result = rule.operation == OperationType.Addition ? (long)first + second
                            : rule.operation == OperationType.Subtraction ? (long)first - second
                            : rule.operation == OperationType.Multiplication ? (long)first * second : second;
                        long displayedFirst = rule.operation == OperationType.Division ? (long)first * second : first;
                        if (result < minimum || result > bounds[i * 3 + 2] || displayedFirst > MaximumResult) continue;
                        pool.Candidates.Add(new Candidate((int)displayedFirst, rule.operation == OperationType.Division ? first : second, (int)result, rule.operation));
                    }
                pools.Add(pool);
            }
            cachedBounds = bounds; cachedPools = pools.ToArray();
            return cachedPools;
        }

        private int Expand(int initial, double minutes, int cap)
        {
            if (minutes <= 0 || rate == 0 || initial <= 0) return initial;
            double upper = initial * (1 + rate * minutes);
            return upper >= cap ? cap : (int)Math.Floor(upper);
        }
    }

    internal static Profile PrepareProfile(GradeMathProfile source, GradePhase grade, out string reason)
    {
        reason = "profile missing";
        if (source == null) return null;
        reason = "invalid profile configuration";
        if (source.grade != grade || source.operations == null || source.operations.Length == 0) return null;
        var allowed = ClassicMathRules.AllowedOperations(grade);
        var seen = new HashSet<OperationType>();
        var copy = new List<GradeMathProfile.OperationSettings>();
        foreach (var rule in source.operations)
        {
            if (rule == null || !allowed.Contains(rule.operation) || !seen.Add(rule.operation)
                || float.IsNaN(rule.weight) || float.IsInfinity(rule.weight) || rule.weight < 0
                || rule.firstMin > rule.firstMax || rule.secondMin > rule.secondMax
                || rule.firstMin < -MaximumOperand || rule.firstMax > MaximumOperand
                || rule.secondMin < -MaximumOperand || rule.secondMax > MaximumOperand
                || (rule.limitMinimumResult && (rule.minimumResult < -MaximumResult || rule.minimumResult > MaximumResult))
                || (rule.limitMaximumResult && (rule.maximumResult < 0 || rule.maximumResult > MaximumResult))
                || (rule.limitMinimumResult && rule.limitMaximumResult && rule.minimumResult > rule.maximumResult)) return null;
            copy.Add(new GradeMathProfile.OperationSettings {
                operation = rule.operation, weight = rule.weight, firstMin = rule.firstMin, firstMax = rule.firstMax,
                secondMin = rule.secondMin, secondMax = rule.secondMax, limitMinimumResult = rule.limitMinimumResult,
                minimumResult = rule.minimumResult, limitMaximumResult = rule.limitMaximumResult,
                maximumResult = rule.maximumResult, exactDivision = rule.exactDivision
            });
        }
        var p = source.futureProgression;
        if (p != null && (float.IsNaN(p.startAfterSeconds) || float.IsInfinity(p.startAfterSeconds) || p.startAfterSeconds < 0
            || float.IsNaN(p.growthPerMinute) || float.IsInfinity(p.growthPerMinute) || p.growthPerMinute < 0)) return null;
        var profile = new Profile(copy.ToArray(), p != null && p.enabled, p == null ? 180 : p.startAfterSeconds, p == null ? 0 : p.growthPerMinute);
        var pools = profile.Pools(0);
        if (pools.Length == 0) return null;
        foreach (var pool in pools) if (pool.Candidates.Count == 0) return null;
        reason = null;
        return profile;
    }

    internal static Profile ClassicProfile(GradePhase grade)
    {
        var rules = new List<GradeMathProfile.OperationSettings>();
        foreach (var operation in ClassicMathRules.AllowedOperations(grade))
        {
            var classic = operation == OperationType.Addition ? ClassicMathRules.Addition(grade)
                : operation == OperationType.Subtraction ? ClassicMathRules.Subtraction(grade)
                : operation == OperationType.Multiplication ? ClassicMathRules.Multiplication(grade) : ClassicMathRules.Division();
            rules.Add(new GradeMathProfile.OperationSettings {
                operation = operation, firstMin = classic.Min, firstMax = classic.Max, secondMin = classic.Min, secondMax = classic.Max,
                limitMaximumResult = classic.MaximumResult != int.MaxValue, maximumResult = classic.MaximumResult,
                exactDivision = operation == OperationType.Division
            });
        }
        return new Profile(rules.ToArray(), false, 180, 0);
    }

    internal static Candidate Draw(IRandomSource random, Pool[] pools, string[] recent)
    {
        var available = new int[pools.Length];
        double total = 0;
        for (int i = 0; i < pools.Length; i++)
        {
            foreach (var candidate in pools[i].Candidates)
                if (Array.IndexOf(recent, candidate.Expression) < 0) available[i]++;
            if (available[i] > 0) total += pools[i].Weight;
        }
        if (total > 0)
        {
            double ticket = random.Value * total;
            int selected = -1;
            for (int i = 0; i < pools.Length; i++)
            {
                if (available[i] == 0) continue;
                selected = i;
                ticket -= pools[i].Weight;
                if (ticket < 0) break;
            }
            int index = random.Range(0, available[selected]);
            foreach (var candidate in pools[selected].Candidates)
                if (Array.IndexOf(recent, candidate.Expression) < 0 && index-- == 0) return candidate;
        }
        else
        {
            int oldest = int.MaxValue;
            Candidate chosen = default;
            foreach (var pool in pools)
                foreach (var candidate in pool.Candidates)
                {
                    int lastUse = Array.LastIndexOf(recent, candidate.Expression);
                    if (lastUse < oldest) { oldest = lastUse; chosen = candidate; }
                }
            if (oldest != int.MaxValue) return chosen;
        }
        throw new InvalidOperationException("No valid mathematical candidates in prepared profile.");
    }

    internal static OperationType SelectOperation(IRandomSource random, List<OperationType> allowedOperations)
    {
        return allowedOperations[random.Range(0, allowedOperations.Count)];
    }

    internal static void Addition(IRandomSource random, ClassicMathRules.OperandRules rules, out int a, out int b, out int result)
    {
        int tries = 0;
        do
        {
            a = random.Range(rules.Min, rules.Max + 1);
            b = random.Range(rules.Min, rules.Max + 1);
            result = a + b;
            tries++;
        }
        while (result > rules.MaximumResult && tries < 100);
    }

    internal static void Subtraction(IRandomSource random, ClassicMathRules.OperandRules rules, bool allowNegative, out int a, out int b, out int result)
    {
        a = random.Range(rules.Min, rules.Max + 1);
        b = random.Range(rules.Min, rules.Max + 1);
        if (!allowNegative && b > a)
            (a, b) = (b, a);
        result = a - b;
    }

    internal static void Multiplication(IRandomSource random, ClassicMathRules.OperandRules rules, out int a, out int b, out int result)
    {
        a = random.Range(rules.Min, rules.Max + 1);
        b = random.Range(rules.Min, rules.Max + 1);
        result = a * b;
    }

    internal static void Division(IRandomSource random, ClassicMathRules.OperandRules rules, out int a, out int b, out int result)
    {
        do
        {
            b = random.Range(rules.Min, rules.Max + 1);
        }
        while (b == 0);

        int quotient = random.Range(rules.Min, rules.Max + 1);
        a = b * quotient;
        result = quotient;
    }

    internal static string Format(int a, string symbol, int b) => $"{a}{symbol}{b}=";
}
