using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerableLoader : MonoBehaviour, IDataLoader
{
    public PowerableData data;

    // Components
    #region components
    BlockManager bm;
    #endregion

    public void Load()
    {
        data.UpdateObject(gameObject);

        // Add wire connection at our position
        if (!bm) bm = GetComponentInParent<BlockManager>();
        bm.wm.AddConnection(data.position);
    }

    public void Unload()
    {
        // Remove wire connection at our position
        if (!bm) bm = GetComponentInParent<BlockManager>();
        bm.wm.RemoveConnection(data.position);
    }

    public BlockData GetData()
    {
        return data;
    }
}
