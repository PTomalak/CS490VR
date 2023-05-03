using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DioRotateInteract : AltInteractable
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
    public DiodeLoader loader;

    public override void Interact()
    {
        Rotate(1);
    }

    public override void AltInteract()
    {
        Rotate(-1);
    }

    public void Rotate(int amount)
    {
        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (!loader) loader = GetComponent<DiodeLoader>();

        string current_rot = loader.GetData().rotation;
        int index = STATES.IndexOf(current_rot);
        int new_index = index + amount % STATES.Count;
        bm.jp.SendUpdateRequest(new { id = loader.data.id, rotation = STATES[new_index] });
    }
}
