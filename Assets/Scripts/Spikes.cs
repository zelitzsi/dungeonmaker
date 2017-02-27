﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spikes : MonoBehaviour {

    public bool active = false;
    public int framesDown;
    public int framesUp;
    [ReadOnly] public int remFrames;

    public int damage;

    Animator animator;

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Health health = other.GetComponent<Health>();
        if (health == null)
            return;
        health.Damage(damage, (other.transform.position - transform.position).normalized * 1200f);
    }

    void FixedUpdate()
    {
        Circuit circuit = GetComponent<Circuit>();
        if (circuit)
            active = circuit.Powered;
        else
        {
            remFrames--;
            if (remFrames <= 0)
            {
                if (active)
                    remFrames = framesDown;
                else
                    remFrames = framesUp;
                active = !active;
                
            }
        }
        GetComponent<Collider2D>().enabled = active;
        animator.SetBool("active", active);
        
    }
}
