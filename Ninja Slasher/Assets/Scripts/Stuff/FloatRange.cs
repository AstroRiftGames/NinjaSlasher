using UnityEngine;

[System.Serializable]
public class FloatRange
{
    public float min;
    public float max;

    public float GetRandom()
    {
        return Random.Range(min, max);
    }
}