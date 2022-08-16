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

    private void Awake()
    {
        _player = GetComponent<Player>();
        
        WalkState = new WalkState();
        IdleState = new IdleState();
        JumpState = new JumpState();
        
        _currentState = IdleState;
    }

    private void Update()
    {
        _currentState = _currentState.DoState(_player);
    }
}
