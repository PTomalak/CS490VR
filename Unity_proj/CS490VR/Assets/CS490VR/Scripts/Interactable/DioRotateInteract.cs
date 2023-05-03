using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DioRotateInteract : Interactable
{
    public static List<string> STATES = new List<string>()
    {
        "FORWARD",
        "RIGHT",
        "BACKWARD",
        "LEFT",
        "UP",
        "DOWN"
    };

    public BlockManager bm;
    public LogicGateLoader loader;

    public override void Interact()
    {
        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (!loader) loader = GetComponent<LogicGateLoader>();

        string current_rot = loader.GetData().rotation;
        int index = STATES.IndexOf(current_rot);
        int new_index = index + 1 % STATES.Count;
        bm.jp.SendUpdateRequest(new { id = loader.data.id, rotation = STATES[new_index] });
    }
}
