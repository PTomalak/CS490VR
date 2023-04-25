using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WireManager : MonoBehaviour
{
    // List of wire IDs
    public List<int> wire_ids = new List<int>();

    // 3D Wire map (maps [x,y,z] to {id, powered})
    [HideInInspector]
    public Dictionary<int[], WireMapObject> wire_map = new Dictionary<int[], WireMapObject>(new WireCompare());
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
    public class WireMapObject
    {
        public int id;
        public bool powered;

        public WireMapObject(int id, bool powered)
        {
            this.id = id;
            this.powered = powered;
        }
    }

    // Connection locations (areas that adjacent wires should connect to)
    [HideInInspector]
    public List<int[]> connections = new List<int[]>();

    // Adds a wire
    public bool AddWire(int x, int y, int z, int id, bool powered)
    {
        if (wire_ids.Contains(id)) return false;

        int[] coords = new int[3] { x, y, z };
        if (wire_map.ContainsKey(coords)) return false;

        wire_ids.Add(id);
        wire_map.Add(coords, new WireMapObject(id, powered));
        return true;
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
        //GetComponent<WireMesh>().ReloadMesh();
    }
}
