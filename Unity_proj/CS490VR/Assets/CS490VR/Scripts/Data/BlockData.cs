using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class BlockData
{
    // Data values shared by all blocks

    // ID
    public int id;

    // Location (multivoxels can interpret this value as they choose, usually the center)
    public int[] position = new int[3];


    /// METHODS TO OVERRIDE ///
    public virtual void UpdateObject(GameObject gameObject)
    {
        gameObject.transform.localPosition = new Vector3Int(position[0], position[1], position[2]);
    }

    public virtual BlockData GetDefaultState()
    {
        BlockData vd = new BlockData();
        vd.id = 0;
        vd.position = new int[3] {0,0,0};
        return vd;
    }
}

