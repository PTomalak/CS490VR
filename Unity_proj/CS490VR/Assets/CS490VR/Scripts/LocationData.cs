using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocationData : Data
{
    // Stores location data for blocks.
    public Vector3Int data;

    public override string Name()
    {
        return "location";
    }

    public override void FromString(string input)
    {
        base.FromString(input);
    }

    public override bool EqualData(Data other)
    {
        if (other is LocationData l)
        {
            return l.data.Equals(this.data);
        }
        return false;
    }

    public override void Load()
    {
        transform.position = data;

        //GetComponent<Renderer>().material = (POWERED) ? POWERED_TEXTURE : UNPOWERED_TEXTURE;
    }
}
