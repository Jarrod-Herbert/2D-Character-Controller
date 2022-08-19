using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _jumpForce= 10f;
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private float _runSpeed = 5f;

    private Rigidbody2D _rb;
    
    public bool IsSprinting
    {
        get => _isSprinting;
    }

    private bool _isSprinting;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Jump(InputAction.CallbackContext obj)
    {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
    }
    
    public void ReleaseJump(InputAction.CallbackContext obj)
    {
        if (_rb.velocity.y > 0)
            _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y * 0.3f);
    }

    public void MoveHorizontal(float direction)
    {
        _rb.velocity = new Vector2(direction * _moveSpeed, _rb.velocity.y);
    }

    public void RunHorizontal(float direction)
    {
        _rb.velocity = new Vector2(direction * _runSpeed, _rb.velocity.y);
    }

    public float XVelocity => _rb.velocity.x;

    public void SetVelocityZero()
    {
        _rb.velocity = Vector2.zero;
    }

    public void Sprint(InputAction.CallbackContext obj) => _isSprinting = !_isSprinting;
}
