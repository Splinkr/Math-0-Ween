using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicPlayer : MonoBehaviour
{
    private static MusicPlayer instance;
    private AudioSource audioSource;

    private void Awake()
    {
        // Singleton logic: keep only one instance across scenes.
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            // Get the AudioSource on this GameObject.
            audioSource = GetComponent<AudioSource>();

            // Ensure your AudioSource is set to loop if desired.
            audioSource.loop = true;

            // Subscribe to the sceneLoaded event.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // This method is called every time a new scene is loaded.
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Check if the loaded scene is your Gameplay scene.
        if (scene.name == "Gameplay")
        {
            // Pause the music in the Gameplay scene.
            audioSource.Pause();
        }
        else
        {
            // For all other scenes, resume playing.
            // If the audio isn't already playing, UnPause() will start it.
            audioSource.UnPause();
        }
    }
}