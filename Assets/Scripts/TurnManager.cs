using System.Collections.Generic;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    [SerializeField] private List<EnemyController> _enemies = new List<EnemyController>();
    [SerializeField] private PlayerController _player;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            PlayerStep();
    }

    public void PlayerStep()
    {
        foreach (EnemyController enemy in _enemies)
            enemy.TakeTurn(_player.CurrentCell);

    }
}