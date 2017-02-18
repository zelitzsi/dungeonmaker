﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Drone : MonoBehaviour
{

    public enum AIState
    {
        Idle,
        Wander
    }

    [ReadOnly]
    public AIState currentState = AIState.Wander;
    public int decisionInterval = 60;
    int remFrames;

    Wander wander;

    void Start()
    {
        wander = GetComponent<Wander>();
    }

    void FixedUpdate()
    {
        if (remFrames-- <= 0)
        {
            currentState = (AIState)Random.Range(0, System.Enum.GetNames(typeof(AIState)).Length);
            remFrames = decisionInterval;
            if (currentState == AIState.Idle)
                wander.enabled = false;
            else
                wander.enabled = true;
            
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {    
        if (collision.collider.tag != "Player")
            return;
        int dmg = collision.collider.GetComponent<Health>().Damage(1);
        if (dmg > 0)
        {
            Vector2 dir = (collision.transform.position - transform.position).normalized;
            collision.rigidbody.AddForce(dir * 1200f);
        }
    }

}
