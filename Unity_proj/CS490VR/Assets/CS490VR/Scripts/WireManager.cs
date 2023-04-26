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
    public bool hasChanged;

    // List of wire IDs
    //public List<int> wire_ids = new List<int>();
    public Dictionary<int, int[]> wire_ids = new Dictionary<int, int[]>();

    // 3D Wire map (maps [x,y,z] to {id, powered})
    [HideInInspector]
    public Dictionary<int[], WireData> wire_map = new Dictionary<int[], WireData>(new WireCompare());
    public class WireCompare : IEqualityComparer<int[]>
    {
        bool IEqualityComparer<int[]>.Equals(int[] x, int[] y)
        {
            return x[0] == y[0] && x[1] == y[1] && x[2] == y[2];
        }

        int IEqualityComparer<int[]>.GetHashCode(int[] obj)
        {
            return new Vector3Int(obj[0], obj[1], obj[2]).GetHashCode();
        }
    }
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
    public List<int[]> connections = new List<int[]>();

    // Adds a wire to the grid
    public bool AddWire(int x, int y, int z, int id, bool powered)
    {
        if (wire_ids.ContainsKey(id)) return false;

        int[] coords = new int[3] { x, y, z };
        if (wire_map.ContainsKey(coords)) return false;

        wire_ids.Add(id, coords);
        wire_map.Add(coords, new WireData(id, powered));
        hasChanged = true;
        return true;
    }

    public bool PlaceWire(PowerableData data)
    {
        int[] p = data.position;
        return AddWire(p[0], p[1], p[2], data.id, data.powered);
    }

    public bool UpdateWire(int id, PowerableData data)
    {
        if (!wire_ids.ContainsKey(id)) return false;

        // Determine if we need to move the wire (new position != old position)
        int[] coords = wire_ids[id];
        bool eq = true;
        for (int i = 0; i < coords.Length; i++)
        {
            if (coords[i] != data.position[i])
            {
                eq = false;
                break;
            }
        }

        if (!wire_map.ContainsKey(coords)) return false;

        if (eq)
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
        int[] coords = wire_ids[id];
        wire_ids.Remove(id);
        wire_map.Remove(coords);

        hasChanged = true;
        return true;
    }

    public void ReloadMesh()
    {
        mesh.ReloadMesh();
    }

    private void Awake()
    {
        mesh = GetComponent<WireMesh>();
    }

    private void Start()
    {
        //AddWire(0, 0, 0, 0, false);
        //AddWire(0, 1, 0, 1, false);
        //AddWire(0, 1, 1, 2, false);
        //AddWire(1, 1, 1, 3, false);
        //AddWire(-1, 0, 0, 4, false);
        //AddWire(2, 2, 2, 5, true);
        //AddWire(3, 2, 2, 6, true);
        //AddWire(3, 2, 3, 7, true);
        //AddWire(3, 3, 3, 8, true);
        //connections.Add(new int[3] { 3, 4, 3 });
        //
    }
}
