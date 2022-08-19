using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StateMachine : MonoBehaviour
{
    private IState _currentState;
    private Player _player;

    public WalkState WalkState;
    public IdleState IdleState;
    public JumpState JumpState;
    public RunState RunState;

    private void Awake()
    {
        _player = GetComponent<Player>();
        
        WalkState = new WalkState();
        IdleState = new IdleState();
        JumpState = new JumpState();
        RunState = new RunState();

        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        _currentState = IdleState;
        _currentState.Enter(_player);
    }

    private void ChangeState(IState newState)
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
}
