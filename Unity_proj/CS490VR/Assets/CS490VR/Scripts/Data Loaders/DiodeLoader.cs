using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiodeLoader : RotatableLoader
{
    public override void Load()
    {
        base.Load();
        // Move slightly to align the blocks onto the grid
        transform.localPosition += GetRotation(data.rotation) * new Vector3(0, 0, 0.5f);

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
}
