using System;
using UnityEngine;

[Serializable]
public struct CellData
{
    public string _initialState;
    public Vector2Int _coordinates;
    public bool _spawnPlayer;
    public string _spawnEnemy;

    public CellData(Vector2Int coordinates, string initialState)
    {
        _coordinates = coordinates;
        _initialState = initialState;
        _spawnPlayer = false;
        _spawnEnemy = null;
    }
}

public class CellsMaps : ScriptableObject
{
    public Vector2Int size;
    public CellData[] _cellsData;
}
