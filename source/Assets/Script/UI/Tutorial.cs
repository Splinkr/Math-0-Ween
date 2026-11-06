using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Tutorial : MonoBehaviour
{
    [Header("Pause Settings")]
    public GameObject pausePanel;
    private bool isGamePaused = false;
    [SerializeField, Min(0f)] private float resumeCountdownDuration = 3f;
    [SerializeField] private TMP_Text resumeCountdownText;
    [SerializeField] private TMP_Text[] operationExamples = new TMP_Text[0];
    private readonly Dictionary<TMP_Text, bool> pausedTextStates = new Dictionary<TMP_Text, bool>();
    private Coroutine resumeRoutine;

    void Update()
    {
        if (resumeRoutine != null)
            return;
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            if (!isGamePaused)
                FreezeGame();
            else
                UnfreezeGame();
        }
    }

    public void FreezeGame()
    {
        if (isGamePaused)
            return;
        isGamePaused = true;
        Time.timeScale = 0f;
        foreach (TMP_Text text in operationExamples)
        {
            if (text == null)
                continue;
            if (!pausedTextStates.ContainsKey(text))
                pausedTextStates.Add(text, text.enabled);
            text.enabled = false;
        }
        if (pausePanel != null)
            pausePanel.SetActive(true);
        Debug.Log("Game is paused.");
    }

    public void UnfreezeGame()
    {
        if (!isGamePaused || resumeRoutine != null)
            return;
        if (pausePanel != null)
            pausePanel.SetActive(false);
        float duration = Mathf.Max(0f, resumeCountdownDuration);
        if (duration == 0f)
        {
            CompleteResume();
            return;
        }
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(true);
        resumeRoutine = StartCoroutine(ResumeCountdown(duration));
    }

    private IEnumerator ResumeCountdown(float duration)
    {
        double end = Time.realtimeSinceStartupAsDouble + duration;
        int displayed = -1;
        while (Time.realtimeSinceStartupAsDouble < end)
        {
            int remaining = Mathf.CeilToInt((float)(end - Time.realtimeSinceStartupAsDouble));
            if (remaining != displayed && resumeCountdownText != null)
                resumeCountdownText.SetText("{0}", remaining);
            displayed = remaining;
            yield return null;
        }
        resumeRoutine = null;
        CompleteResume();
    }

    private void RestoreExamples()
    {
        foreach (var entry in pausedTextStates)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }
        pausedTextStates.Clear();
    }

    private void CompleteResume()
    {
        RestoreExamples();
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);
        isGamePaused = false;
        Time.timeScale = 1f;
        Debug.Log("Game resumed.");
    }

    private void CancelPauseTransition()
    {
        if (resumeRoutine != null)
        {
            StopCoroutine(resumeRoutine);
            resumeRoutine = null;
        }
        RestoreExamples();
        if (resumeCountdownText != null)
            resumeCountdownText.gameObject.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (isGamePaused)
            Time.timeScale = 1f;
        isGamePaused = false;
    }

    private void OnDisable()
    {
        CancelPauseTransition();
    }

    private void OnDestroy()
    {
        CancelPauseTransition();
    }
}