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
}
