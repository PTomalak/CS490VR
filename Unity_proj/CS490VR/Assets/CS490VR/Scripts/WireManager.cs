using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static JSONParser;

public class WireManager : MonoBehaviour
{
    #region components
    WireMesh mesh;
    #endregion

    // Used to update mesh
    [HideInInspector]
    public bool hasChanged;

    // List of wire IDs
    //public List<int> wire_ids = new List<int>();
    public Dictionary<int, Vector3Int> wire_ids = new Dictionary<int, Vector3Int>();

    // 3D Wire map (maps [x,y,z] to {id, powered})
    [HideInInspector]
    public Dictionary<Vector3Int, WireData> wire_map = new Dictionary<Vector3Int, WireData>();
    public class WireData
    {
        public int id;
        public bool powered;

        public WireData(int id, bool powered)
        {
            this.id = id;
            this.powered = powered;
        }
    }

    // Connection locations (areas that adjacent wires should connect to)
    [HideInInspector]
    public List<Vector3Int> connections = new List<Vector3Int>();

    // Adds a wire to the grid
    public bool AddWire(int x, int y, int z, int id, bool powered)
    {
        if (wire_ids.ContainsKey(id)) return false;

        Vector3Int coords = new Vector3Int(x, y, z);
        if (wire_map.ContainsKey(coords)) return false;

        wire_ids.Add(id, coords);
        wire_map.Add(coords, new WireData(id, powered));
        hasChanged = true;
        return true;
    }

    public bool PlaceWire(PowerableData data)
    {
        Vector3Int p = data.position;
        return AddWire(p.x, p.y, p.z, data.id, data.powered);
    }

    public bool UpdateWire(int id, PowerableData data)
    {
        if (!wire_ids.ContainsKey(id)) return false;

        // Determine if we need to move the wire (new position != old position)
        Vector3Int coords = wire_ids[id];

        if (!wire_map.ContainsKey(coords)) return false;

        if (coords == data.position)
        {
            wire_map[coords].powered = data.powered;
        } else
        {
            wire_ids[id] = data.position;
            wire_map.Remove(coords);
            wire_map.Add(data.position, new WireData(id, data.powered));
        }

        hasChanged = true;
        return true;
    }

    public bool RemoveWire(int id)
    {
        if (!wire_ids.ContainsKey(id)) return false;
        Vector3Int coords = wire_ids[id];
        wire_ids.Remove(id);
        wire_map.Remove(coords);

        hasChanged = true;
        return true;
    }

    public void AddConnection(Vector3Int c)
    {
        connections.Add(c);
        hasChanged = true;
    }

    public void RemoveConnection(Vector3Int c)
    {
        connections.Remove(c);
        hasChanged = true;
    }

    public void ReloadMesh()
    {
        mesh.ReloadMesh();
    }

    private void Awake()
    {
        mesh = GetComponent<WireMesh>();
    }
}
