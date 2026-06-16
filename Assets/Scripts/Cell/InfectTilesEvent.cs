using ImageCampus.ToolBox.Events;
using System.Numerics;
using UnityEngine;

internal struct InfectTilesEvent : IEvent
{
    public Vector2Int position;

    public void Assign(params object[] parameters)
    {
        position = (Vector2Int)parameters[0];
    }

    public void Reset()
    {
        position = default(Vector2Int);
    }
}