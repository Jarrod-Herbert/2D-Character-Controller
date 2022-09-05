using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    
    public IState DoState(Player player)
    {
        if (player.IsGrounded && player.Movement.XVelocity == 0 && player.Movement.YVelocity <= 0.05f)
            return player.StateMachine.IdleState;
        
        if (player.IsGrounded && player.Movement.IsSprinting && player.Movement.YVelocity <= 0.05f)
            return player.StateMachine.RunState;
        
        if (player.IsGrounded && !player.Movement.IsSprinting && player.Movement.YVelocity <= 0.05f)
            return player.StateMachine.WalkState;
        
        return player.StateMachine.InAirState;
    }

    public void Enter(Player player)
    {
        Debug.Log("Entered in air state");
    }

    public void Exit(Player player)
    {
        Debug.Log("Exiting inAir state");
    }
}
