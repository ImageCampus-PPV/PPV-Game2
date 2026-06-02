using ImageCampus.ToolBox.Services;

public class Player : Unit
{
    private uint _life;

    public void SetLife(uint life)
    {
        _life = life;
    }

    public void AddLife(uint life)
    {
        _life += life;
    }

    public void RemoveLife(uint life)
    {
        _life -= life;
    }

    public void HandleMovement(Cell clickedCell)
    {
        RequestPath(clickedCell);
    }

    protected override void OnMovementStarted()
    {
    }
}