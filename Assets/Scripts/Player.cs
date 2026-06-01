using ImageCampus.ToolBox.Services;

public class Player : Unit
{
    public void HandleMovement(Cell clickedCell)
    {
        RequestPath(clickedCell);
    }

    protected override void OnMovementStarted()
    {
    }
}