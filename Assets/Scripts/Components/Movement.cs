using System;
using System.Collections;
using System.Collections.Generic;
using Mono.Cecil.Cil;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _jumpForce= 10f;
    [SerializeField] private float _moveSpeed = 9f;
    [SerializeField] private float _walkSpeed = 5f;
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

    public void WalkHorizontal(float direction)
    {
        _rb.velocity = new Vector2(direction * _walkSpeed, _rb.velocity.y);
    }
    
    public float XVelocity => _rb.velocity.x;
    public float YVelocity => _rb.velocity.y;

    public void SetVelocityZero()
    {
        _rb.velocity = Vector2.zero;
    }

    public void AddVelocity(Vector2 amount)
    {
        _rb.velocity += amount;
    }

    public void SetVelocity(Vector2 amount) => _rb.velocity = amount;
    
    public bool IsWalking
    {
        get => _isWalking;
    }

    private bool _isWalking;

    public void Walk(InputAction.CallbackContext obj) => _isWalking = !_isWalking;

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
