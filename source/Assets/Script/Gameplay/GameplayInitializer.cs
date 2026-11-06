using UnityEngine;

public class GameplayInitializer : MonoBehaviour
{
    // Drag your MathOperationGenerator (or similar spawner) into this field from the Inspector.
    public MathOperationGenerator operationSpawner;

    void OnEnable()
    {
        if (operationSpawner == null)
        {
            Debug.LogError("GameplayInitializer: MathOperationGenerator reference is not assigned.");
            return;
        }

        GradePhase currentPhase = GameSettings.GradePhase;
        if (!System.Enum.IsDefined(typeof(GradePhase), currentPhase))
        {
            currentPhase = operationSpawner.gradePhase;
            if (!System.Enum.IsDefined(typeof(GradePhase), currentPhase))
                currentPhase = GradePhase.Second;
            Debug.LogWarning("No valid grade selected; initializing gameplay with " + currentPhase + ".");
        }

        GameSettings.GradePhase = currentPhase;
        GameSettings.Difficulty = (int)currentPhase;
        operationSpawner.gradePhase = currentPhase;
        Debug.Log("Initializing gameplay with Difficulty: " + GameSettings.Difficulty + " and Grade Phase: " + currentPhase);
    }
}
