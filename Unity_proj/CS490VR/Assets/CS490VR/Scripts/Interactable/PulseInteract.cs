using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulseInteract : Interactable
{
    public BlockManager bm;
    public PowerableLoader pl;

    public override void Interact()
    {
        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (!pl) pl = GetComponent<PowerableLoader>();

        bm.ClientUpdateBlock(pl.data.id, new { data = new { data = new { start_tick = bm.tick, powered = true } } });
    }
}
