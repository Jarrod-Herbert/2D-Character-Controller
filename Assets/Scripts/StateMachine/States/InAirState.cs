using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    
    public IState DoState(Player player)
    {
        if (coyoteTimeCounter > 0 && player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;

        coyoteTimeCounter -= Time.deltaTime;
        Debug.Log($"CoyoteTimer {coyoteTimeCounter}");

        if (player.IsGrounded && player.Movement.YVelocity < 0.05f)
            return player.StateMachine.LandState;

        return player.StateMachine.InAirState;
    }

    public void Enter(Player player)
    {
        player.AnimManager.PlayAnimation(InAir);
        coyoteTimeCounter = coyoteTime;
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
