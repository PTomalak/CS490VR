using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatableLoader : MonoBehaviour, IDataLoader
{
    [SerializeField]
    public RotatableData data;

    // Components
    #region components
    protected BlockManager bm;
    #endregion

    public virtual void Load()
    {
        data.UpdateObject(gameObject);
    }

    public virtual void Unload()
    {
    }

    public BlockData GetData()
    {
        return data;
    }
}
