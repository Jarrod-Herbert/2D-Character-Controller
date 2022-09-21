using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : MonoBehaviour
{
    [SerializeField] private float _inputBufferDuration = 0.2f;
    public Vector2 Movement => _playerInputActions.Player.Movement.ReadValue<Vector2>();
    public bool AttackInput;

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

        _playerInputActions.Player.Jump.started += Jump;

        _playerInputActions.Player.WalkStart.performed += _player.Movement.Walk;
        _playerInputActions.Player.WalkFinish.performed += _player.Movement.Walk;

        _playerInputActions.Player.Attack.performed += Attack;
        
        _playerInputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _playerInputActions.Player.Movement.Disable();
        _playerInputActions.Player.Jump.Disable();
        _playerInputActions.Player.WalkStart.Disable();
        _playerInputActions.Player.WalkFinish.Disable();
    }

    private void Jump(InputAction.CallbackContext context)
    {
        JumpInput = true;
        StartCoroutine(CancelJumpInput());
    }

    private IEnumerator CancelJumpInput()
    {
        yield return new WaitForSeconds(_inputBufferDuration);
        JumpInput = false;
    }

    public void UseJumpInput()
    {
        JumpInput = false;
    }
    
    private void Attack(InputAction.CallbackContext context)
    {
        AttackInput = true;
    }
}
