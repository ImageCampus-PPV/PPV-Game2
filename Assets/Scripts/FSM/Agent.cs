using UnityEngine;

class Agent : MonoBehaviour
{
    public FSM _fsm;

    public Transform _target;
    public float _speed;
    public float _explodeDistance;
    public float _lostDistance;

    public Transform _wayPoint1;
    public Transform _wayPoint2;
    public float _chaseDistance;

    public void Start()
    {
        _fsm = new FSM(typeof(PatrolState));

        _fsm.AddState<PatrolState>(
            onTickParameters: () => new object[] { _wayPoint1, _wayPoint2, transform, _target, _speed, _chaseDistance, Time.deltaTime }
            );

        _fsm.AddState<ChaseState>(
            onTickParameters: () => new object[] { transform, _target, _speed, _explodeDistance, _lostDistance, Time.deltaTime }
            );

        _fsm.AddState<ExplodeState>();
    }

    private void Update()
    {
        _fsm.Tick();
    }
}
