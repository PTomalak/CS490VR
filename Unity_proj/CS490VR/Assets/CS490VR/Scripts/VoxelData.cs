using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VoxelData : MonoBehaviour
{
    // Quick and dirty class to store data for a single voxel
    // NOTE: only data that affects how a block is rendered matters!

    // Whether this block is powered
    public bool POWERED = false;
    public Material POWERED_TEXTURE;
    public Material UNPOWERED_TEXTURE;

    // The rotation of this block
    // Stored as (degrees around x axis, degrees around y axis, degrees around z axis)
    public Vector3 ROTATION = Vector3.zero;

    // Loads (or reloads) this block's data
    public void Load()
    {
        GetComponent<Renderer>().material = (POWERED) ? POWERED_TEXTURE : UNPOWERED_TEXTURE;
        transform.rotation = Quaternion.Euler(ROTATION);
    }

    private void Start()
    {
        Load();
    }
}
