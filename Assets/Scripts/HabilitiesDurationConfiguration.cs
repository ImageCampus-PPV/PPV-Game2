
using ImageCampus.ToolBox.Services;
using UnityEngine;

[CreateAssetMenu(fileName = "HabilitiesDurationConfiguration", menuName = "ScriptableObjects/HabilitiesDurationConfiguration")]
public class HabilitiesDurationConfiguration : ScriptableObject, IService
{
    public uint stunDuration = 0;
    public uint pushDistance = 0;

    public bool IsPersistance => false;
}