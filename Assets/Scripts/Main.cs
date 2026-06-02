using ImageCampus.ToolBox.Services;
using UnityEngine;

public class Main : MonoBehaviour
{
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    PathFinding Pathfinding => ServiceProvider.Instance.GetService<PathFinding>();

    void Awake()
    {
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid());
        ServiceProvider.Instance.AddService<PathFinding>(new PathFinding());

        MapGrid.Init();
    }
}
