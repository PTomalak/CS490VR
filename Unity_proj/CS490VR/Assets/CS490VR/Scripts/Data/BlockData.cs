using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class BlockData
{
    // Data values shared by all blocks

    // Block (type of block that this block is
    public string block = "";

    // ID (UUID of this exact block instance)
    public int id = 0;

    // Location (multivoxels can interpret this value as they choose, usually the center)
    public Vector3Int position = new Vector3Int();


    /// METHODS TO OVERRIDE ///
    public virtual void UpdateObject(GameObject gameObject)
    {
        gameObject.transform.localPosition = position;
    }

    public virtual BlockData GetDefaultState()
    {
        return new BlockData();
    }
}

