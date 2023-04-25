using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelData : IData
{
    // Data values shared by all individual voxels

    // ID
    public int id;

    // Location
    public Vector3Int position;

    public virtual void UpdateObject(GameObject gameObject)
    {
        gameObject.transform.localPosition = position;
    }

    public override IData GetDefaultState()
    {
        VoxelData vd = new VoxelData();
        vd.id = 0;
        vd.position = new Vector3Int(0, 0, 0);
        return vd;
    }
}

