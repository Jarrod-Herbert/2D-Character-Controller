using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    private int attackNumber;
    
    private readonly int PunchA = Animator.StringToHash("punch_a");
    
    public IState DoState(Player player)
    {
        return player.StateMachine.AttackState;
    }

    public void Enter(Player player)
    {
        attackNumber = 0;
        player.AnimManager.PlayAnimation(PunchA);
    }

    public void Exit(Player player)
    {
    }

    public void AnimationTrigger()
    {
    }

    public void AnimationFinishTrigger()
    {
    }
}
