using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClockLoader : PowerVoxelLoader
{
    public override BlockData GetDefaultState()
    {
        AdditionalData.Clock add = new AdditionalData.Clock();
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, add);
        return new_data;
    }
    public override void UpdateFromTick(BlockData target, BlockManager bm)
    {
        AdditionalData.Clock add = new AdditionalData.Clock();
        add.start_tick = bm.tick;

        target.SetAdditionalData(block, add);
    }
}
