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

    [Header("Offsets")]
    [SerializeField] private float _reachableYOffset = 0.03f;
    [SerializeField] private float _pathYOffset = 0.06f;
    [SerializeField] private float _targetYOffset = 0.09f;

    private readonly List<GameObject> _spawnedVisuals = new();

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
        DrawReachableCells();

        if (_player.SelectedTargetCell != null)
        {
            DrawPlannedPath();
            DrawSelectedTarget();
        }
    }

    private void DrawReachableCells()
    {
        for (int x = 0; x < MapGrid.Width; x++)
        {
            for (int z = 0; z < MapGrid.Height; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                int cost = _player.GetPathCostPreview(cell);

                if (cost <= 0)
                    continue;

                if (cost > APWallet.CurrentAP)
                    continue;

                SpawnVisual(_reachableCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _reachableYOffset);
            }
        }
    }

    private void DrawPlannedPath()
    {
        if (_player.PlannedPath == null)
            return;

        foreach (Cell cell in _player.PlannedPath)
            SpawnVisual(_pathCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _pathYOffset);
    }

    private void DrawSelectedTarget()
    {
        SpawnVisual(_targetCellPrefab, _player.SelectedTargetCell.GetWorldTopPosition() + Vector3.up * _targetYOffset);
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
    }

    private void OnDisable()
    {
        ClearVisuals();
    }
}