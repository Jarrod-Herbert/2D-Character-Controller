using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : IState
{
    private int _attackIndex;

    private bool _animDone;

    private readonly float _animInputBuffer = 0.275f;
    private float _animInputCounter;
    
    private readonly int[] _anims = new int[4]
    {
        Animator.StringToHash("punch_a"),
        Animator.StringToHash("punch_b"),
        Animator.StringToHash("punch_c"),
        Animator.StringToHash("punch_d"),
    };

    private readonly Vector2[] _addedVelocity = new Vector2[]
    {
        new Vector2(2f, 0),
        new Vector2(2.5f, 0),
        new Vector2(0, 0),
        new Vector2(5,0)
    }; 
    
    public IState DoState(Player player)
    {
        if (!_animDone)
            return player.StateMachine.AttackState;
        
        if (_attackIndex == 4)
            return player.StateMachine.IdleState;
        
        if (_animInputCounter < 0)
            return player.StateMachine.IdleState;
        
        if (player.InputManager.AttackInput)
            PerformAttack(player);

        _animInputCounter -= Time.deltaTime;
        return player.StateMachine.AttackState;
    }
    

    public void Enter(Player player)
    {
        _attackIndex = 0;
        PerformAttack(player);
    }

    private void PerformAttack(Player player)
    {
        player.InputManager.UseAttackInput();
        Punch(player);
        
        _attackIndex++;
    }

    private void Punch(Player player)
    {
        _animDone = false;
        
        player.AnimManager.PlayAnimation(_anims[_attackIndex]);
        player.Movement.SetVelocity(_addedVelocity[_attackIndex] * player.FacingDirection);
    }

    public void Exit(Player player)
    {
    }

    public void AnimationTrigger()
    {
        Debug.Log($"[ ATTACK {_attackIndex} ]");
    }

    public void AnimationFinishTrigger()
    {
        _animDone = true;
        _animInputCounter = _animInputBuffer;
    }
}
