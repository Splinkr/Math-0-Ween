using UnityEngine;
using TMPro;

public class ScoreEntryController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text positionText;
    public TMP_Text scoreText;
    public TMP_Text nameText;
    public TMP_Text difficultyText; // Renamed for clarity

    public void SetEntryValues(int position, int score, string playerName, string difficulty)
    {
        if (positionText != null) positionText.text = position.ToString();
        if (scoreText != null) scoreText.text = score.ToString();
        if (nameText != null) nameText.text = playerName;
        if (difficultyText != null) difficultyText.text = difficulty;
    }

    /// <summary>
    /// Changes the color of all text elements to the provided highlight color.
    /// </summary>
    public void HighlightEntry(Color highlightColor)
    {
        if (positionText != null)
            positionText.color = highlightColor;
        if (scoreText != null)
            scoreText.color = highlightColor;
        if (nameText != null)
            nameText.color = highlightColor;
        if (difficultyText != null)
            difficultyText.color = highlightColor;
    }
}