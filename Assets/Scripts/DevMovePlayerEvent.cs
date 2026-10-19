using ImageCampus.ToolBox.Events;

public struct DevMovePlayerEvent : IEvent
{
    public int coordX;
    public int coordY;
    public void Assign(params object[] p) 
    { 
        coordX = (int)p[0]; 
        coordY = (int)p[1]; 
    }

    public void Reset() 
    { 
        coordX = default; 
        coordY = default; 
    }
}
