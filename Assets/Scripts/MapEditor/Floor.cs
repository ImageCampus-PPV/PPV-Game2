using System;
using UnityEngine;

[Serializable]
public class Floor : ScriptableObject
{
    public Vector2Int size;
    public CellData[] _cellsData;
}
