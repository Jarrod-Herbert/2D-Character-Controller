using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WalkState : IState
{
    public IState DoState(Player player)
    {
        player.Movement.MoveHorizontal(player.InputManager.Movement.x);

        if (Mathf.Abs(player.Movement.XVelocity) <= 0.01f)
            return player.StateMachine.IdleState;

        return player.StateMachine.WalkState;
    }
}
