using ImageCampus.ToolBox.Events;

public struct DevResizeGridEvent : IEvent
{
    public int width;
    public int height;
    public void Assign(params object[] p)
    {
        width = (int)p[0];
        height = (int)p[1];
    }
    public void Reset()
    {
        width = default;
        height = default;
    }
}