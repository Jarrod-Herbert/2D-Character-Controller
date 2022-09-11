using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{
    private Animator _animator;
    private Player _player;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _player = GetComponent<Player>();
    }

    private void Update()
    {
        _animator.SetFloat("yVelocity", _player.Movement.YVelocity);
    }

    public void PlayAnimation(int animID)
    {
        _animator.Play(animID);
    }
}
