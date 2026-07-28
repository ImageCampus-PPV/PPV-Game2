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

public struct LevelCompleteEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
        return;
    }

    public void Reset()
    {
        return;
    }
}

public struct LevelFailedEvent : IEvent
{
    public void Assign(params object[] parameters)
    {
        return;
    }

    public void Reset()
    {
        return;
    }
}

public struct DevAddTerminalEvent : IEvent
{
    public int coordX;
    public int coordY;
    public string terminalTypeName;

    public void Assign(params object[] parameters)
    {
        coordX = (int)parameters[0];
        coordY = (int)parameters[1];
        terminalTypeName = (string)parameters[2];
    }

    public void Reset()
    {
        coordX = default;
        coordY = default;
        terminalTypeName = default;
    }
}
