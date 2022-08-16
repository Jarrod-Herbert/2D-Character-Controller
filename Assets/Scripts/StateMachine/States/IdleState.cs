using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : IState
{
    public IState DoState(Player player)
    {
        player.Movement.SetVelocityZero();

        if (player.InputManager.Movement.x != 0)
            return player.StateMachine.WalkState;
        
        else return player.StateMachine.IdleState;
    }
}
