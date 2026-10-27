using Assets.Scripts.Entities;
using ImageCampus.ToolBox.Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Enemy : Unit
{
    //Normal enemy values
    protected uint _damage = 25;
    protected int _movementRange = 1;
    protected int _fortitude = 2;
    protected int _pushDistance = 2;
    protected int _attackTickCost = 1;

    public uint Damage => _damage;
    public int MovementRange => _movementRange;
    public int Fortitude => _fortitude;
    public int PushDistance => _pushDistance;
    public int AttackTickCost => _attackTickCost;

    [Header("Health")]
    protected int _maxHp = 100;

    private int _currentHp;
    private bool _isDead;

    private EnemyHealthBar _healthBar;
    private Coroutine _damageCoroutine;

    public int CurrentHp => _currentHp;
    public int MaxHp => _maxHp;
    public bool IsDead => _isDead;


    public override void Init()
    {
        _currentHp = Mathf.Max(1, _maxHp);
        _healthBar = GetComponentInChildren<EnemyHealthBar>(true);

        if (_healthBar != null)
            _healthBar.SetHealth(_currentHp, _maxHp);
        else
            Debug.LogWarning($"{name} has no EnemyHealthBar.");

        base.Init();
    }

    public void TakeDamage(int damage)
    {
        if (_isDead || damage <= 0)
            return;

        _currentHp = Mathf.Max(0, _currentHp - damage);

        if (_healthBar != null)
            _healthBar.SetHealth(_currentHp, _maxHp);

        if (_currentHp <= 0)
        {
            Die();
            return;
        }

        if (_damageCoroutine != null)
            StopCoroutine(_damageCoroutine);

        _damageCoroutine = StartCoroutine(PlayDamageAnimation());
    }

    private IEnumerator PlayDamageAnimation()
    {
        CustomAnimator.Play(AnimationStates.TakeDamage, 1, 1f, true);

        float duration = CustomAnimator.GetAnimationDuration(
            AnimationStates.TakeDamage);

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        else
            yield return null;

        if (!_isDead)
        {
            CustomAnimator.Play(
                IsStun ? AnimationStates.Stun : AnimationStates.Idle);
        }

        _damageCoroutine = null;
    }

    private void Die()
    {
        if (_isDead)
            return;

        _isDead = true;

        if (_damageCoroutine != null)
        {
            StopCoroutine(_damageCoroutine);
            _damageCoroutine = null;
        }

        ClearPlan();

        if (_currentCell != null && _currentCell.stander == this)
            _currentCell.stander = null;

        TurnManager turnManager = ServiceProvider.Instance.GetService<TurnManager>();

        if (turnManager != null)
            turnManager.RemoveStun(this);

        EntityRegistry registry = ServiceProvider.Instance.GetService<EntityRegistry>();

        if (registry != null)
            registry.Remove(this);

        StartCoroutine(DieRoutine());
    }

    private IEnumerator DieRoutine()
    {
        if (_healthBar != null)
            _healthBar.gameObject.SetActive(false);

        CustomAnimator.Play(AnimationStates.TakeDamage, 1, 1f, true);

        float duration = CustomAnimator.GetAnimationDuration(AnimationStates.TakeDamage);

        if (duration > 0f)
            yield return new WaitForSeconds(duration);
        else
            yield return null;

        Destroy(gameObject);
    }

    public void PlanTurn(Cell playerCell, int emptyActions)
    {
        _plannedActions.Clear();
        _usedTicksThisTurn = 0;

        for (int i = 0; i < emptyActions; i++)
            _plannedActions.Add(new WaitAction());

        Cell newCurrentCell = _currentCell;
        if (!IsInAttackRange(_currentCell, playerCell, _attackRange))
            newCurrentCell = PlanMovementActions(playerCell);

        if (IsInAttackRange(newCurrentCell, playerCell, _attackRange))
            PlanCombatActions(playerCell);

    }

    private Cell PlanMovementActions(Cell playerCell)
    {
        Cell newCurrentCell = CurrentCell;
        Cell targetCell = FindBestCell(playerCell);

        if (targetCell == null)
        {
            Debug.Log($"{name} found no valid move");
            return newCurrentCell;
        }

        List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, targetCell.Coordinates);

        if (path == null || path.Count <= 1)
            return newCurrentCell;

        int actionLimit = path.Count < _maxTicksPerTurn ? path.Count : _maxTicksPerTurn;

        for (int i = 1; i < actionLimit; i++)
        {
            _plannedActions.Add(new MoveAction(path[i - 1], path[i], 0));
            newCurrentCell = path[i];
        }

        return newCurrentCell;
    }

    private bool IsInRange(Cell playerCell)
    {
        return GetGridDistance(_currentCell, playerCell) <= _movementRange;
    }


    private Cell FindBestCell(Cell playerCell)
    {
        Cell bestCoverCell = null;
        float bestCoverDist = float.MaxValue;

        Cell fallbackCell = null;
        float fallbackDist = float.MaxValue;

        int minX = Mathf.Max(0, playerCell.Coordinates.x - _movementRange);
        int maxX = Mathf.Min(MapGrid.Width - 1, playerCell.Coordinates.x + _movementRange);
        int minZ = Mathf.Max(0, playerCell.Coordinates.y - _movementRange);
        int maxZ = Mathf.Min(MapGrid.Height - 1, playerCell.Coordinates.y + _movementRange);

        for (int x = minX; x <= maxX; x++)
        {
            for (int z = minZ; z <= maxZ; z++)
            {
                Cell cell = MapGrid.GetCell(x, z);

                if (cell == null)
                    continue;

                if (!cell.IsWalkable)
                    continue;

                if (cell.isOccupied)
                    continue;

                int distToPlayer = GetGridDistance(cell, playerCell);

                if (distToPlayer > _movementRange)
                    continue;

                //Make sure enemy can reach the cell
                List<Cell> path = PathFinding.FindPath(_currentCell.Coordinates, cell.Coordinates);

                if (path == null || path.Count <= 1)
                    continue;

                if (HasCoverAgainstPlayer(cell, playerCell))
                {
                    float coverScore = GetGridDistance(cell, _currentCell);

                    if (coverScore < bestCoverDist)
                    {
                        bestCoverDist = coverScore;
                        bestCoverCell = cell;
                    }

                    continue;
                }


                if (!IsInRange(playerCell))
                {
                    //Try to stay close to attack range
                    float rangeScore = Mathf.Abs(distToPlayer - _movementRange);

                    if (rangeScore < fallbackDist)
                    {
                        fallbackDist = rangeScore;
                        fallbackCell = cell;
                    }
                }
            }
        }

        //Prefer cover
        if (bestCoverCell != null)
            return bestCoverCell;

        //Otherwise fallback movement
        return fallbackCell;
    }

    private bool HasCoverAgainstPlayer(Cell cell, Cell playerCell)
    {
        Vector3 end = playerCell.GetWorldTopPosition();
        Vector3 direction = end - cell.GetWorldTopPosition();
        Vector3 start = cell.GetWorldTopPosition() + direction.normalized * 0.1f;
        float distance = direction.magnitude;

        if (Physics.Raycast(start, direction.normalized, out RaycastHit hit, distance))
        {
            Cell hitCell = hit.collider.GetComponentInParent<Cell>();

            if (hitCell != null && hitCell != playerCell && hitCell != cell && !hitCell.IsWalkable)
                return true;
        }

        return false;
    }

    private int GetGridDistance(Cell a, Cell b)
    {
        return Mathf.Abs(a.Coordinates.x - b.Coordinates.x) + Mathf.Abs(a.Coordinates.y - b.Coordinates.y);
    }

    protected virtual void PlanCombatActions(Cell playerCell)
    {
    }
}