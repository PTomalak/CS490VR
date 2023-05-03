using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleInteract : Interactable
{
    public BlockManager bm;
    public PowerableLoader pl;

    public override void Interact()
    {
        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (!pl) pl = GetComponent<PowerableLoader>();

        AdditionalData.Powered pow = AdditionalData.GetPoweredData(pl.data.data);
        bm.ClientUpdateBlock(pl.data.id, new { data = new { data = new { powered = !pow.powered } } });
    }
}
