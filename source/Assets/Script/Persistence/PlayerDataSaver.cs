using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDataSaver : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button continueButton;
    [SerializeField] private SceneSwitcher sceneSwitcher;

    private string filePath;
    private ScoreCollection scoreCollection;
    private string originalQuestion;
    private Color originalQuestionColor;
    private bool isSaving;
    private bool hasSaved;
    private bool navigationStarted;

    // Retained for existing callers; highlighting uses the committed record ID.
    public static string CurrentPlayerName;
    internal static string CurrentEntryId { get; private set; }

    internal static void ClearCurrentRecord()
    {
        CurrentPlayerName = null;
        CurrentEntryId = null;
    }

    private void Awake()
    {
        filePath = Application.persistentDataPath + "/scoreboard.json";
        if (feedbackText != null)
        {
            originalQuestion = feedbackText.text;
            originalQuestionColor = feedbackText.color;
        }
        LoadScoreData();
    }

    private void OnEnable()
    {
        if (playerNameInput != null)
            playerNameInput.onValueChanged.AddListener(OnNameChanged);
    }

    private void OnDisable()
    {
        if (playerNameInput != null)
            playerNameInput.onValueChanged.RemoveListener(OnNameChanged);
    }

    private void OnNameChanged(string value)
    {
        RestoreQuestion();
    }

    private void RestoreQuestion()
    {
        if (feedbackText != null)
        {
            feedbackText.text = originalQuestion;
            feedbackText.color = originalQuestionColor;
        }
    }

    private void ShowFeedback(string message)
    {
        if (feedbackText != null)
        {
            feedbackText.text = message;
            feedbackText.color = Color.red;
        }
    }

    private bool LoadScoreData()
    {
        try
        {
            string json = File.ReadAllText(filePath, new UTF8Encoding(false, true));
            scoreCollection = ScoreCollection.ParseChecked(json);
            return true;
        }
        catch (FileNotFoundException)
        {
            scoreCollection = new ScoreCollection();
            return true;
        }
        catch (DirectoryNotFoundException)
        {
            scoreCollection = new ScoreCollection();
            return true;
        }
        catch (Exception ex)
        {
            scoreCollection = null;
            ShowFeedback("Não foi possível ler os recordes. Verifique o arquivo e tente novamente.");
            Debug.LogWarning("Error loading score data; original file preserved: " + ex.Message);
            return false;
        }
    }

    public void SavePlayerData()
    {
        TrySavePlayerData();
    }

    public void ConfirmAndContinue()
    {
        if (navigationStarted || isSaving)
            return;
        if (sceneSwitcher == null)
        {
            ShowFeedback("Não foi possível continuar. Tente novamente.");
            Debug.LogWarning("Score confirmation has no SceneSwitcher assigned.");
            return;
        }
        if (!TrySavePlayerData())
            return;

        navigationStarted = true;
        if (continueButton != null)
            continueButton.interactable = false;
        sceneSwitcher.LoadScoreBoardScene();
    }

    private bool TrySavePlayerData()
    {
        if (hasSaved)
            return true;
        if (isSaving)
            return false;

        isSaving = true;
        try
        {
            RestoreQuestion();
            string playerName = playerNameInput != null ? playerNameInput.text.Trim() : "";
            if (string.IsNullOrEmpty(playerName))
            {
                ShowFeedback("Digite seu nome para salvar a pontuação.");
                return false;
            }
            if (!LoadScoreData())
                return false;

            string id;
            do { id = Guid.NewGuid().ToString("N"); }
            while (scoreCollection.entries.Exists(entry => string.Equals(entry.id, id, StringComparison.Ordinal)));

            var completed = GameSettings.LastMatch;
            var newEntry = new ScoreEntry
            {
                id = id,
                nome = playerName,
                dificuldade = (completed == null ? GameSettings.Difficulty : (int)completed.Grade) + "º ano",
                pontos = GameSettings.LastScore,
                date = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                match = completed?.Metadata
            };
            var candidate = new ScoreCollection { entries = new List<ScoreEntry>(scoreCollection.entries) };
            candidate.entries.Add(newEntry);
            if (!SaveScoreData(candidate))
                return false;

            scoreCollection = candidate;
            hasSaved = true;
            CurrentPlayerName = playerName;
            CurrentEntryId = id;
            if (ReferenceEquals(GameSettings.LastMatch, completed)) GameSettings.LastMatch = null;
            return true;
        }
        finally
        {
            isSaving = false;
        }
    }

    private bool SaveScoreData(ScoreCollection candidate)
    {
        string temporaryPath = null;
        bool temporaryCreated = false;
        try
        {
            string destination = Path.GetFullPath(filePath);
            string directory = Path.GetDirectoryName(destination);
            Directory.CreateDirectory(directory);
            temporaryPath = Path.Combine(directory, ".scoreboard-" + Guid.NewGuid().ToString("N") + ".tmp");
            using (var stream = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                temporaryCreated = true;
                using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(candidate.ToJson());
                    writer.Flush();
                    stream.Flush(true);
                }
            }
            if (File.Exists(destination))
                File.Replace(temporaryPath, destination, destination + ".bak");
            else
                File.Move(temporaryPath, destination);
            Debug.Log("Score data saved to: " + destination);
            return true;
        }
        catch (Exception ex)
        {
            ShowFeedback("Não foi possível salvar. Sua pontuação foi mantida; tente novamente.");
            Debug.LogWarning("Error saving score data; confirmation cancelled: " + ex.Message);
            return false;
        }
        finally
        {
            if (temporaryCreated && File.Exists(temporaryPath))
            {
                try { File.Delete(temporaryPath); }
                catch (Exception ex) { Debug.LogWarning("Could not remove score temporary file: " + ex.Message); }
            }
        }
    }
}
