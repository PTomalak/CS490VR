using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

[System.Serializable]
public class BlockData
{
    // Data values shared by all blocks

    // ID (UUID of this exact block instance)
    public int id = 0;

    // Location (multivoxels can interpret this value as they choose, usually the center)
    [SerializeField]
    public BlockPosition position = new BlockPosition();

    // Rotation (most ignore this field)
    public string rotation = "FORWARD";

    // Additional data, used for storing stuff like the block name
    [SerializeField]
    public AdditionalData data;

    public void SetAdditionalData(string block, object data)
    {
        this.data = new AdditionalData(block, data);
    }

    // Block position class
    [System.Serializable]
    public class BlockPosition
    {
        public int x;
        public int y;
        public int z;

        public BlockPosition(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public BlockPosition() : this(0, 0, 0) { }

        public void Set(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public void Set(Vector3Int v)
        {
            Set(v.x, v.y, v.z);
        }

        public bool Compare(Vector3Int v)
        {
            return (x == v.x) && (y == v.y) && (z == v.z);
        }

        public Vector3Int GetVector()
        {
            return new Vector3Int(x, y, z);
        }
    }
}

