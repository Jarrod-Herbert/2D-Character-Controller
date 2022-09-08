using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    
    public IState DoState(Player player)
    {
        player.Animator.SetFloat("yVelocity", player.Movement.YVelocity);
        return (player.StateMachine.InAirState);
    }

    public void Enter(Player player)
    {
        player.Movement.Jump();
        player.Animator.CrossFade(InAir, 0, 0);
        
    }

    public void Exit(Player player)
    {
    }
}
