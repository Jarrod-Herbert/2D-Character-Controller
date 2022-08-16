using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IState
{
    IState DoState(Player player);
}
