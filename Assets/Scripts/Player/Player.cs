using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public PlayerInputManager InputManager { get; private set; }
    public Movement Movement { get; private set; }
    public Sensors Sensors { get; private set;  }
    public StateMachine StateMachine { get; private set; }
    public Animator Animator { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }

    private int _facingDirection = 1;
    
    private void Awake()
    {
        InputManager = GetComponent<PlayerInputManager>();
        Movement = GetComponent<Movement>();
        Sensors = GetComponent<Sensors>();
        StateMachine = GetComponent<StateMachine>();
        Animator = GetComponent<Animator>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        CheckIfShouldFlipSprite(InputManager.Movement.normalized.x);
        
        if (ShouldResetJumps())
            Movement.ResetJumpsRemaining();
    }

    private void CheckIfShouldFlipSprite(float normalizedX)
    {
        if (normalizedX != 0 && (int)normalizedX != _facingDirection) Flip();
    }

    private void Flip()
    {
        _facingDirection *= -1;
        SpriteRenderer.flipX = !SpriteRenderer.flipX;
    }

    public void AttemptToJump(InputAction.CallbackContext obj)
    {
        if (!CheckIfCanJump()) return;
        
        StateMachine.ChangeState(StateMachine.JumpState);
        // Movement.Jump();
    }
    
    public bool CheckIfCanJump()
    {
        return Sensors.IsGrounded || (!Sensors.IsGrounded && Movement.JumpsRemaining > 0);
    }

    private bool ShouldResetJumps()
    {
        return Sensors.IsGrounded && Movement.YVelocity <= 0.01f;
    }

    public bool IsGrounded => Sensors.IsGrounded;
}
