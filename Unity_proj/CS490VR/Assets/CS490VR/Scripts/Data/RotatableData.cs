using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RotatableData : PowerableData
{
    public enum BlockRotation
    {
        FORWARD,        //Facing +z
        BACKWARD,       //Facing -z
        UP,             //Facing +y
        DOWN,           //Facing -y
        RIGHT,          //Facing +x
        LEFT,           //Facing -x
    }

    public BlockRotation rotation;

    public override void UpdateObject(GameObject gameObject)
    {
        base.UpdateObject(gameObject);
        gameObject.transform.rotation = GetRotation(rotation);
    }

    public override BlockData GetDefaultState()
    {
        return new RotatableData();
    }

    public static Quaternion GetRotation(BlockRotation rot)
    {
        return rot switch
        {
            BlockRotation.RIGHT => Quaternion.AngleAxis(90, Vector3.up),
            BlockRotation.LEFT => Quaternion.AngleAxis(270, Vector3.up),
            BlockRotation.FORWARD => Quaternion.AngleAxis(0, Vector3.up),
            BlockRotation.BACKWARD => Quaternion.AngleAxis(180, Vector3.up),
            BlockRotation.UP => Quaternion.AngleAxis(270, Vector3.right),
            BlockRotation.DOWN => Quaternion.AngleAxis(90, Vector3.right),
            _ => Quaternion.identity
        };
    }
}
