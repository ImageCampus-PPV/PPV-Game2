using ImageCampus.ToolBox.Services;

public class Player : Unit
{
    private uint _life;
    public void HandleMovement(Cell clickedCell)
    {
        RequestPath(clickedCell);
    }

    protected override void OnMovementStarted()
    {
    }
}