using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleLatchLoader : DiodeLoader
{
    public override BlockData GetDefaultState()
    {
        AdditionalData.Memory mem = new AdditionalData.Memory();
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, mem);
        return new_data;
    }
}
