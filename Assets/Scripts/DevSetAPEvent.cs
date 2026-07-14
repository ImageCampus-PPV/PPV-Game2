using ImageCampus.ToolBox.Events;

public struct DevSetAPEvent : IEvent
{
    public int amount;
    public void Assign(params object[] p) 
    { 
        amount = (int)p[0]; 
    }

    public void Reset() 
    { 
        amount = default; 
    }
}