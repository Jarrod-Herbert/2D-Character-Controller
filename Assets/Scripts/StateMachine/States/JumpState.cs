using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JumpState : IState
{
    public IState DoState(Player player)
    {
        return (player.StateMachine.InAirState);
    }

    public void Enter(Player player)
    {
        player.Movement.Jump();
    }

    public void Exit(Player player)
    {
    }
}
