using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : CoreComponent
{
    [SerializeField] private float _jumpForce= 10f;
    public Rigidbody2D RB { get; private set; }

    private void Awake()
    {
        RB = GetComponentInParent<Rigidbody2D>();
    }

    public void Jump(InputAction.CallbackContext obj)
    {
        Debug.Log("Jumped from Movement component!");
        RB.velocity = new Vector2(RB.velocity.x, RB.velocity.y + _jumpForce);
    }
}
