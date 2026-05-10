using UnityEngine;

public enum SurfaceMaterial
{
    General,
    Rock,
    Wood,
    Leaves,
}

public class SurfaceProperties : MonoBehaviour
{
    public SurfaceMaterial MaterialType;
}
