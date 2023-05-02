using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseLatchLoader : DiodeLoader
{
    public override BlockData GetDefaultState()
    {
        AdditionalData.PulseLatch add = new AdditionalData.PulseLatch();
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, add);
        return new_data;
    }
}
