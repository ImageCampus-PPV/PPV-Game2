using System;
using System.Collections.Generic;
using DragonBones;
using UnityEngine;

public class CustomAnimator : MonoBehaviour
{
    [SerializeField] private UnityArmatureComponent _armatureComponent;

    public UnityArmatureComponent ArmatureComponent => _armatureComponent;

    [Serializable]
    private class AnimationMapping
    {
        public AnimationStates state;
        public string animationName;
    }

    [SerializeField]
    private List<AnimationMapping> _animationMappings = new();

    private AnimationStates _currentState;

    public void Play(AnimationStates state, int playTimes = 0, float timeScale = 1f, bool forceRestart = false)
    {
        string animationName = GetAnimationName(state);

        if (string.IsNullOrEmpty(animationName))
        {
            Debug.LogWarning($"{name}: No animation mapping found for {state}.");
            return;
        }

        if (!AnimationExists(animationName))
        {
            Debug.LogWarning($"{name}: DragonBones animation '{animationName}' does not exist.");
            return;
        }

        if (_currentState == state && !forceRestart)
            return;

        _armatureComponent.armature.animation.timeScale = timeScale;
        _currentState = state;

        _armatureComponent.armature.animation.Play(animationName, playTimes);
    }


    public float GetAnimationDuration(AnimationStates state)
    {
        string animationName = GetAnimationName(state);

        if (string.IsNullOrEmpty(animationName))
            return 0f;

        if (!AnimationExists(animationName))
            return 0f;


        Debug.Log("Timescale: " + _armatureComponent.armature.animation.timeScale);
        return _armatureComponent.armature.animation.animations[animationName].duration / _armatureComponent.armature.animation.timeScale;
    }

    public bool AnimationExists(string animationName)
    {
        if (_armatureComponent == null || _armatureComponent.armature == null || _armatureComponent.armature.animation == null)
            return false;

        return _armatureComponent.armature.animation.animations.ContainsKey(animationName);
    }

    public bool IsAnimationPlaying()
    {
        return _armatureComponent != null &&
               _armatureComponent.armature != null &&
               _armatureComponent.armature.animation != null &&
               _armatureComponent.armature.animation.isPlaying;
    }

    public AnimationStates GetCurrentState()
    {
        return _currentState;
    }

    private string GetAnimationName(AnimationStates state)
    {
        foreach (AnimationMapping mapping in _animationMappings)
        {
            if (mapping.state == state)
                return mapping.animationName;
        }

        return null;
    }
}



public enum AnimationStates
{
    Idle,
    Walk,
    Stun,
    Attack,
    TakeDamage,
    CounterAbilty,
    LagSpikeAbility
}

