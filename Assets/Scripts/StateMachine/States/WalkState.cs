using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    private readonly int Walk = Animator.StringToHash("Walk");
    private bool _facingRight = true;
    
    public IState DoState(Player player)
    {
        if (ShouldFlip(player))
            Flip(player.SpriteRenderer);
        
        player.Movement.MoveHorizontal(player.InputManager.Movement.x);

        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;

        return player.StateMachine.WalkState;
    }

    private bool ShouldFlip(Player player)
    {
        var inputDir = player.InputManager.Movement.x >= 0 ? true : false;
        return _facingRight == inputDir;
    }

    private void Flip(SpriteRenderer sr)
    {
        _facingRight = !_facingRight;
        sr.flipX = _facingRight;
    }


    public void Enter(Player player)
    {
        player.Animator.CrossFade(Walk, 0,0);
    }

    public void Exit(Player player)
    {
    }
}
