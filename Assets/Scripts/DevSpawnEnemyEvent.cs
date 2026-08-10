using ImageCampus.ToolBox.Events;

public struct DevSpawnEnemyEvent : IEvent
{
    public string enemyTypeName;
    public int coordX;
    public int coordY;
    public void Assign(params object[] p) 
    { 
        enemyTypeName = (string)p[0];
        coordX = (int)p[1];
        coordY = (int)p[2]; 
    }

    public void Reset() 
    { 
        enemyTypeName = default; 
        coordX = default; 
        coordY = default; 
    }
}
