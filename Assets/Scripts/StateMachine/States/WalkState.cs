using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    private readonly int Walk = Animator.StringToHash("Walk");

    public IState DoState(Player player)
    {
        player.Movement.MoveHorizontal(player.InputManager.Movement.x);

        if (player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;
        
        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;

        if (player.Movement.IsSprinting)
            return player.StateMachine.RunState;

        if (!player.IsGrounded)
            return player.StateMachine.InAirState;

        return player.StateMachine.WalkState;
    }
    
    public void Enter(Player player)
    { 
        player.AnimManager.PlayAnimation(Walk);
    }

    public void Exit(Player player)
    {
    }

    public void AnimationTrigger()
    {
        throw new System.NotImplementedException();
    }

    public void AnimationFinishTrigger()
    {
        throw new System.NotImplementedException();
    }
}
