using ImageCampus.ToolBox.Events;

public struct DevSpawnEnemyEvent : IEvent
{
    public string enemyTypeName;
    public int coordX;
    public int coordY;
    public void Assign(params object[] parameters) 
    { 
        enemyTypeName = (string)parameters[0];
        coordX = (int)parameters[1];
        coordY = (int)parameters[2]; 
    }

    public void Reset() 
    { 
        enemyTypeName = default; 
        coordX = default; 
        coordY = default; 
    }
}
