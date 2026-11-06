using UnityEngine;
using UnityEngine.SceneManagement;





public class SceneSwitcher : MonoBehaviour
{
    // Set via UI events (like button clicks or via a level chooser)
    public int difficultyToLoad = 0;

    private void Awake()
    {
        string scene = gameObject.scene.name;
        if (scene == "Menu" || scene == "Gameplay")
            PlayerDataSaver.ClearCurrentRecord();
        if (scene == "Menu" || scene == "Gameplay" || scene == "Tutorial" || scene == "Tutorial 1")
            GameSettings.LastMatch = null;
    }

    // Sets the grade phase using the static GameSettings.
    public void SetGradePhaseFromInt(int phaseValue)
    {
        // Convert the integer value to a GradePhase.
        // Ensure the integer value maps to a valid enum value.
        GameSettings.GradePhase = (GradePhase)phaseValue;
        Debug.Log("Grade Phase set to: " + GameSettings.GradePhase);
    }

    // Load the Gameplay scene. The grade phase controls the math problem generation.
    public void LoadGameplayScene()
    {
        Debug.Log("Loading Gameplay Scene with difficulty: " + (int)GameSettings.GradePhase +
                  " and Grade Phase: " + GameSettings.GradePhase);

        // Use the GradePhase value (2, 3, 4, or 5) as the difficulty level
        GameSettings.Difficulty = (int)GameSettings.GradePhase;

        // Ensure the game is unpaused before switching scenes.
        Time.timeScale = 1f;

        // Now load the Gameplay scene.
        SceneManager.LoadScene("Gameplay");
    }




    public void LoadMenuScene()
    {
        Debug.Log("Loading Menu Scene...");
        // Reset the time scale to ensure the game isn't frozen upon returning to the menu.
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    // New method: Load the ScoreBoard scene.
    public void LoadScoreBoardScene()
    {
        Debug.Log("Loading ScoreBoard Scene...");
        // Ensure we reset time scale before switching scenes.
        Time.timeScale = 1f;
        SceneManager.LoadScene("ScoreBoard"); // Ensure the scene "ScoreBoard" is added to your Build Settings.
    }

    public void LoadTutorialScene()
    {
        Debug.Log("Loading Tutorial Scene...");
        Time.timeScale = 1f; // Ensure game isn't paused when transitioning.
        SceneManager.LoadScene("Tutorial"); // Replace "Tutorial" with the actual scene name in Unity.
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
}
