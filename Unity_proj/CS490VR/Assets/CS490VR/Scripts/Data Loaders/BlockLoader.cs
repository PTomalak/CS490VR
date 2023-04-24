using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLoader : MonoBehaviour, IDataLoader
{
    public VoxelData data;

    public virtual void Load()
    {
        data.UpdateObject(gameObject);
    }

    public virtual void SetData(object o)
    {
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(o), data);
    }
}
