using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public PlayerInputManager InputManager { get; private set; }
    public Movement Movement { get; private set; }
    public Sensors Sensors { get; private set;  }
    public StateMachine StateMachine { get; private set; }
    public Animator Animator { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    
    private void Awake()
    {
        InputManager = GetComponent<PlayerInputManager>();
        Movement = GetComponent<Movement>();
        Sensors = GetComponent<Sensors>();
        StateMachine = GetComponent<StateMachine>();
        Animator = GetComponent<Animator>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }
}
