using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLoader : MonoBehaviour, IDataLoader
{
    public VoxelData data;

    public void Load()
    {
        data.UpdateObject(gameObject);
    }

    public void SetData(object o)
    {
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(o), data);
    }

    public object GetData()
    {
        return data;
    }
}
