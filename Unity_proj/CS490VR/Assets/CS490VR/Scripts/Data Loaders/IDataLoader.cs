using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataLoader
{
    // Load new changes into the environment (when created or updated)
    public virtual void Load() {}

    // Unload old changes into the environment (when updated or destroyed)
    public virtual void Unload() {}

    // Set this loader's BlockData
    public virtual void SetData(BlockData o) {}

    // Get this loader's BlockData
    public virtual BlockData GetData() { return null; }
}
