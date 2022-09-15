using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _jumpForce= 10f;
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private float _runSpeed = 5f;
    [SerializeField] private float _maxFallSpeed = 12f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        ResetJumpsRemaining();
    }

    private void Update()
    {
        if (_rb.velocity.y < -_maxFallSpeed)
            ClampFallSpeed();
    }

    private void ClampFallSpeed()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, -_maxFallSpeed);
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
    public float YVelocity => _rb.velocity.y;

    public void SetVelocityZero()
    {
        _rb.velocity = Vector2.zero;
    }
    
    public bool IsSprinting
    {
        get => _isSprinting;
    }

    private bool _isSprinting;

    public void Sprint(InputAction.CallbackContext obj) => _isSprinting = !_isSprinting;

    #region Jump
    
    [SerializeField] private int _maxJumps = 1;
    
    public int JumpsRemaining { get; private set; }
    
    public void ResetJumpsRemaining()
    {
        JumpsRemaining = _maxJumps;
    }
    
    public void Jump()
    {
        _rb.velocity = new Vector2(_rb.velocity.x, _jumpForce);
        UseJump();
    }

    private void UseJump()
    {
        JumpsRemaining--;
    }

    public bool AtMaxJumps()
    {
        return JumpsRemaining == _maxJumps;
    }

    #endregion
}
