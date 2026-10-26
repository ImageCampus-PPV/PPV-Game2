
using ImageCampus.ToolBox.Events;
using ImageCampus.ToolBox.Services;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class Unit : BaseEntity
{
    protected PathFinding PathFinding => ServiceProvider.Instance.GetService<PathFinding>();
    protected EventBus EventBus => ServiceProvider.Instance.GetService<EventBus>();
    protected MapGrid MapGrid => ServiceProvider.Instance.GetService<MapGrid>();

    [Header("Spawn")]
    [SerializeField] protected Cell _spawnCell;

    public Cell SpawnCell => _spawnCell;
    public void SetSpawnCell(Cell spawnCell) => _spawnCell = spawnCell;

    [Header("Movement")]
    [SerializeField] protected int _maxTicksPerTurn = 7;
    [SerializeField] protected float _timeToMoveCells = 0.2f;
    [SerializeField] protected float _timeToStayInCell = 0.05f;
    protected List<TurnAction> _plannedActions = new();
    public List<TurnAction> PlannedActions => _plannedActions;

    protected List<Cell> _currentPath;
    protected int _pathIndex;
    protected bool _isTurnPlaying;

    protected int _usedTicksThisTurn;
    public int UsedTicksThisTurn => _usedTicksThisTurn;
    public int RemainingTicksThisTurn => Mathf.Max(0, _maxTicksPerTurn - _usedTicksThisTurn - GetPlannedTickCost());

    public bool IsTurnPlaying { get => _isTurnPlaying; set => _isTurnPlaying = value; }

    protected int _currentAction;
    public int CurrentAction { get => _currentAction; set => _currentAction = value; }

    protected Cell _currentCell;

    protected int _attackRange = 4;
    public int AttackRange => _attackRange;

    private bool _isStun = false;
    public bool IsStun => _isStun;

    private Transform _cellPivot;

    protected Transform CellPivot
    {
        get
        {
            if (_cellPivot == null)
            {
                UnitCellPivot anchor = GetComponentInChildren<UnitCellPivot>(true);

                if (anchor != null)
                    _cellPivot = anchor.transform;
            }

            return _cellPivot;
        }
    }

    private CustomAnimator _frontAnimator;
    private CustomAnimator _backAnimator;
    private CustomAnimator _activeAnimator;

    private Vector3 _frontInitialScale;
    private Vector3 _backInitialScale;

    private bool _animatorsInitialized;
    private bool _isFacingLeft;

    public CustomAnimator CustomAnimator
    {
        get
        {
            InitializeAnimators();
            return _activeAnimator;
        }
    }

    public Cell CurrentCell => _currentCell;
    public int MaxTicksPerTurn => _maxTicksPerTurn;

    public virtual void Init()
    {
        InitializeAnimators();
        Spawn();
    }

    protected virtual void Spawn()
    {
        _currentCell = _spawnCell;

        if (_currentCell != null)
        {
            //transform.position = GetStandPosition(_currentCell.GetWorldTopPosition());
            AdjustEntityPositionToCell(gameObject, _spawnCell.gameObject);
            _currentCell.stander = this;
        }
    }

    public void Stun()
    {
        CustomAnimator.Play(AnimationStates.Stun);
        _isStun = true;
    }

    public void Unstun()
    {
        CustomAnimator.Play(AnimationStates.Idle);
        _isStun = false;
    }

    public bool IsInAttackRange(Cell originCell, Cell targetCell, int attackRange)
    {
        Vector2Int origin = originCell.Coordinates;
        Vector2Int target = targetCell.Coordinates;

        Vector2Int[] directions = new Vector2Int[]
        {
        new Vector2Int( 1,  0),
        new Vector2Int(-1,  0),
        new Vector2Int( 0,  1),
        new Vector2Int( 0, -1),
        };

        foreach (Vector2Int dir in directions)
        {
            for (int i = 1; i <= attackRange; i++)
            {
                Vector2Int current = origin + dir * i;

                if (current.x < 0 || current.y < 0 || current.x >= MapGrid.Width || current.y >= MapGrid.Height)
                    break;

                Cell currentCell = MapGrid.GetCell(current);

                if (currentCell.ProvidesCover)
                    break;

                if (current == target)
                    return true;
            }
        }

        return false;
    }

    protected bool IsCellAvailable(Cell targetCell)
    {
        if (targetCell == null)
            return false;

        if (!targetCell.IsWalkable)
            return false;

        if (targetCell.isOccupied)
            return false;

        return true;
    }

    protected int GetPathCost(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell))
        {
            Debug.LogWarning("Target cell unavailable");
            return -1;
        }

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path == null)
            return -1;

        return path.Count - 1;
    }

    protected int GetPathCost(Cell originCell, Cell targetCell)
    {
        if (targetCell == originCell)
            return -1;

        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning($"Target cell unavailable: {targetCell}");
            return -1;
        }

        if (!IsCellAvailable(originCell) && originCell.stander is not Player)
        {
            Debug.LogWarning($"Origin cell unavailable: {originCell}");
            return -1;
        }

        List<Cell> path = PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);

        if (path == null)
            return -1;

        return path.Count - 1;
    }

    protected List<Cell> GetPathCells(Cell targetCell)
    {
        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);
    }

    protected List<Cell> GetPathCells(Cell originCell, Cell targetCell)
    {
        if (!IsCellAvailable(targetCell) && targetCell.stander is not Player)
        {
            Debug.LogWarning("Target cell unavailable");
            return null;
        }

        return PathFinding.FindPath(originCell.Coordinates, targetCell.Coordinates);
    }

    public IEnumerator MoveTo(Cell targetCell)
    {
        if (targetCell.isOccupied)
        {
            Debug.Log($"Cell {targetCell} is occupied " + $"(stander: {targetCell.stander}). Clearing plan.");

            ClearPlan();
            yield break;
        }

        if (!targetCell.IsWalkable)
        {
            Debug.Log($"Cell {targetCell} is not walkable. Clearing plan.");

            ClearPlan();
            yield break;
        }

        _isTurnPlaying = true;

        FaceCell(targetCell);
        Vector3 startPos = transform.position;
        Vector3 finalTarget = GetStandPosition(targetCell.GetWorldTopPosition());

        Cell previousCell = _currentCell;
        _currentCell = targetCell;

        previousCell.stander = null;
        _currentCell.stander = this;

        CustomAnimator.Play(AnimationStates.Walk, 0, 1.5f);

        float movementDuration = CustomAnimator.GetAnimationDuration(AnimationStates.Walk);

        if (movementDuration <= 0f)
            movementDuration = _timeToMoveCells;

        float elapsed = 0f;
        while (elapsed < movementDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / movementDuration);
            transform.position = Vector3.Lerp(startPos, finalTarget, t);

            yield return null;
        }

        transform.position = finalTarget;

        CustomAnimator.Play(AnimationStates.Idle);

        _isTurnPlaying = false;
    }



    public IEnumerator Wait()
    {
        float elapsed = 0;
        _isTurnPlaying = true;

        while (elapsed < _timeToMoveCells * 1.5f)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        _isTurnPlaying = false;
    }

    public void MoveInstant(Cell targetCell)
    {
        if (_currentCell != null)
            _currentCell.stander = null;

        _currentCell = targetCell;
        _currentCell.stander = this;

        transform.position = GetStandPosition(targetCell.GetWorldTopPosition());
    }


    protected int GetPlannedTickCost()
    {
        int ticks = 0;

        foreach (TurnAction action in _plannedActions)
            ticks += action.TotalTicks;

        return ticks;
    }

    protected virtual bool CanAddAction(TurnAction action)
    {
        int futureTicks = GetPlannedTickCost() + action.TotalTicks;

        return futureTicks <= _maxTicksPerTurn;
    }

    protected virtual void OnMovementStarted() { }

    protected virtual void OnMovementFinished() { }

    public virtual IEnumerator<Vector2Int> AttackPattern()
    {
        return null;
    }

    protected Vector3 GetStandPosition(Vector3 basePosition)
    {
        if (CellPivot == null)
        {
            Debug.LogWarning($"{gameObject.name} has no UnitCellPivot.");
            return basePosition;
        }

        Vector3 anchorOffset = CellPivot.position - transform.position;
        return basePosition - anchorOffset;
    }


    protected void AdjustEntityPositionToCell(GameObject entity, GameObject cellGO)
    {
        Renderer[] cellRenderers = cellGO.GetComponentsInChildren<Renderer>(true);

        if (cellRenderers.Length == 0)
        {
            Debug.LogWarning($"{cellGO.name} has no Renderer.");
            return;
        }

        Bounds cellBounds = cellRenderers[0].bounds;
        for (int i = 1; i < cellRenderers.Length; i++)
            cellBounds.Encapsulate(cellRenderers[i].bounds);

        float cellWidth = cellBounds.size.x;
        float cellDepth = cellBounds.size.z;

        float cellX = cellBounds.center.x;
        float cellZ = cellBounds.center.z;

        float cellTopY = cellBounds.max.y;


        Renderer[] entityRenderers = entity.GetComponentsInChildren<Renderer>(true);

        if (entityRenderers.Length == 0)
        {
            Debug.LogWarning($"{entity.name} has no Renderer.");
            return;
        }

        Bounds entityBounds = entityRenderers[0].bounds;
        for (int i = 1; i < entityRenderers.Length; i++)
            entityBounds.Encapsulate(entityRenderers[i].bounds);

        float entityWidth = entityBounds.size.x;
        float entityDepth = entityBounds.size.z;


        if (entityWidth > 0f && entityDepth > 0f)
        {
            float scaleX = cellWidth / entityWidth;
            float scaleZ = cellDepth / entityDepth;

            float scale = Mathf.Min(scaleX, scaleZ);

            entity.transform.localScale *= scale * 1.75f;
        }

        if (CellPivot == null)
        {
            Debug.LogWarning($"{entity.name} has no UnitCellAnchor.");
            return;
        }

        Vector3 anchorPosition = CellPivot.position;
        Vector3 targetPosition = new Vector3(cellX, cellTopY, cellZ);

        entity.transform.position += targetPosition - anchorPosition;
    }

    public virtual void FaceCell(Cell targetCell)
    {
        if (targetCell == null || CurrentCell == null)
            return;

        Vector2Int direction = targetCell.Coordinates - CurrentCell.Coordinates;

        if (direction.y > 0)
            SetArmatureView(ArmatureView.Back);
        else if (direction.y < 0)
            SetArmatureView(ArmatureView.Front);

        if (direction.x > 0)
            FaceLeft();
        else if (direction.x < 0)
            FaceRight();
        else if (direction.y > 0)
            FaceRight();
        else if (direction.y < 0)
            FaceLeft();
    }

    protected void FaceLeft()
    {
        _isFacingLeft = true;
        InitializeAnimators();
        ApplyHorizontalFacing();
    }

    protected void FaceRight()
    {
        _isFacingLeft = false;
        InitializeAnimators();
        ApplyHorizontalFacing();
    }

    public IEnumerator PlayAnimationAndWait(AnimationStates state, int playTimes = 1, float timeScale = 1f)
    {
        CustomAnimator.Play(state, playTimes, timeScale, true);

        float duration = CustomAnimator.GetAnimationDuration(state);

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        else
            yield return null;
    }

    private void InitializeAnimators()
    {
        if (_animatorsInitialized)
            return;

        _animatorsInitialized = true;

        CustomAnimator[] animators = GetComponentsInChildren<CustomAnimator>(true);

        foreach (CustomAnimator animator in animators)
        {
            switch (animator.View)
            {
                case ArmatureView.Front:
                    _frontAnimator = animator;
                    break;

                case ArmatureView.Back:
                    _backAnimator = animator;
                    break;
            }
        }

        if (_frontAnimator == null && _backAnimator == null)
        {
            Debug.LogError($"{name} has no front or back CustomAnimator.");
            return;
        }

        if (_frontAnimator != null)
            _frontInitialScale = _frontAnimator.transform.localScale;

        if (_backAnimator != null)
            _backInitialScale = _backAnimator.transform.localScale;

        _activeAnimator = _frontAnimator != null ? _frontAnimator : _backAnimator;

        UpdateArmatureActivation();
        ApplyHorizontalFacing();
    }

    private void SetArmatureView(ArmatureView view)
    {
        InitializeAnimators();

        CustomAnimator selectedAnimator = view == ArmatureView.Front ? _frontAnimator : _backAnimator;

        if (selectedAnimator == null)
            return;

        if (_activeAnimator == selectedAnimator)
            return;

        _activeAnimator = selectedAnimator;

        UpdateArmatureActivation();
        ApplyHorizontalFacing();
    }

    private void UpdateArmatureActivation()
    {
        if (_frontAnimator != null)
            _frontAnimator.gameObject.SetActive(_activeAnimator == _frontAnimator);

        if (_backAnimator != null)
            _backAnimator.gameObject.SetActive(_activeAnimator == _backAnimator);
    }

    private void ApplyHorizontalFacing()
    {
        if (_activeAnimator == null)
            return;

        Vector3 initialScale = _activeAnimator == _frontAnimator ? _frontInitialScale : _backInitialScale;

        bool shouldFlip = _isFacingLeft != _activeAnimator.FacesLeftByDefault;

        initialScale.x = Mathf.Abs(initialScale.x) * (shouldFlip ? -1f : 1f);

        _activeAnimator.transform.localScale = initialScale;
    }


    public virtual void ClearPlan()
    {
        Debug.Log($"{gameObject.name} plan cleared");
        _plannedActions.Clear();
    }

    public virtual void ResetActionsKick()
    {
        _currentAction = 0;
    }

    public virtual void ConsumeAP(TurnAction action)
    {
    }
}