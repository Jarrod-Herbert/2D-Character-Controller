using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InAirState : IState
{
    private readonly int InAir = Animator.StringToHash("InAir");
    private float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    private float _fallMultipler = 2.5f;

    public IState DoState(Player player)
    {
        player.Movement.MoveHorizontal(player.InputManager.Movement.x);

        if (coyoteTimeCounter > 0 && player.InputManager.JumpInput)
            return player.StateMachine.JumpState;
        
        if (player.Movement.YVelocity < 0)
        {
            var amount = Vector2.up * (Physics2D.gravity.y * (_fallMultipler - 1) * Time.deltaTime);
            player.Movement.AddVelocity(amount);
        }
        
        if (player.InputManager.JumpInput && player.CheckIfCanJump())
            return player.StateMachine.JumpState;

        coyoteTimeCounter -= Time.deltaTime;

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
