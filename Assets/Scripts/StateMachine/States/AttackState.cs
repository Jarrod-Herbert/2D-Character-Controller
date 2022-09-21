using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    private int _attackIndex;

    private bool _animDone;

    private readonly int PunchA = Animator.StringToHash("punch_a");
    private readonly int PunchB = Animator.StringToHash("punch_b");
    private readonly int PunchC = Animator.StringToHash("punch_c");
    private readonly int PunchD = Animator.StringToHash("punch_d");
    
    public IState DoState(Player player)
    {
        if (_animDone && player.InputManager.AttackInput)
            PerformAttack(player);
        
        return player.StateMachine.AttackState;
    }
    

    public void Enter(Player player)
    {
        _attackIndex = 0;
        
        PerformAttack(player);
    }

    private void PerformAttack(Player player)
    {
        if (_attackIndex == 0)
            Punch(player, PunchA);
        else if (_attackIndex == 1)
            Punch(player, PunchB);
        else if (_attackIndex == 2)
            Punch(player, PunchC);
        else if (_attackIndex == 3) 
            Punch(player, PunchD);
    }

    private void Punch(Player player, int id)
    {
        player.InputManager.UseAttackInput();
        player.AnimManager.PlayAnimation(id);
        _animDone = false;
        _attackIndex++;
    }

    public void Exit(Player player)
    {
    }

    public void AnimationTrigger()
    {
        Debug.Log($"[ PUNCH {_attackIndex} ]");
    }

    public void AnimationFinishTrigger()
    {
        _animDone = true;
    }
}
