using ImageCampus.ToolBox.Services;
using UnityEngine;

public class Main : MonoBehaviour
{
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    void Awake()
    {
        ServiceProvider.Instance.AddService<MapGrid>(new MapGrid());
        MapGrid.Init();
    }
}
