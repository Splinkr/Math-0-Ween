using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using TMPro;

public class ScoreboardDisplay : MonoBehaviour
{
    [Header("UI Elements")]
    public Transform contentPanel;
    public GameObject scoreEntryPrefab;

    private string filePath;
    private ScoreCollection scoreCollection;

    private void Awake()
    {
        filePath = Application.persistentDataPath + "/scoreboard.json";
    }

    private void Start()
    {
        LoadScores();
        DisplayScores();
    }

    private void LoadScores()
    {
        if (File.Exists(filePath))
        {
            try
            {
                string json = File.ReadAllText(filePath, new UTF8Encoding(false, true));
                scoreCollection = ScoreCollection.ParseChecked(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Error loading scores; original file preserved: " + ex.Message);
                scoreCollection = new ScoreCollection();
            }
        }
        else
        {
            scoreCollection = new ScoreCollection();
        }
    }

    private void DisplayScores()
    {
        var sortedEntries = scoreCollection.entries
            .OrderByDescending(entry => entry.pontos)
            .ToList();

        foreach (Transform child in contentPanel) Destroy(child.gameObject);

        for (int i = 0; i < sortedEntries.Count; i++)
        {
            ScoreEntry entry = sortedEntries[i];
            GameObject entryGO = Instantiate(scoreEntryPrefab, contentPanel);
            ScoreEntryController controller = entryGO.GetComponent<ScoreEntryController>();

            if (controller != null)
            {
                // Pass entry.dificuldade instead of entry.escola
                controller.SetEntryValues(i + 1, entry.pontos, entry.nome, entry.dificuldade);

                if (!string.IsNullOrEmpty(PlayerDataSaver.CurrentEntryId) && string.Equals(entry.id, PlayerDataSaver.CurrentEntryId, StringComparison.Ordinal))
                {
                    controller.HighlightEntry(Color.green);
                }
            }
        }
    }
}
