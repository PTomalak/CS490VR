using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static JSONParser;

public class BlockManager : MonoBehaviour
{
    // Stores all blocks (not voxels) by ID
    public Dictionary<string, GameObject> blocks = new Dictionary<string, GameObject>();



    // Interact with in editor to add string / gameObject pairs
    // The string is the block's name, the gameObject is that block's prefab
    [SerializeField]
    public List<VoxelPrefabEntry> voxelPrefabs;

    [System.Serializable]
    public struct VoxelPrefabEntry
    {
        public string voxel;
        public GameObject prefab;
    }



    // Places a block into the world
    public void PlaceBlock(PlaceData placeData)
    {
        // Check if our dictionary already contains the block
        // and remove that block
        string id = ((VoxelData)placeData.data).id;
        if (blocks.ContainsKey(id))
        {
            RemoveData rm = new RemoveData();
            rm.id = id;
            RemoveBlock(rm);
        }

        // Instantiate this voxel if we have a prefab for it
        VoxelPrefabEntry e = voxelPrefabs.Find((v) => v.voxel == placeData.voxel);
        if (voxelPrefabs.Contains(e))
        {
            // Spawn the object in the world and make its name readable in the editor
            GameObject newObject = Instantiate(e.prefab, transform);
            if (!newObject) return;
            newObject.name = id;

            // Update and load the data
            IDataLoader bl = newObject.GetComponent<IDataLoader>();
            if (bl == null) return;
            bl.SetData(placeData.data);
            bl.Load();

            // Update our block dictionary
            blocks.Add(id, newObject);
        }
    }

    // Removes a block from the world
    public void RemoveBlock(RemoveData removeData)
    {
        // Check if we have that block in our system
        GameObject target = blocks[removeData.id];
        if (!target) return;

        // Destroy gameobject
        blocks.Remove(removeData.id);
        Destroy(target);
    }

    // Updates the data of a block
    public void UpdateBlock(UpdateData updateData)
    {
        // Check if we have that block in our system
        GameObject target = blocks[updateData.id];
        if (!target) return;

        IDataLoader bl = target.GetComponent<IDataLoader>();
        if (bl == null) return;
        bl.SetData(updateData.data);
        bl.Load();
    }



    ///// TEMPORARY TESTS /////
    public void PlaceBlockTest(int x, int y, int z, string voxel="block")
    {
        PlaceData d = new PlaceData();
        d.voxel = voxel;
        if (voxel == "block")
        {
            VoxelData v = new VoxelData();
            v.position = new Vector3Int(x, y, z);
            v.id = v.position.ToString();
            d.data = v;
        } else
        {
            PowerableData p = new PowerableData();
            p.position = new Vector3Int(x, y, z);
            p.id = p.position.ToString();
            p.powered = false;
            d.data = p;
        }
        Debug.Log(JsonUtility.ToJson(d.data));
        PlaceBlock(d);
    }

    public void RemoveBlockTest(int x, int y, int z)
    {
        Vector3Int v = new Vector3Int(x, y, z);
        RemoveData rm = new RemoveData();
        rm.id = v.ToString();
        RemoveBlock(rm);
    }

    public void UpdateBlockTest(int x, int y, int z)
    {
        Vector3Int v = new Vector3Int(x, y, z);
        UpdateData up = new UpdateData();
        up.id = v.ToString();
        PowerableData p = new PowerableData();
        p.position = v;
        p.id = up.id;
        p.powered = true;
        up.data = p;

        UpdateBlock(up);
    }

    // Start is called before the first frame update
    void Start()
    {
        // For now, call the temporary tests again
        PlaceBlockTest(0, 0, 0);
        PlaceBlockTest(0, 0, 1, "pixel");
        PlaceBlockTest(0, 0, 2, "pixel");
        PlaceBlockTest(0, 0, 3, "pixel");
        RemoveBlockTest(0, 0, 2);
        UpdateBlockTest(0, 0, 3);
    }
}
