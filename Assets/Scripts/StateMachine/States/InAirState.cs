using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    
    public IState DoState(Player player)
    {
        player.Animator.SetFloat("yVelocity", player.Movement.YVelocity);

        if (player.IsGrounded && player.Movement.YVelocity < 0.05f)
            return player.StateMachine.LandState;

        return player.StateMachine.InAirState;
    }

    public void Enter(Player player)
    {
        player.Animator.CrossFade(InAir, 0, 0);
        Debug.Log("Entered in air state");
    }

    public void Exit(Player player)
    {
        Debug.Log("Exiting inAir state");
    }
}
