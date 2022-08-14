using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public InputAction Movement { get; private set; }
    
    private PlayerInputActions _playerInputActions;
    private Player _player;

    private void Awake()
    {
        _player = GetComponentInParent<Player>();
        _playerInputActions = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        Movement = _playerInputActions.Player.Movement;
        Movement.Enable();

        _playerInputActions.Player.Jump.performed += _player.Core.Movement.Jump;
        _playerInputActions.Player.Jump.Enable();
    }
    
    private void OnDisable()
    {
        Movement.Disable();
        _playerInputActions.Player.Jump .Disable();
    }
}
