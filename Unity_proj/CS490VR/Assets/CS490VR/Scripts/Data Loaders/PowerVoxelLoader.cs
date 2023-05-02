using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerVoxelLoader : PowerableLoader
{
    public override void Load()
    {
        base.Load();
        // Add wire connection at our position
        if (!bm) bm = GetComponentInParent<BlockManager>();
        bm.wm.AddConnection(data.position.GetVector());
    }

    public override void Unload()
    {
        // Remove wire connection at our position
        if (!bm) bm = GetComponentInParent<BlockManager>();
        bm.wm.RemoveConnection(data.position.GetVector());
    }
}
