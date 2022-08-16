using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    IState DoState(Player player);

    void Enter(Player player);

    void Exit(Player player);

}
