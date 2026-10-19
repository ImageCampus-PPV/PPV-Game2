using DragonBones;
using UnityEngine;

public class TestAnimation : MonoBehaviour
{
    [SerializeField] private UnityArmatureComponent _armature;
    [SerializeField] private float _timer;
    [SerializeField] private float _timeToChangeAnimation;

    private int _totalAnimations;
    private int _animationIndex;


    void Start()
    {
        _timer = 0f;
        _armature.animation.Play("IDLE");
        _totalAnimations = _armature.animation.animations.Count;
        _animationIndex = 0;
        _armature.animation.Play(_armature.animation.animationNames[_animationIndex]);
    }

    void Update()
    {
        _timer += Time.deltaTime;

        if (_timer > _timeToChangeAnimation)
        {
            if (_animationIndex + 1 < _totalAnimations)
                _animationIndex++;
            else
                _animationIndex = 0;

            _armature.animation.Play(_armature.animation.animationNames[_animationIndex]);
            _timer = 0;
        }
    }
}
