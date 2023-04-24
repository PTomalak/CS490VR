using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireData : MonoBehaviour
{
    // Class to store wire-specific data

    // The size of the rectangle containing all of the wires
    public Vector3Int size;

    // A 3D array of booleans representing whether there is a wire at each point
    public bool[,,] wires;

    // A 3D array of booleans representing coordinates connected to this wire network
    public bool[,,] connections;

    public void Load()
    {
        Initialize(3, 3, 3);

        // Demo code to show what a wire looks like
        wires[1, 1, 1] = true;
        wires[1, 1, 0] = true;
        wires[1, 0, 0] = true;
        wires[1, 1, 2] = true;
        wires[2, 1, 2] = true;
        wires[2, 1, 1] = true;
        wires[1, 0, 2] = true;
        connections[0, 0, 2] = true;

        GetComponent<WireMesh>().ReloadMesh();
    }

    // Initializes wire connection
    public void Initialize(int x, int y, int z)
    {
        size = new Vector3Int(x, y, z);
        wires = new bool[size.x, size.y, size.z];
        connections = new bool[size.x, size.y, size.z];
    }
}
