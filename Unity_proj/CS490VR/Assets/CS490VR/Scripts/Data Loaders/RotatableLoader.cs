using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatableLoader : PowerableLoader
{
    public override void Load()
    {
        base.Load();
        gameObject.transform.rotation = GetRotation(data.rotation);
    }

    public static Quaternion GetRotation(string rot)
    {
        return rot switch
        {
            "RIGHT" => Quaternion.AngleAxis(90, Vector3.up),
            "LEFT" => Quaternion.AngleAxis(270, Vector3.up),
            "FORWARD" => Quaternion.AngleAxis(0, Vector3.up),
            "BACKWARD" => Quaternion.AngleAxis(180, Vector3.up),
            "UPWARD" => Quaternion.AngleAxis(270, Vector3.right),
            "DOWNWARD" => Quaternion.AngleAxis(90, Vector3.right),
            _ => Quaternion.identity
        };
    }

    protected void ModifyConnection(Vector3Int a, bool add)
    {
        Vector3Int pos = data.position.GetVector();
        Quaternion rot = GetRotation(data.rotation);
        Vector3Int da = pos + Vector3Int.RoundToInt(rot * a);

        if (!bm) bm = GetComponentInParent<BlockManager>();
        if (add)
        {
            bm.wm.AddConnection(da);
        }
        else
        {
            bm.wm.RemoveConnection(da);
        }
    }
}
