﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyGate : MonoBehaviour {

    Circuit circuit;

    void Update()
    {
        if (circuit == null)
        {
            circuit = GetComponent<Circuit>();
            if (circuit != null)
                SetupCircuit();
        }
    }

    void SetupCircuit()
    {
        circuit.gateConditions.Add(() => { return AllEnemiesDeadInRoom(); });
    }

    bool AllEnemiesDeadInRoom()
    {
        foreach (MapNode node in LevelEditor.main.currentRoom)
        {
            List<ObjectData> goList = LevelEditor.main.tilemap[node.ToVector2()];
            if (goList == null)
                continue;
            foreach (ObjectData info in goList)
                if (info.type == ObjectType.Enemy && info.gameObject.activeInHierarchy)
                    return false;
        }
        return true;
    }
}