using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataLoader
{
    public virtual void Load() {}

    public virtual void SetData(BlockData o) {}

    public virtual BlockData GetData() { return null; }
}
