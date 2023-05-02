using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDataLoader
{
    // Load new changes into the environment (when created or updated)
    public virtual void Load() {}

    // Unload old changes into the environment (when updated or destroyed)
    public virtual void Unload() {}

    // Get this loader's BlockData object
    public virtual BlockData GetData() { return null; }

    // Get this loader's default state
    public virtual BlockData GetDefaultState() { return new BlockData(); }

    // Update this loader's state based on the tick
    public virtual void UpdateFromTick(BlockData target, BlockManager bm) { }
}
