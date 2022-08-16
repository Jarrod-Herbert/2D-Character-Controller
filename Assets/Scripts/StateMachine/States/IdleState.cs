using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    private readonly int Idle = Animator.StringToHash("Idle");
    
    public IState DoState(Player player)
    {
        if (player.InputManager.Movement.x != 0)
            return player.StateMachine.WalkState;
        
        else return player.StateMachine.IdleState;
    }

    public void Enter(Player player)
    {
        player.Animator.CrossFade(Idle, 0,0);
    }

    public void Exit(Player player)
    {
    }
}
