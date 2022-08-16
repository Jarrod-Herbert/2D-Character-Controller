using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public Vector2 Movement => _playerInputActions.Player.Movement.ReadValue<Vector2>();

    private PlayerInputActions _playerInputActions;

    private Movement _movement;

    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _movement = GetComponent<Movement>();
    }
    
    private void OnEnable()
    {
        _playerInputActions.Player.Movement.Enable();

        _playerInputActions.Player.Jump.performed += _movement.Jump;
        _playerInputActions.Player.Jump.Enable();
    }
    
    private void OnDisable()
    {
        _playerInputActions.Player.Movement.Disable();
        _playerInputActions.Player.Jump .Disable();
    }
}
