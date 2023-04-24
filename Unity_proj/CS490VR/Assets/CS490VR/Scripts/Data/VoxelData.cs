using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class VoxelData
{
    // Data values shared by all individual voxels

    // ID
    public string id;

    // Location
    public Vector3Int position;

    public virtual void UpdateObject(GameObject gameObject)
    {
        gameObject.transform.localPosition = position;
    }
}


