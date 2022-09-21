using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    private int _attackIndex;

    private bool _animDone;
    
    private readonly int[] _anims = new int[4]
    {
        Animator.StringToHash("punch_a"),
        Animator.StringToHash("punch_b"),
        Animator.StringToHash("punch_c"),
        Animator.StringToHash("punch_d"),
    };
    
    public IState DoState(Player player)
    {
        if (!_animDone)
            return player.StateMachine.AttackState;
        
        if (_attackIndex == 4)
            return player.StateMachine.IdleState;
        
        if (player.InputManager.AttackInput)
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
        Punch(player, _anims[_attackIndex]);
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
