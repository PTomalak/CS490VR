using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockLoader : MonoBehaviour, IDataLoader
{
    public BlockData data;
    public string block;

    public virtual void Load()
    {
        transform.localPosition = data.position.GetVector();
    }

    public virtual void Unload()
    {
    }

    public BlockData GetData()
    {
        return data;
    }

    public virtual BlockData GetDefaultState()
    {
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, new { });
        return new_data;
    }
}
