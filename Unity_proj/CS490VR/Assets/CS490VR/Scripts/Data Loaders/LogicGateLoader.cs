using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogicGateLoader : RotatableLoader
{
    public override void Load()
    {
        base.Load();

        // Add wire connections at output and both inputs
        ModifyConnection(new Vector3Int(0, 0, 1), true);
        ModifyConnection(new Vector3Int(-1, 0, -1), true);
        ModifyConnection(new Vector3Int(1, 0, -1), true);
    }

    public override void Unload()
    {
        base.Unload();

        // Remove wire connections at output and both inputs
        ModifyConnection(new Vector3Int(0, 0, 1), false);
        ModifyConnection(new Vector3Int(-1, 0, -1), false);
        ModifyConnection(new Vector3Int(1, 0, -1), false);
    }

    private void ModifyConnection(Vector3Int a, bool add)
    {
        Vector3Int pos = new Vector3Int(data.position[0], data.position[1], data.position[2]);
        Quaternion rot = RotatableData.GetRotation(data.rotation);
        Vector3Int da = pos + Vector3Int.RoundToInt(rot * a);

        if (!bm) bm = GetComponentInParent<BlockManager>();
        int[] target = new int[3] { da.x, da.y, da.z };
        Debug.Log(target);
        if (add)
        {
            bm.wm.AddConnection(target);
        } else
        {
            bm.wm.RemoveConnection(target);
        }
    }
}
