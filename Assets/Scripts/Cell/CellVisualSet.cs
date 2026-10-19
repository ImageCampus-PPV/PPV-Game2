using System;
using UnityEngine;

[CreateAssetMenu(fileName = "CellVisualSet", menuName = "Map/Cell Visual Set")]
public class CellVisualSet : ScriptableObject
{
    [Header("Default")]
    [SerializeField] private GameObject _defaultPrefab;

    [Header("State Variants")]
    [SerializeField] private GameObject _emptyPrefab;
    [SerializeField] private GameObject _unstablePrefab;
    [SerializeField] private GameObject _brokenPrefab;
    [SerializeField] private GameObject _healingPrefab;
    [SerializeField] private GameObject _contagiousPrefab;
    [SerializeField] private GameObject _infectedPrefab;

    public GameObject GetPrefab(Type stateType)
    {
        if (stateType == null)
            return _defaultPrefab;

        return GetPrefab(stateType.Name);
    }

    public GameObject GetPrefab(string stateName)
    {
        GameObject prefab = stateName switch
        {
            nameof(Empty) => _emptyPrefab,
            nameof(Unstable) => _unstablePrefab,
            nameof(Broken) => _brokenPrefab,
            nameof(Healing) => _healingPrefab,
            nameof(Contagious) => _contagiousPrefab,
            nameof(Infected) => _infectedPrefab,
            nameof(DefaultCell) => _defaultPrefab,
            _ => null
        };

        return prefab != null ? prefab : _defaultPrefab;
    }
}