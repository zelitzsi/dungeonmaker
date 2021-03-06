﻿using System;
using System.IO;
using UnityEngine;

public class TreasureChest : MonoBehaviour, ICustomSerializable
{

    public enum ChestItem
    {
        None,
        Key,
        GustItem,
        ShadowCloak
    }

    [PlayerEditableEnum("Contents", typeof(ChestItem))]
    public ChestItem contents;

    public GameObject[] prefabOptions;
    public AudioClip collectSound;

    Collider2D collider2d;
    Renderer renderer;
    Circuit circuit;

    void Start()
    {
        collider2d = GetComponent<Collider2D>();
        renderer = GetComponent<Renderer>();
        SetupCircuit();
    }

    void FixedUpdate()
    {
        SetupCircuit();
        if (circuit && LevelEditor.main.currentRoom.Contains(transform.position.ToGrid()))
        {
            collider2d.enabled = circuit.Powered;
            renderer.enabled = circuit.Powered;
        }   
    }

    void SetupCircuit()
    {
        if (circuit != null)
            return;
        circuit = GetComponent<Circuit>();
        if (circuit == null)
            return;
        circuit.gateConditions.Add(() => { return !enabled; });
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || other.isTrigger)
            return;
        enabled = false;
        collider2d.enabled = false;
        renderer.enabled = false;
        if (collectSound)
            Camera.main.GetComponent<AudioSource>().PlayOneShot(collectSound);
        if (contents == ChestItem.None)
            return;
        if (contents == ChestItem.Key)
        {
            other.GetComponentInParent<Player>().keys++;
            return;
        }
        GameObject prefab = Array.Find(prefabOptions, (p) => { return p.name == contents.ToString(); });
        if (!prefab)
        {
            Debug.LogWarning("Could not find prefab on treasure chest: " + contents.ToString());
            return;
        }
        GameObject go = Instantiate(prefab, other.transform);
        go.transform.localPosition = Vector3.zero;
        go.name = prefab.name;
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
