using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLoader : MonoBehaviour, IDataLoader
{
    public BlockData data;

    public void Load()
    {
        data.UpdateObject(gameObject);
    }

    public BlockData GetData()
    {
        return data;
    }
}
