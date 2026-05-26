using UnityEngine;

public class PlayerController : UnitController
{
    [Header("Player")]
    [SerializeField] private Camera _camera;
    [SerializeField] private TurnManager _turnManager;

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (_isMoving)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                Cell clickedCell = hit.collider.GetComponentInParent<Cell>();

                if (clickedCell != null)
                    RequestPath(clickedCell);
            }
        }
    }

    protected override void OnMovementStarted()
    {
        _turnManager.PlayerStep();
    }
}