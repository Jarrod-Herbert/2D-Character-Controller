using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _playerInputActions;
    private InputAction _movement;

    private Rigidbody2D _rb;
    
    [SerializeField] private float _jumpForce;

    private void Awake()
    {
        _playerInputActions = new PlayerInputActions();
        _rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        _movement = _playerInputActions.Player.Movement;
        _movement.Enable();
        
        _playerInputActions.Player.Jump.performed += DoJump;
        _playerInputActions.Player.Jump.Enable();
    }

    private void DoJump(InputAction.CallbackContext obj)
    {
        _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y + _jumpForce);
    }

    private void OnDisable()
    {
        _movement.Disable();
        _playerInputActions.Player.Jump .Disable();
    }

    private void FixedUpdate()
    {
        // Read movement input
    }
}
