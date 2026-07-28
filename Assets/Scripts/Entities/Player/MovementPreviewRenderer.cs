using Assets.Scripts.Combat;
using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Services;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MovementPreviewRenderer : MonoBehaviour
{
    APWallet APWallet => ServiceProvider.Instance.GetService<APWallet>();
    MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();
    EntityRegistry EntityRegistry => ServiceProvider.Instance.GetService<EntityRegistry>();

    [Header("References")]
    private Player _player => EntityRegistry.FilterEntities<Player>().First();

    [Header("Visual Prefabs")]
    [SerializeField] private GameObject _reachableCellPrefab;
    [SerializeField] private GameObject _pathCellPrefab;
    [SerializeField] private GameObject _targetCellPrefab;
    [SerializeField] private GameObject _waypointCellPrefab;

    [Header("Offset")]
    [SerializeField] private float _yOffset = 0.1f;

    private readonly List<GameObject> _spawnedVisuals = new();
    private readonly HashSet<Cell> _drawnCells = new();

    private void Update()
    {
        if (_player == null)
            return;

        RefreshVisuals();
    }

    private void RefreshVisuals()
    {
        ClearVisuals();

        if (_player.PlannedActions.Count > 0)
        {
            DrawPlannedPath();
        }

        if (!_player.IsTurnPlaying)
            DrawReachableCells();
    }

    private void DrawReachableCells()
    {
        Cell origin = _player.GetLastPlannedCell();

        int remainingAP = APWallet.CurrentAP - _player.PlannedAPCost;
        int remainingTicks = _player.MaxTicksPerTurn - _player.PlannedTicks;

        for (int x = 0; x < MapGrid.Width; x++)
        {
            for (int z = 0; z < MapGrid.Height; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                if (cell == null)
                    continue;

                if (_drawnCells.Contains(cell))
                    continue;

                int apCost = _player.GetPathCostPreview(origin, cell);

                if (apCost <= 0)
                    continue;

                if (apCost > remainingAP)
                    continue;

                int ticksCost = _player.GetPathTicksPreview(origin, cell);

                if (ticksCost > remainingTicks)
                    continue;

                SpawnVisual(_reachableCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _yOffset);
            }
        }
    }

    private void DrawPlannedPath()
    {
        foreach (Cell cell in _player.PlannedPath)
        {
            if (cell == null)
                continue;

            if (_drawnCells.Contains(cell))
                continue;

            SpawnVisual(_pathCellPrefab, cell.GetWorldTopPosition() + Vector3.up * _yOffset);
            _drawnCells.Add(cell);
        }
    }


    private void SpawnVisual(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
            return;

        GameObject obj = Instantiate(prefab, position, Quaternion.identity, transform);
        _spawnedVisuals.Add(obj);
    }

    private void ClearVisuals()
    {
        foreach (GameObject visual in _spawnedVisuals)
        {
            if (visual != null)
                Destroy(visual);
        }

        _spawnedVisuals.Clear();
        _drawnCells.Clear();
    }

    private void OnDisable()
    {
        ClearVisuals();
    }
}