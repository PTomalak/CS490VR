using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseLoader : PowerVoxelLoader
{
    public override BlockData GetDefaultState()
    {
        AdditionalData.Pulse add = new AdditionalData.Pulse();
        add.start_tick = 0;
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, add);
        return new_data;
    }
}
