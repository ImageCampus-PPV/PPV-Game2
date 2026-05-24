using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MyGrid _grid;
    [SerializeField] private PathfindingController _pathfindingController;
    [SerializeField] private Camera _camera;

    [Header("Movement Settings")]
    [SerializeField] private float _timeToMoveCells = 0.2f;
    [SerializeField] private float _timeToStayInCell = 0.05f;

    private List<Cell> _currentPath;
    private int _pathIndex;
    private bool _isMoving;

    private Cell _currentCell;

    private void Start()
    {
        _currentCell = _grid.GetCell(0, 0);
        transform.position = GetStandPosition(_currentCell.GetWorldTopPosition());
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        if (_isMoving) return;

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

    private void RequestPath(Cell targetCell)
    {
        if (!targetCell.IsWalkable)
            return;

        List<Cell> path = _pathfindingController.FindPath(
            _currentCell.Coordinates,
            targetCell.Coordinates
        );

        if (path != null && path.Count > 0)
        {
            _currentPath = path;
            _pathIndex = 0;
            StartCoroutine(FollowPath());
        }
    }

    private IEnumerator FollowPath()
    {
        _isMoving = true;

        while (_pathIndex < _currentPath.Count)
        {
            Cell targetCell = _currentPath[_pathIndex];

            Vector3 startPos = transform.position;

            //Horizontal movement
            Vector3 flatTarget = new Vector3(
                targetCell.transform.position.x,
                startPos.y,
                targetCell.transform.position.z
            );

            Vector3 finalTarget;
            if (targetCell.Height != _currentCell.Height)
            {
                finalTarget = GetStandPosition(targetCell.GetWorldTopPosition());
            }
            else
            {
                finalTarget = targetCell.transform.position;
                finalTarget.y = transform.position.y;
            }


            float elapsed = 0f;
            while (elapsed < _timeToMoveCells)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _timeToMoveCells;

                transform.position = Vector3.Lerp(startPos, flatTarget, t);
                yield return null;
            }

            //Height adjustment
            elapsed = 0f;
            float heightTime = _timeToMoveCells * 0.5f;

            while (elapsed < heightTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / heightTime;

                transform.position = Vector3.Lerp(flatTarget, finalTarget, t);
                yield return null;
            }

            transform.position = finalTarget;

            _currentCell = targetCell;
            _pathIndex++;

            if (_timeToStayInCell > 0)
                yield return new WaitForSeconds(_timeToStayInCell);
        }

        _isMoving = false;
    }

    private Vector3 GetStandPosition(Vector3 basePostion)
    {
        return basePostion + new Vector3(0, transform.localScale.y * 0.5f, 0);
    }
}