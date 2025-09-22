using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultsUIManager : MonoBehaviourSingleton<ResultsUIManager>
{
    [SerializeField] private Transform _starsContainer;
    [SerializeField] private TextMeshProUGUI _primaryGoalText;
    [SerializeField] private TextMeshProUGUI[] _secondaryGoalTexts;

    [Header("STAR SPRITES")]
    [SerializeField] private Sprite _starNotAcquiredSprite;
    [SerializeField] private Sprite _starAcquiredSprite;

    private bool isObjectiveComplete = false;

    public void ShowResultsPanel()
    {
        int levelId = GetLevelIdFromSceneName(SceneManager.GetActiveScene().name);

        var cfgMgr = LevelConfigurationManager.Instance;
        var config = cfgMgr != null ? cfgMgr.GetConfigurationForLevel(levelId) : null;

        if (config == null)
        {
            _primaryGoalText.text = "Goals not configured.";
            foreach (var t in _secondaryGoalTexts) if (t) t.text = string.Empty;
            return;
        }

        var primary = config.GetPrimaryObjective();
        if (_primaryGoalText)
        {
            _primaryGoalText.text = primary != null ? primary.description : "-";
        }

        isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, primary) ?? false;

        if (isObjectiveComplete)
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = true;
            SetStar(0, true);
        }
        else
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = false;
            SetStar(0, false);
        }

        var secondaries = config.GetSecondaryObjectives();
        for (int i = 0; i < _secondaryGoalTexts.Length; i++)
        {
            if (!_secondaryGoalTexts[i]) continue;

            if (i < secondaries.Length && secondaries[i] != null)
            {
                _secondaryGoalTexts[i].text = secondaries[i].description;
                isObjectiveComplete = SaveManager.Instance?.IsObjectiveCompleted(levelId, secondaries[i]) ?? false;
                if (isObjectiveComplete)
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = true;
                    SetStar(i + 1, true);
                }
                else
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = false;
                    SetStar(i + 1, false);
                }
            }
            else
            {
                _secondaryGoalTexts[i].text = string.Empty;
            }
        }
    }

    private void SetStar(int idx, bool acquired)
    {
        var img = _starsContainer.GetChild(idx).GetComponent<Image>();
        if (!img) return;
        img.sprite = acquired ? _starAcquiredSprite : _starNotAcquiredSprite;
    }

    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}
