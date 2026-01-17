using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ObjectiveEvaluationResult
{
    public int starsEarned = 0;
    public bool primaryCompleted = false;
    public List<ObjectiveData> completedObjectives = new List<ObjectiveData>();
}
