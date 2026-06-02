using Assets.Scripts.Combat;
using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using UnityEngine;

public class MovementPreviewRenderer : MonoBehaviour
{
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    [Header("References")]
    [SerializeField] private Player _player;

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject _reachableCellPrefab;
    [SerializeField] private GameObject _pathCellPrefab;
    [SerializeField] private GameObject _targetCellPrefab;

    [Header("Offset")]
    [SerializeField] private float _YOffset;

    private List<GameObject> _spawnedVisuals = new();
    private List<Cell> _drawnCells = new();

    private Cell _lastTarget;
    private int _lastAP;
    private Cell _lastPlayerCell;

    private void Update()
    {
        if (_player == null)
            return;

        bool shouldRefresh =
            _lastTarget != _player.SelectedTargetCell ||
            _lastAP != APWallet.CurrentAP ||
            _lastPlayerCell != _player.CurrentCell;

        if (!shouldRefresh)
            return;

        RefreshVisuals();

        _lastTarget = _player.SelectedTargetCell;
        _lastAP = APWallet.CurrentAP;
        _lastPlayerCell = _player.CurrentCell;
    }

    private void RefreshVisuals()
    {
        ClearVisuals();

        if (_player.SelectedTargetCell != null)
        {
            DrawSelectedTarget();
            DrawPlannedPath();
        }

        DrawReachableCells();
    }

    private void DrawReachableCells()
    {
        for (int x = 0; x < MapGrid.Width; x++)
        {
            for (int z = 0; z < MapGrid.Height; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);
                if (_drawnCells.Contains(cell))
                    continue;

                int cost = _player.GetPathCostPreview(cell);

                if (cost <= 0)
                    continue;

                if (cost > APWallet.CurrentAP)
                    continue;

                SpawnVisual(_reachableCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _YOffset);
            }
        }
    }

    private void DrawPlannedPath()
    {
        if (_player.PlannedPath == null)
            return;

        foreach (Cell cell in _player.PlannedPath)
        {
            if (_drawnCells.Contains(cell))
                continue;

            SpawnVisual(_pathCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _YOffset);
            _drawnCells.Add(cell);
        }
    }

    private void DrawSelectedTarget()
    {
        _drawnCells.Add(_player.SelectedTargetCell);
        SpawnVisual(_targetCellPrefab, _player.SelectedTargetCell.GetWorldTopPosition() + Vector3.up * _YOffset);
    }

    private void SpawnVisual(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        GameObject obj = Instantiate(
            prefab,
            position,
            Quaternion.identity,
            transform
        );

        _spawnedVisuals.Add(obj);
    }

    private void ClearVisuals()
    {
        for (int i = 0; i < _spawnedVisuals.Count; i++)
            if (_spawnedVisuals[i] != null)
                Destroy(_spawnedVisuals[i]);

        _spawnedVisuals.Clear();
        _drawnCells.Clear();
    }

    private void OnDisable()
    {
        ClearVisuals();
    }
}