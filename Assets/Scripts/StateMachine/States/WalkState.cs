using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    private readonly int Walk = Animator.StringToHash("Walk");

    public IState DoState(Player player)
    {
        player.Movement.MoveHorizontal(player.InputManager.Movement.x);

        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;

        if (player.Movement.IsSprinting)
            return player.StateMachine.RunState;

        return player.StateMachine.WalkState;
    }
    
    public void Enter(Player player)
    { 
        player.AnimManager.PlayAnimation(Walk);
    }

    public void Exit(Player player)
    {
    }
}
