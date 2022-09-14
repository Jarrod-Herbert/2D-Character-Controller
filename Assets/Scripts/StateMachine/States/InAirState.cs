using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    
    public IState DoState(Player player)
    {
        if (player.IsGrounded && player.Movement.YVelocity < 0.05f)
            return player.StateMachine.LandState;

        return player.StateMachine.InAirState;
    }

    public void Enter(Player player)
    {
        player.AnimManager.PlayAnimation(InAir);
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
