using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private PlayerInputActions _playerInputActions;
    private InputAction _movement;
    private Rigidbody2D _rb;
    
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _horizontalSpeed;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;
    
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(_groundCheck.position, 0.1f, _groundLayer);
    }

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
        _rb.velocity = new Vector2(_movement.ReadValue<Vector2>().x * _horizontalSpeed,
            _rb.velocity.y);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(_groundCheck.position, 0.1f);
    }
}