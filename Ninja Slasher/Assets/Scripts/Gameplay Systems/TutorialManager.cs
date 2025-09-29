using UnityEngine;
using TMPro;
using System.Collections;

public class TutorialManager : MonoBehaviourSingleton<TutorialManager>
{
    [Header("UI REFERENCES")]
    [SerializeField] private GameObject[] tutorialTexts;

    [Header("INDICATORS")]
    [SerializeField] private GameObject handAnimation;

    [Header("REFERENCES")]
    [SerializeField] private Controller playerController;

    [Header("SETTINGS")]
    [SerializeField] private bool skipTutorial = false;
    [SerializeField] private float textDisplayTime = 5f;

    private bool tutorialActive = false;
    private bool hasPerformedFirstDash = false;
    private bool hasKilledFirstEnemy = false;
    private int enemiesKilledCount = 0;

    private int currentTextIndex = 0;
    private bool waitingForDash = false;
    private bool waitingForEnemyKill = false;

    private void Start()
    {
        if (skipTutorial)
        {
            DisableTutorial();
            return;
        }

        StartTutorial();
    }

    public void StartTutorial()
    {
        tutorialActive = true;
        currentTextIndex = 0;

        if (playerController != null)
        {
            playerController.SetInputEnabled(false);
        }

        HideAllTexts();
        ShowCurrentText();
    }

    private void ShowCurrentText()
    {
        if (currentTextIndex >= tutorialTexts.Length)
        {
            CompleteTutorial();
            return;
        }

        HideAllTexts();

        if (tutorialTexts[currentTextIndex] != null)
        {
            tutorialTexts[currentTextIndex].SetActive(true);
        }

        ConfigureTextBehavior(currentTextIndex);
    }

    private void ConfigureTextBehavior(int textIndex)
    {
        switch (textIndex)
        {
            case 0:
                ShowHandAnimation(true);
                if (playerController != null)
                {
                    playerController.SetInputEnabled(true);
                }
                waitingForDash = true;
                break;

            case 1:
                HideHandAnimation();
                waitingForEnemyKill = true;
                break;

            case 2:
                StartCoroutine(HideTextAfterDelay(textDisplayTime, () => {
                    waitingForEnemyKill = true;
                }));
                break;

            case 3:
                StartCoroutine(HideTextAfterDelay(2f, () => {
                    CompleteTutorial();
                }));
                break;
        }
    }

    private IEnumerator HideTextAfterDelay(float delay, System.Action onComplete = null)
    {
        yield return new WaitForSecondsRealtime(delay);

        if (currentTextIndex < tutorialTexts.Length && tutorialTexts[currentTextIndex] != null)
        {
            tutorialTexts[currentTextIndex].SetActive(false);
        }

        onComplete?.Invoke();
    }

    private void CheckForActionCompletion()
    {
        if (waitingForDash && hasPerformedFirstDash)
        {
            waitingForDash = false;

            if (tutorialTexts[currentTextIndex] != null)
            {
                tutorialTexts[currentTextIndex].SetActive(false);
            }

            currentTextIndex++;
            ShowCurrentText();
        }

        if (waitingForEnemyKill && hasKilledFirstEnemy)
        {
            waitingForEnemyKill = false;

            if (tutorialTexts[currentTextIndex] != null)
            {
                tutorialTexts[currentTextIndex].SetActive(false);
            }

            currentTextIndex++;
            ShowCurrentText();
        }
    }

    public void OnDashPerformed()
    {
        if (!tutorialActive)
        {
            return;
        }

        if (!hasPerformedFirstDash)
        {
            hasPerformedFirstDash = true;
            CheckForActionCompletion();
        }
    }

    public void OnEnemyKilled()
    {
        if (!tutorialActive)
        {
            return;
        }

        enemiesKilledCount++;

        if (enemiesKilledCount == 1 && !hasKilledFirstEnemy)
        {
            hasKilledFirstEnemy = true;
            CheckForActionCompletion();
        }
    }

    private void CompleteTutorial()
    {
        tutorialActive = false;

        HideAllTexts();
        HideHandAnimation();

        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
    }

    public void DisableTutorial()
    {
        tutorialActive = false;

        waitingForDash = false;
        waitingForEnemyKill = false;
        hasPerformedFirstDash = false;
        hasKilledFirstEnemy = false;
        enemiesKilledCount = 0;
        currentTextIndex = 0;

        StopAllCoroutines();

        HideAllTexts();
        HideHandAnimation();

        if (playerController != null)
        {
            playerController.SetInputEnabled(true);
        }
    }

    private void HideAllTexts()
    {
        for (int i = 0; i < tutorialTexts.Length; i++)
        {
            if (tutorialTexts[i] != null)
            {
                tutorialTexts[i].SetActive(false);
            }
        }
    }

    private void ShowHandAnimation(bool show)
    {
        if (handAnimation != null)
        {
            handAnimation.SetActive(show);
        }
    }

    private void HideHandAnimation()
    {
        ShowHandAnimation(false);
    }

    public bool IsTutorialActive()
    {
        return tutorialActive;
    }
}