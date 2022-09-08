using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LandState : IState
{
    private readonly int Land = Animator.StringToHash("Land");
    
    private int exitAfter = 44;
    private int counter;
    
    public IState DoState(Player player)
    {
        if (counter < exitAfter)
        {
            counter++;
            Debug.Log(counter);
            return player.StateMachine.LandState;
        }

        return player.StateMachine.IdleState;
    }

    public void Enter(Player player)
    {
        counter = 0;
        player.Animator.Play(Land);
        Debug.Log("enter land state");
    }

    public void Exit(Player player)
    {
        Debug.Log("exit land state");
    }
}
