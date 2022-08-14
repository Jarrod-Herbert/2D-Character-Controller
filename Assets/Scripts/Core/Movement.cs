using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : CoreComponent
{
    [SerializeField] private float _jumpForce= 10f;
    [SerializeField] private float _moveSpeed = 3f;
    
    public Rigidbody2D RB { get; private set; }

    private Player _player;

    private void Awake()
    {
        RB = GetComponentInParent<Rigidbody2D>();
        _player = GetComponentInParent<Player>();
    }

    public void Jump(InputAction.CallbackContext obj)
    {
        Debug.Log("Jumped from Movement component!");
        RB.velocity = new Vector2(RB.velocity.x, RB.velocity.y + _jumpForce);
    }

    public void MoveHorizontal()
    {
        RB.velocity = new Vector2(_player.InputManager.Movement.ReadValue<Vector2>().x * _moveSpeed, RB.velocity.y);
    }

    private void FixedUpdate()
    {
        MoveHorizontal();
    }
}
