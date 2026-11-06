using System;
using UnityEngine;
using SpecialEnemyType = OperationEnemySpawner.SpecialEnemyType;

// Synchronous effects only: no scene references, stored state or credit scheduling.
internal static class SpecialEnemyEffects
{
    // Rewards retain their answer-time activation; Golden points belong solely to ScoreRules.
    internal static void ApplyRewards(SpecialEnemyType type, Action heal, Action<float> freeze,
        Action<int> charge, Func<float> blueDuration)
    {
        Apply(type, 0, heal, freeze, charge, null, blueDuration);
    }

    internal static int Apply(SpecialEnemyType type, int reward, Action heal,
        Action<float> freeze, Action<int> charge, Func<float> goldenMultiplier, Func<float> blueDuration)
    {
        switch (type)
        {
            case SpecialEnemyType.DarkRed:
                Debug.Log("Dark Red special enemy effect: Healing Mage.");
                heal();
                charge(1);
                break;
            case SpecialEnemyType.Golden:
                Debug.Log("Golden special enemy effect: Charging bat attack bar.");
                if (goldenMultiplier != null)
                    reward = MathAnswerEvaluator.ApplyMultiplier(reward, goldenMultiplier());
                charge(1);
                break;
            case SpecialEnemyType.Blue:
                Debug.Log("Blue special enemy effect: Activating Freeze Mode with custom duration.");
                freeze(blueDuration());
                charge(1);
                break;
            case SpecialEnemyType.Silver:
                Debug.Log("Silver special enemy effect: Granting 10 Bat Attack Bar points.");
                charge(10);
                break;
            default:
                charge(1);
                break;
        }
        return reward;
    }
}
