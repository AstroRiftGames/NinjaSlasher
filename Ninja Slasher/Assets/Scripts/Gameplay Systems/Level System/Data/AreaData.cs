using UnityEngine;

[CreateAssetMenu(fileName = "Area_Data", menuName = "Game/Area Data")]
public class AreaData : ScriptableObject
{
    [Header("Identificación")]
    [Tooltip("ID del area. Debe coincidir con los IDs de LevelProgressionManager")]
    public int areaId;

    [Tooltip("Nombre del área para mostrar en UI")]
    public string displayName;
}
