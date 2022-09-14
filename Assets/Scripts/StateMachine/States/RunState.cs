using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RunState : IState
{
    private readonly int Run = Animator.StringToHash("Run");
    
    public IState DoState(Player player)
    {
        player.Movement.RunHorizontal(player.InputManager.Movement.x);

        if (!player.Movement.IsSprinting && player.InputManager.Movement.x != 0)
            return player.StateMachine.WalkState;
        
        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;

        return player.StateMachine.RunState;
    }

    public void Enter(Player player)
    {
        player.AnimManager.PlayAnimation(Run);
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
