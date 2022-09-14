using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandState : IState
{
    private readonly int Land = Animator.StringToHash("Land");
    private bool isAnimationFinished;

    public IState DoState(Player player)
    {
        if (isAnimationFinished)
        {
            return player.StateMachine.IdleState;
        }

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
