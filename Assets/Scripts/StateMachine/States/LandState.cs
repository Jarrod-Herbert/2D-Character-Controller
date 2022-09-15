using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandState : IState
{
    private readonly int Land = Animator.StringToHash("Land");
    private bool isAnimationFinished;

    public IState DoState(Player player)
    {
        if (player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;
        
        if (player.InputManager.Movement.x != 0)
            return player.StateMachine.MoveState;

        if (isAnimationFinished && player.InputManager.Movement.x == 0)
            return player.StateMachine.IdleState;

        if (!player.IsGrounded)
            return player.StateMachine.InAirState;

        return player.StateMachine.LandState;
    }

    public void Enter(Player player)
    {
        isAnimationFinished = false;
        player.AnimManager.PlayAnimation(Land);
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
        isAnimationFinished = true;
    }
}
