using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    private readonly int Idle = Animator.StringToHash("Idle");
    
    public IState DoState(Player player)
    {
        if (player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;
        
        if (player.InputManager.Movement.x != 0 && player.Movement.IsSprinting)
            return player.StateMachine.RunState;
        
        if (player.InputManager.Movement.x != 0 && !player.Movement.IsSprinting)
            return player.StateMachine.WalkState;
        
        if (!player.IsGrounded)
            return player.StateMachine.InAirState;
        
        return player.StateMachine.IdleState;
    }

    public void Enter(Player player)
    {
        player.AnimManager.PlayAnimation(Idle);
        player.Movement.SetVelocityZero();
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
