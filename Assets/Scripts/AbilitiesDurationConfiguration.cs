
using ImageCampus.ToolBox.Services;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilitiesDurationConfiguration", menuName = "ScriptableObjects/AbilitiesDurationConfiguration")]
public class AbilitiesDurationConfiguration : ScriptableObject, IService
{
    public uint stunDuration = 0;
    public uint pushDistance = 0;

    public bool IsPersistance => false;
}