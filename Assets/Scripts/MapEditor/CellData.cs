using System;
using UnityEngine;

[Serializable]
public struct CellData
{
    public string _initialState;
    public Vector2Int _coordinates;
    public string _spawnUnit;

    public CellData(Vector2Int coordinates, string initialState)
    {
        _coordinates = coordinates;
        _initialState = initialState;
        _spawnUnit = "None";
    }
}
