using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{
    public Core Core { get; private set; }
    public PlayerInputManager InputManager { get; private set; }
    
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private LayerMask _groundLayer;
    private void Awake()
    {
        Core = GetComponentInChildren<Core>();
        InputManager = GetComponent<PlayerInputManager>();
    }
    
    private bool IsGrounded()
    {
        return Physics2D.OverlapCircle(_groundCheck.position, 0.1f, _groundLayer);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(_groundCheck.position, 0.1f);
    }
}
