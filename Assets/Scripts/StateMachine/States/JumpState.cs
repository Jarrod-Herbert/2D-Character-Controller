using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    
    public IState DoState(Player player)
    {
        return (player.StateMachine.InAirState);
    }

    public void Enter(Player player)
    {
        player.Movement.Jump();
        player.AnimManager.PlayAnimation(InAir);
        player.InputManager.UseJumpInput();
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
