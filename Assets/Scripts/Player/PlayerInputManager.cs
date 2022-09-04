using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    public Vector2 Movement => _playerInputActions.Player.Movement.ReadValue<Vector2>();
    public bool JumpInput;

    private Player _player;
    
    private PlayerInputActions _playerInputActions;
    
    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _player = GetComponent<Player>();
    }
    
    private void OnEnable()
    {
        _playerInputActions.Player.Movement.Enable();

        _playerInputActions.Player.Jump.performed += _player.AttemptToJump;
        _playerInputActions.Player.Jump.canceled += _player.Movement.ReleaseJump;

        _playerInputActions.Player.RunStart.performed += _player.Movement.Sprint;
        _playerInputActions.Player.RunFinish.performed += _player.Movement.Sprint;
        
        _playerInputActions.Player.Enable();
    }
    
    private void OnDisable()
    {
        _playerInputActions.Player.Movement.Disable();
        _playerInputActions.Player.Jump .Disable();
        _playerInputActions.Player.RunStart.Disable();
        _playerInputActions.Player.RunFinish.Disable();
    }
}
