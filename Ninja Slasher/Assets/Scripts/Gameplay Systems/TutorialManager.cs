using UnityEngine;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI REFERENCES")]
    [SerializeField] private GameObject[] tutorialTextsLevel1;
    [SerializeField] private GameObject[] tutorialTextsLevel2;
    [SerializeField] private GameObject[] tutorialTextsLevel3;

    [Header("INDICATORS")]
    [SerializeField] private GameObject handAnimation;
    [SerializeField] private GameObject handAnimationParry;

    [Header("REFERENCES")]
    [SerializeField] private Controller playerController;

    [Header("SETTINGS")]
    // DEPRECATED
    //[SerializeField] private bool skipTutorial = false;
    //[SerializeField] private float textDisplayTime = 5f;
    [SerializeField] private int currentLevel = 1;

    private bool SkipTutorial => !GameConfigManager.Config.enableTutorial;
    private float TextDisplayTime => GameConfigManager.Config.tutorialTextDisplayTime;

    private bool tutorialActive = false;
    private int currentTextIndex = 0;
    private bool canCompleteParryTutorial = false;

    private bool hasPerformedFirstDash = false;
    private bool hasKilledFirstEnemy = false;
    private bool hasPerformedCombo = false;
    private int enemiesKilledCount = 0;
    private bool hasPerformedParry;

    private bool waitingForDash = false;
    private bool waitingForEnemyKill = false;
    private bool waitingForCombo = false;
    private bool waitingForParry;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (SkipTutorial)
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

        hasPerformedFirstDash = false;
        hasKilledFirstEnemy = false;
        hasPerformedCombo = false;
        hasPerformedParry = false;
        enemiesKilledCount = 0;
        canCompleteParryTutorial = false;

        waitingForDash = false;
        waitingForEnemyKill = false;
        waitingForCombo = false;
        waitingForParry = false;

        if (playerController != null)           //TODO: REVISAR
        { 
            //playerController.SetInputEnabled(false);
        }

        HideAllTexts();
        HideAllAnimations();
        ShowCurrentText();
    }

    private void ShowCurrentText()
    {
        GameObject[] currentTutorialTexts = GetCurrentTutorialTexts();

        if (currentTutorialTexts == null || currentTutorialTexts.Length == 0)
        {
            CompleteTutorial();
            return;
        }

        if (currentTextIndex >= currentTutorialTexts.Length)
        {
            CompleteTutorial();
            return;
        }

        if (currentLevel == 2)
        {
            ShowAllLevel2Texts();
        }
        else if (currentLevel == 3)
        {
            ShowAllLevel3Texts();
        }
        else
        {
            HideAllTexts();
            if (currentTutorialTexts[currentTextIndex] != null)
            {
                currentTutorialTexts[currentTextIndex].SetActive(true);
            }
        }

        ConfigureTextBehavior(currentTextIndex);
    }

    private void ShowAllLevel2Texts()
    {
        HideAllTexts();

        if (tutorialTextsLevel2 != null)
        {
            for (int i = 0; i < tutorialTextsLevel2.Length; i++)
            {
                if (tutorialTextsLevel2[i] != null)
                {
                    tutorialTextsLevel2[i].SetActive(true);
                }
            }
        }
    }

    private void ShowAllLevel3Texts()
    {
        HideAllTexts();

        if (tutorialTextsLevel3 != null)
        {
            for (int i = 0; i < tutorialTextsLevel3.Length; i++)
            {
                if (tutorialTextsLevel3[i] != null)
                {
                    tutorialTextsLevel3[i].SetActive(true);
                }
            }
        }
    }

    private void HideAllLevel2Texts()
    {
        if (tutorialTextsLevel2 != null)
        {
            for (int i = 0; i < tutorialTextsLevel2.Length; i++)
            {
                if (tutorialTextsLevel2[i] != null)
                {
                    tutorialTextsLevel2[i].SetActive(false);
                }
            }
        }
    }

    private void HideAllLevel3Texts()
    {
        if (tutorialTextsLevel3 != null)
        {
            for (int i = 0; i < tutorialTextsLevel3.Length; i++)
            {
                if (tutorialTextsLevel3[i] != null)
                {
                    tutorialTextsLevel3[i].SetActive(false);
                }
            }
        }
    }

    private GameObject[] GetCurrentTutorialTexts()
    {
        return currentLevel switch
        {
            2 => tutorialTextsLevel2,
            3 => tutorialTextsLevel3,
            _ => tutorialTextsLevel1
        };
    }

    private void ConfigureTextBehavior(int textIndex)
    {
        switch (currentLevel)
        {
            case 2:
                ConfigureLevel2Behavior(textIndex);
                break;
            case 3:
                ConfigureLevel3Behavior(textIndex);
                break;
            default:
                ConfigureLevel1Behavior(textIndex);
                break;
        }
    }

    private void ConfigureLevel1Behavior(int textIndex)
    {
        switch (textIndex)
        {
            case 0:
                if (handAnimation != null)
                {
                    handAnimation.SetActive(true);
                }
                if (playerController != null)           //TODO REVISAR
                {
                    //playerController.SetInputEnabled(true);
                }
                waitingForDash = true;
                break;

            case 1:
                if (playerController != null)
                {
                    //playerController.SetInputEnabled(true);
                }
                waitingForEnemyKill = true;
                break;

            case 2:
                if (playerController != null)
                {
                    //playerController.SetInputEnabled(true);
                }
                StartCoroutine(HideTextAfterDelay(TextDisplayTime, () => {
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

    private void ConfigureLevel2Behavior(int textIndex)
    {
        switch (textIndex)
        {
            case 0:
                if (playerController != null)       //TODO REVISAR
                {
                    //playerController.SetInputEnabled(true);
                }
                waitingForCombo = true;
                break;
        }
    }

    private void ConfigureLevel3Behavior(int textIndex)
    {
        switch (textIndex)
        {
            case 0:
                if (playerController != null)       
                {
                    //playerController.SetInputEnabled(true);
                }
                waitingForParry = true;
                StartCoroutine(EnableParryTutorialCompletion());

                if (handAnimationParry != null)
                {
                    handAnimationParry.SetActive(true);
                }
                break;
        }
    }

    private IEnumerator EnableParryTutorialCompletion()
    {
        yield return new WaitForSeconds(0.5f);
        canCompleteParryTutorial = true;
    }

    private IEnumerator HideTextAfterDelay(float delay, System.Action onComplete = null)
    {
        yield return new WaitForSecondsRealtime(delay);

        GameObject[] currentTutorialTexts = GetCurrentTutorialTexts();
        if (currentTextIndex < currentTutorialTexts.Length && currentTutorialTexts[currentTextIndex] != null)
        {
            currentTutorialTexts[currentTextIndex].SetActive(false);
        }

        onComplete?.Invoke();
    }

    private void CheckForActionCompletion()
    {
        GameObject[] currentTutorialTexts = GetCurrentTutorialTexts();

        if (waitingForDash && hasPerformedFirstDash)
        {
            waitingForDash = false;

            if (currentTextIndex < currentTutorialTexts.Length && currentTutorialTexts[currentTextIndex] != null)
            {
                currentTutorialTexts[currentTextIndex].SetActive(false);
            }

            currentTextIndex++;
            ShowCurrentText();
        }

        if (waitingForEnemyKill && hasKilledFirstEnemy)
        {
            waitingForEnemyKill = false;

            if (currentTextIndex < currentTutorialTexts.Length && currentTutorialTexts[currentTextIndex] != null)
            {
                currentTutorialTexts[currentTextIndex].SetActive(false);
            }

            currentTextIndex++;
            ShowCurrentText();
        }

        if (waitingForCombo && hasPerformedCombo)
        {
            waitingForCombo = false;

            HideAllLevel2Texts();

            CompleteTutorial();
        }

        if (waitingForParry && hasPerformedParry)
        {
            waitingForParry = false;

            if (handAnimationParry != null)
            {
                handAnimationParry.SetActive(false);
            }

            HideAllLevel3Texts();

            CompleteTutorial();
        }
    }

    public void OnDashPerformed()
    {
        if (!tutorialActive) return;

        if (!hasPerformedFirstDash)
        {
            hasPerformedFirstDash = true;

            StartCoroutine(DelayedAnimationHide());
        }
    }

    private IEnumerator DelayedAnimationHide()
    {
        yield return new WaitForSeconds(0.3f);

        HandSwipeAnimation[] allHandAnimations = FindObjectsOfType<HandSwipeAnimation>(true);

        foreach (var anim in allHandAnimations)
        {
            string path = GetGameObjectPath(anim.gameObject);
            anim.StopAnimation();
            anim.gameObject.SetActive(false);
        }

        if (handAnimation != null)
        {
            handAnimation.SetActive(false);
        }

        CheckForActionCompletion();
    }

    private string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        Transform current = obj.transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }
        return path;
    }

    public void OnEnemyKilled()
    {
        if (!tutorialActive) return;

        enemiesKilledCount++;

        if (enemiesKilledCount == 1 && !hasKilledFirstEnemy)
        {
            hasKilledFirstEnemy = true;
            CheckForActionCompletion();
        }
    }

    public void OnComboPerformed()
    {
        if (!tutorialActive || currentLevel != 2) return;

        if (!hasPerformedCombo)
        {
            hasPerformedCombo = true;
            CheckForActionCompletion();
        }
    }

    public void OnParryPerformed()
    {
        if (!tutorialActive || currentLevel != 3) return;
        if (!canCompleteParryTutorial) return;

        if (!hasPerformedParry)
        {
            hasPerformedParry = true;
            CheckForActionCompletion();
        }
    }

    private void CompleteTutorial()
    {
        tutorialActive = false;

        HideAllTexts();
        HideAllAnimations();

        if (playerController != null)       //TODO REVISAR
        {
            //playerController.SetInputEnabled(true);
        }
    }

    public void DisableTutorial()
    {
        tutorialActive = false;

        waitingForDash = false;
        waitingForEnemyKill = false;
        waitingForCombo = false;
        waitingForParry = false;
        hasPerformedFirstDash = false;
        hasKilledFirstEnemy = false;
        hasPerformedCombo = false;
        hasPerformedParry = false;
        canCompleteParryTutorial = false;
        enemiesKilledCount = 0;
        currentTextIndex = 0;

        StopAllCoroutines();

        HideAllTexts();
        HideAllAnimations();

        if (playerController != null) //TODO REVISAR
        {
            //playerController.SetInputEnabled(true);
        }
    }

    private void HideAllTexts()
    {
        if (tutorialTextsLevel1 != null)
        {
            for (int i = 0; i < tutorialTextsLevel1.Length; i++)
            {
                if (tutorialTextsLevel1[i] != null)
                {
                    tutorialTextsLevel1[i].SetActive(false);
                }
            }
        }

        if (tutorialTextsLevel2 != null)
        {
            for (int i = 0; i < tutorialTextsLevel2.Length; i++)
            {
                if (tutorialTextsLevel2[i] != null)
                {
                    tutorialTextsLevel2[i].SetActive(false);
                }
            }
        }

        if (tutorialTextsLevel3 != null)
        {
            for (int i = 0; i < tutorialTextsLevel3.Length; i++)
            {
                if (tutorialTextsLevel3[i] != null)
                {
                    tutorialTextsLevel3[i].SetActive(false);
                }
            }
        }
    }

    private void HideAllAnimations()
    {
        if (handAnimation != null)
        {
            handAnimation.SetActive(false);
        }

        if (handAnimationParry != null)
        {
            handAnimationParry.SetActive(false);
        }
    }

    public bool IsTutorialActive()
    {
        return tutorialActive;
    }

    public void SetCurrentLevel(int level)
    {
        currentLevel = level;
    }
}