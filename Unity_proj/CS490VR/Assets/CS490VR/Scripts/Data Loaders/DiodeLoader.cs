using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiodeLoader : RotatableLoader
{
    public override void Load()
    {
        base.Load();
        // Move slightly to align the blocks onto the grid
        transform.localPosition += RotatableData.GetRotation(data.rotation) * new Vector3(0, 0, 0.5f);

        // Add wire connections at input and output
        ModifyConnection(new Vector3Int(0, 0, 1), true);
        ModifyConnection(new Vector3Int(0, 0, 0), true);
    }

    public override void Unload()
    {
        base.Unload();

        // Remove wire connections at input and output
        ModifyConnection(new Vector3Int(0, 0, 1), false);
        ModifyConnection(new Vector3Int(0, 0, 0), false);
    }

    private void ModifyConnection(Vector3Int a, bool add)
    {
        Vector3Int pos = new Vector3Int(data.position[0], data.position[1], data.position[2]);
        Quaternion rot = RotatableData.GetRotation(data.rotation);
        Vector3Int da = pos + Vector3Int.RoundToInt(rot * a);

        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (add)
        {
            bm.wm.AddConnection(da);
        }
        else
        {
            bm.wm.RemoveConnection(da);
        }
    }
}
