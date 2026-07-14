using ImageCampus.ToolBox.Events;

public struct DevChangeCellStateEvent : IEvent
{
    public int coordX;
    public int coordY;
    public string stateName;
    public void Assign(params object[] p) 
    { 
        coordX = (int)p[0]; 
        coordY = (int)p[1]; 
        stateName = (string)p[2]; 
    }

    public void Reset() 
    { 
        coordX = default; 
        coordY = default; 
        stateName = default; 
    }
}
