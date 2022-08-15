using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private float _jumpForce= 10f;
    [SerializeField] private float _moveSpeed = 3f;

    private Rigidbody2D _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Jump(InputAction.CallbackContext obj)
    {
        _rb.velocity = new Vector2(_rb.velocity.x, _rb.velocity.y + _jumpForce);
    }

    public void MoveHorizontal(int direction)
    {
        _rb.velocity = new Vector2(direction * _moveSpeed, _rb.velocity.y);
    }
}
