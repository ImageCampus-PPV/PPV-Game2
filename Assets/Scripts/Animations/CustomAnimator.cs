using DragonBones;
using UnityEngine;

public class CustomAnimator : MonoBehaviour
{
    [SerializeField] private UnityArmatureComponent _armatureComponent;
    public UnityArmatureComponent ArmatureComponent => _armatureComponent;

    private AnimationStates _currentState;

    public void Play(AnimationStates state, int playTimes = 0)
    {
        if (_currentState == state)
            return;

        _currentState = state;
        string animationName = state.ToString().ToUpper();

        if (AnimationExists(animationName))
            _armatureComponent.armature.animation.Play(animationName, playTimes);
    }

    public float GetAnimationDuration(AnimationStates state)
    {
        string animationName = state.ToString().ToUpper();

        if (AnimationExists(animationName))
            return _armatureComponent.armature.animation.animations[animationName].duration;

        return 0;
    }

    public bool AnimationExists(string animationName)
    {
        if (_armatureComponent == null || _armatureComponent.armature == null || _armatureComponent.armature.animation == null)
            return false;

        return (_armatureComponent.armature.animation.animations.ContainsKey(animationName));
    }

    public bool IsAnimationPlaying()
    {
        return _armatureComponent.armature.animation.isPlaying;
    }

    public AnimationStates GetCurrentState() => _currentState;
}


public enum AnimationStates
{
    Idle,
    Walk,
    Attack,
    Death
}

