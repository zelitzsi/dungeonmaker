﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum ObjectType
{
    None,
    Floor,
    FloorAttachment, // E.g., spikes, grass, etc.
    Wall,
    Interactable,
    Enemy
}

public enum Category
{
    Basic,
    Interactive,
    Circuits,
    Enemies,
    Decor
}

public class ObjectData : MonoBehaviour, ICustomSerializable
{
    public string uiName;
    // Tooltip text
    [TextArea]
    public string createText;
    public Category category;

    public Guid guid; // Set outside of this behavior...
    public ObjectType type;
    [Tooltip("Can see through tile for the purposes of room calculation")]
    public bool seeThrough = true;
    [Tooltip("Counts as ground for the purposes of falling")]
    public bool ground = false;
    [Tooltip("Does the tile prevent pathing?")]
    public bool blocksPathing = false;
    public int sortOrderModifier;
    public bool hideInPlayMode = false;

    Renderer renderer;

    void Start()
    {
        renderer = GetComponentInChildren<Renderer>();
        UpdateSortOrder();
    }

    void Update()
    {
        if (hideInPlayMode)
            renderer.enabled = (LevelEditor.main.mode >= EditMode.Create);
        UpdateSortOrder();
    }

    void UpdateSortOrder()
    {
        // Sprite draw order is dependent on Y because we are 2.5D. However, we multiple everything by a constant so that 
        // we don't get jumps when rounding. Ties are broken by object type on layer.
        if (renderer as SpriteRenderer != null)
            (renderer as SpriteRenderer).sortingOrder = sortOrderModifier + -1 * (int)Math.Floor(transform.position.y * 4) + (int)type;
    }

    public bool Navigable
    {
        get
        {
            return type != ObjectType.Wall;
        }
    }

    public void Deserialize(BinaryReader br)
    {
        transform.Rotate(Vector3.forward * br.ReadSingle());
    }

    public void Serialize(BinaryWriter bw)
    {
        bw.Write(transform.rotation.eulerAngles.z);
    }
}


