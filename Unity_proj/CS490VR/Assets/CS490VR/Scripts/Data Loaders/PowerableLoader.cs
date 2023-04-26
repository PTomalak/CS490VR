using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerableLoader : MonoBehaviour, IDataLoader
{
    // Two materials (textures) for whether it is on or off
    [SerializeField]
    public Material ON_MATERIAL;
    [SerializeField]
    public Material OFF_MATERIAL;

    // Store the powerable data (which references this component for textures)
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
        bm.wm.connections.Add(data.position);
    }

    public void Unload()
    {
        // Remove wire connection at our position
        if (!bm) bm = GetComponentInParent<BlockManager>();
        bm.wm.connections.Remove(data.position);
    }

    public BlockData GetData()
    {
        return data;
    }
}
