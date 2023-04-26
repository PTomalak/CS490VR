using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PowerableData : BlockData
{
    // Whether the block is on or off
    public bool powered = false;

    // Caching GetComponent calls (not serialized)
    private PowerableLoader pl;

    public override void UpdateObject(GameObject gameObject)
    {
        base.UpdateObject(gameObject);
        if (!pl) pl = gameObject.GetComponent<PowerableLoader>();
        gameObject.GetComponent<Renderer>().material = (powered) ? pl.ON_MATERIAL : pl.OFF_MATERIAL;
    }

    public override BlockData GetDefaultState()
    {
        return new PowerableData();
    }
}