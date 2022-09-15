using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    private readonly int Walk = Animator.StringToHash("Walk");
    
    public IState DoState(Player player)
    {
        player.Movement.WalkHorizontal(player.InputManager.Movement.x);
        
        if (player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;

        if (!player.Movement.IsWalking && player.InputManager.Movement.x != 0)
            return player.StateMachine.MoveState;
        
        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;
        
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
        // throw new System.NotImplementedException();
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
