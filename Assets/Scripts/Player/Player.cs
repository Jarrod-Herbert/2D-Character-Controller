using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Core Core { get; private set; }
    public PlayerInputManager InputManager { get; private set; }
    
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _horizontalSpeed;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;

    private Rigidbody2D _rb;

    private void Awake()
    {
        Core = GetComponentInChildren<Core>();
        InputManager = GetComponent<PlayerInputManager>();

        _rb = GetComponent<Rigidbody2D>();
    }
    
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(_groundCheck.position, 0.1f, _groundLayer);
    }

    private void FixedUpdate()
    {
        _rb.velocity = new Vector2(InputManager.Movement.ReadValue<Vector2>().x * _horizontalSpeed,
            _rb.velocity.y);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(_groundCheck.position, 0.1f);
    }
}
