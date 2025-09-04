using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class ResultsUIManager : MonoBehaviourSingleton<ResultsUIManager>
{
    [SerializeField] private Transform _starsContainer;
    [SerializeField] private TextMeshProUGUI _primaryGoalText;
    [SerializeField] private TextMeshProUGUI[] _secondaryGoalTexts;
    [SerializeField] private Color _starNotAcquired = Color.black;
    [SerializeField] private Color _starAcquired = Color.yellow;
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
            _starsContainer.GetChild(0).GetComponent<Image>().color = _starAcquired;
        }
        else
        {
            _primaryGoalText.GetComponentInChildren<Image>().enabled = false;
            _starsContainer.GetChild(0).GetComponent<Image>().color = _starNotAcquired;
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
                    _starsContainer.GetChild(i + 1).GetComponent<Image>().color = _starAcquired;
                }
                else
                {
                    _secondaryGoalTexts[i].GetComponentInChildren<Image>().enabled = false;
                    _starsContainer.GetChild(i + 1).GetComponent<Image>().color = _starNotAcquired;
                }
            }
            else
            {
                _secondaryGoalTexts[i].text = string.Empty;
            }
        }
    }


    private int GetLevelIdFromSceneName(string name)
    {
        return (name != null &&
                name.Contains("Level") && int.TryParse(name.Substring(5), out var id)) ? id : 1;
    }
}
