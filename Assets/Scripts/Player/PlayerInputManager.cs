using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public InputAction Movement { get; private set; }
    
    private PlayerInputActions _playerInputActions;

    private Movement _movement;

    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _movement = GetComponent<Movement>();
    }
    
    private void OnEnable()
    {
        Movement = _playerInputActions.Player.Movement;
        Movement.Enable();

        _playerInputActions.Player.Jump.performed += _movement.Jump;
        _playerInputActions.Player.Jump.Enable();
    }
    
    private void OnDisable()
    {
        Movement.Disable();
        _playerInputActions.Player.Jump .Disable();
    }
}
