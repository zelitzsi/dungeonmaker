﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Door : MonoBehaviour, ICustomSerializable {

    public bool open = false;
    public bool Open
    {
        get { return open; }
        set
        {
            if (open != value)
                LevelEditor.main.currentRoomDirty = true;
            open = value;
            data.seeThrough = open;
        }
    }

    [PlayerEditable("Invert")]
    public bool invert = false;

    public AudioClip sound;

    Animator animator;
    public GameObject child;
    ObjectData data;

    void Awake()
    {
        animator = GetComponent<Animator>();
        data = GetComponent<ObjectData>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GetComponent<Circuit>() != null || other.isTrigger || !enabled)
            return;
        Open = true;
        AudioSource.PlayClipAtPoint(sound, transform.position);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || GetComponent<Circuit>() != null || other.isTrigger || !enabled)
            return;
        Open = false;
        AudioSource.PlayClipAtPoint(sound, transform.position);
    }

    void Update()
    {
        animator.SetBool("open", Open);
    }

    void FixedUpdate()
    {
        Circuit circuit = GetComponent<Circuit>();
        if (circuit)
            Open = circuit.Powered ^ invert;
        child.layer = Open ? LayerMask.NameToLayer("IgnorePlayer") : LayerMask.NameToLayer("Default");
        
    }

    public void Serialize(BinaryWriter bw)
    {
        ObjectSerializer.Serialize(bw, this);
    }

    public void Deserialize(BinaryReader br)
    {
        ObjectSerializer.Deserialize(br, this);
    }
}
