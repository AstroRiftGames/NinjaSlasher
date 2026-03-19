using System.Collections.Generic;
using UnityEngine;

public class TutorialDisplayController
{
    private readonly Dictionary<string, GameObject[]> _bindings;
    private readonly GameObject _dashIndicator;
    private readonly GameObject _parryIndicator;

    public TutorialDisplayController(
        Dictionary<string, GameObject[]> bindings,
        GameObject dashIndicator,
        GameObject parryIndicator)
    {
        _bindings = bindings;
        _dashIndicator = dashIndicator;
        _parryIndicator = parryIndicator;
    }

    public void ShowStep(TutorialStepDefinition step)
    {
        HideAllTexts();
        HideIndicators();

        if (step == null)
            return;

        GameObject[] boundTexts = GetTexts(step.stepId);
        if (boundTexts != null)
        {
            if (step.showAllBoundTexts)
            {
                for (int i = 0; i < boundTexts.Length; i++)
                {
                    if (boundTexts[i] != null)
                        boundTexts[i].SetActive(true);
                }
            }
            else if (boundTexts.Length > 0 && boundTexts[0] != null)
            {
                boundTexts[0].SetActive(true);
            }
        }

        switch (step.handIndicator)
        {
            case TutorialHandIndicator.Dash:
                if (_dashIndicator != null)
                    _dashIndicator.SetActive(true);
                break;
            case TutorialHandIndicator.Parry:
                if (_parryIndicator != null)
                    _parryIndicator.SetActive(true);
                break;
        }
    }

    public void HideAll()
    {
        HideAllTexts();
        HideIndicators();
    }

    public void HideDashIndicator()
    {
        HandSwipeAnimation[] allHandAnimations = Object.FindObjectsOfType<HandSwipeAnimation>(true);
        for (int i = 0; i < allHandAnimations.Length; i++)
        {
            allHandAnimations[i].StopAnimation();
            allHandAnimations[i].gameObject.SetActive(false);
        }

        if (_dashIndicator != null)
            _dashIndicator.SetActive(false);
    }

    private void HideAllTexts()
    {
        foreach (var entry in _bindings)
        {
            GameObject[] objects = entry.Value;
            if (objects == null)
                continue;

            for (int i = 0; i < objects.Length; i++)
            {
                if (objects[i] != null)
                    objects[i].SetActive(false);
            }
        }
    }

    private void HideIndicators()
    {
        if (_dashIndicator != null)
            _dashIndicator.SetActive(false);

        if (_parryIndicator != null)
            _parryIndicator.SetActive(false);
    }

    private GameObject[] GetTexts(string stepId)
    {
        if (string.IsNullOrEmpty(stepId))
            return null;

        return _bindings.TryGetValue(stepId, out GameObject[] texts) ? texts : null;
    }
}
