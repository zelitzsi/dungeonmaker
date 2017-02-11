﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles objects that should touching floor or else fall.
/// </summary>
public class Gravity : MonoBehaviour {

    [ReadOnly]
    public List<Collider2D> touching = new List<Collider2D>();

    void OnTriggerEnter2D(Collider2D other)
    {
        touching.Add(other);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        touching.Remove(other);
        CheckForDeath();
    }

    void CheckForDeath()
    {
        if (touching.Count <= 0)
            Destroy(gameObject);
    }
}