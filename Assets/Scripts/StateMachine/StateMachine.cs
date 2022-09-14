using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private IState _currentState;
    private Player _player;

    public readonly WalkState WalkState = new();
    public readonly IdleState IdleState = new();
    public readonly JumpState JumpState = new();
    public readonly RunState RunState = new();
    public readonly InAirState InAirState = new();
    public readonly LandState LandState = new();

    private void Awake()
    {
        _player = GetComponent<Player>();
    }

    private void Start()
    {
        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        _currentState = IdleState;
        _currentState.Enter(_player);
    }

    public void ChangeState(IState newState)
    {
        _currentState.Exit(_player);
        _currentState = newState;
        _currentState.Enter(_player);
    }
    
    private void Update()
    {
        var returnState = _currentState.DoState(_player);

        if (returnState == _currentState)
            return;
        
        ChangeState(returnState);
    }

    public void AnimationFinishTrigger()
    {
        _currentState.AnimationFinishTrigger();
    }
}
