using ImageCampus.ToolBox.Events;

internal struct PlayerChangeLifeEvent : IEvent
{
    public uint currentLife;

    public void Assign(params object[] parameters)
    {
        currentLife = (uint)parameters[0];
    }

    public void Reset()
    {
        currentLife = default(uint);
    }
}