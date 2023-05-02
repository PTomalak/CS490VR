using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class PowerableLoader : BlockLoader
{
    public PoweredTextures powerableTextures;

    // Components
    #region components
    protected BlockManager bm;
    #endregion

    public override void Load()
    {
        // Set position
        base.Load();

        // Set texture
        AdditionalData.Powered pow = AdditionalData.GetPoweredData(data.data.data);
        gameObject.GetComponent<Renderer>().material = (pow.powered) ? powerableTextures.ON_MATERIAL : powerableTextures.OFF_MATERIAL;
    }

    public override BlockData GetDefaultState()
    {
        AdditionalData.Powered pow = new AdditionalData.Powered();
        BlockData new_data = new BlockData();
        new_data.SetAdditionalData(block, pow);
        return new_data;
    }
}
