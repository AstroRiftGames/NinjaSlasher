using System;

[Serializable]
public class SingleObjectiveProgress
{
    public ObjectiveData objective;
    public float progress;
    public bool isCompleted;
    public bool canBeEvaluated;
}
