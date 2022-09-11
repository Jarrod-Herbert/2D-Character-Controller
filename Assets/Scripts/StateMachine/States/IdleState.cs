using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    private readonly int Idle = Animator.StringToHash("Idle");
    
    public IState DoState(Player player)
    {
        if (player.InputManager.Movement.x != 0 && player.Movement.IsSprinting)
            return player.StateMachine.RunState;
        
        if (player.InputManager.Movement.x != 0 && !player.Movement.IsSprinting)
            return player.StateMachine.WalkState;
        
        else return player.StateMachine.IdleState;
    }

    public void Enter(Player player)
    {
        player.AnimManager.PlayAnimation(Idle);
    }

    public void Exit(Player player)
    {
    }
}
