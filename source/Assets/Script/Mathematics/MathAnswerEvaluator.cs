using UnityEngine;

// Stateless rules: Unity objects, input display and effects belong to the facade.
internal static class MathAnswerEvaluator
{
    internal static bool TryParse(string input, out int answer)
    {
        return int.TryParse(input, out answer);
    }

    internal static bool IsPreferredCandidate(float y, float lowestY)
    {
        return y < lowestY;
    }

    internal static int ApplyMultiplier(int reward, float multiplier)
    {
        return Mathf.RoundToInt(reward * multiplier);
    }
}
