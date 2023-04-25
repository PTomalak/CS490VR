using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using static JSONParser;

public class BlockManager : MonoBehaviour
{
    JSONParser jp;

    // Stores all blocks (not voxels) by ID
    public Dictionary<int, GameObject> blocks = new Dictionary<int, GameObject>();



    // Interact with in editor to add string / gameObject pairs
    // The string is the block's name, the gameObject is that block's prefab
    [SerializeField]
    public List<BlockPrefabEntry> blockPrefabs;

    [System.Serializable]
    public struct BlockPrefabEntry
    {
        public string block;
        public GameObject prefab;
    }



    ///// FROM SERVER FUNCTIONS /////
    // Places a block into the world
    public BMResponse PlaceBlock(PlaceData placeData)
    {
        string message = "ok";

        // Check if our dictionary already contains the block
        // and remove that block
        int id = ((VoxelData)placeData.data).id;
        if (blocks.ContainsKey(id))
        {
            RemoveBlock(new RemoveData(id));
            message = "Removed Duplicate Block";
        }

        // Instantiate this voxel if we have a prefab for it
        BlockPrefabEntry e = blockPrefabs.Find((v) => v.block == placeData.block);
        if (blockPrefabs.Contains(e))
        {
            // Spawn the object in the world and make its name readable in the editor
            GameObject newObject = Instantiate(e.prefab, transform);
            if (!newObject) return new BMResponse(false, "Unity Object Creation Failed");
            newObject.name = id.ToString();

            // Update and load the data
            IDataLoader bl = newObject.GetComponent<IDataLoader>();
            if (bl == null)
            {
                Destroy(newObject);
                return new BMResponse(false, "Block had no Data Loader");
            }
            bl.SetData(placeData.data);
            bl.Load();

            // Update our block dictionary
            blocks.Add(id, newObject);
        } else
        {
            return new BMResponse(false, "No Prefab for: "+placeData.block);
        }

        return new BMResponse(true, message);
    }

    // Removes a block from the world
    public BMResponse RemoveBlock(RemoveData removeData)
    {
        // Check if we have that block in our system
        GameObject target = blocks[removeData.id];
        if (!target) return new BMResponse(false, "Block Not Found");

        // Destroy gameobject
        blocks.Remove(removeData.id);
        Destroy(target);

        return new BMResponse(true, "ok");
    }

    // Updates the data of a block
    public BMResponse UpdateBlock(UpdateData updateData)
    {
        // Check if we have that block in our system
        GameObject target = blocks[updateData.id];
        if (!target) return new BMResponse(false, "Block Not Found");

        IDataLoader bl = target.GetComponent<IDataLoader>();
        if (bl == null) return new BMResponse(false, "Block had no Data Loader");
        bl.SetData(updateData.data);
        bl.Load();

        return new BMResponse(true, "ok");
    }


    
    ///// CLIENT SENDING METHODS /////
    
    // Used by client to place a block at a given local XYZ position
    public void ClientPlaceBlock(string block, int x, int y, int z)
    {
        BlockPrefabEntry e = blockPrefabs.Find((v) => v.block == block);
        if (!blockPrefabs.Contains(e)) return;

        GameObject prefab = e.prefab;
        IDataLoader dl = prefab.GetComponent<IDataLoader>();
        if (dl == null) return;

        IData d = dl.GetData();
        if (d == null) return;

        // Obtaine block default data to pass to server
        object data = d.getDefaultState();

        // Change position to the requested positin and set ID to zero
        VoxelData vd = new VoxelData();
        vd.id = 0;
        vd.position = new Vector3Int(x, y, z);
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(vd), data);

        // Prepare and send request
        PlaceData placeData = new PlaceData(block, data);
        if (!jp) jp = GetComponent<JSONParser>();
        if (!jp) return;
        jp.SendRequest("place", placeData);
    }

    // Global coordinate version of ClientPlaceBlock
    public void ClientPlaceBlockGlobal(string block, float x, float y, float z)
    {
        Vector3 lpos = transform.InverseTransformPoint(x, y, z);
        Vector3Int p = Vector3Int.FloorToInt(lpos);

        ClientPlaceBlock(block, p.x, p.y, p.z);
    }


    ///// TEMPORARY TESTS /////
    public void PlaceBlockTest(int x, int y, int z, string voxel="block")
    {
        PlaceData d;
        if (voxel == "block")
        {
            VoxelData v = new VoxelData();
            v.position = new Vector3Int(x, y, z);
            v.id = x*100+y*10+z;
            d = new PlaceData(voxel, v);
        } else
        {
            PowerableData p = new PowerableData();
            p.position = new Vector3Int(x, y, z);
            p.id = x * 100 + y * 10 + z;
            p.powered = false;
            d = new PlaceData(voxel, p);
        }
        Debug.Log(JsonUtility.ToJson(d.data));
        PlaceBlock(d);
    }

    public void RemoveBlockTest(int x, int y, int z)
    {
        int id = x * 100 + y * 10 + z;
        RemoveData rm = new RemoveData(id);
        RemoveBlock(rm);
    }

    public void UpdateBlockTest(int x, int y, int z)
    {
        Vector3Int v = new Vector3Int(x, y, z);
        int id = x * 100 + y * 10 + z;
        PowerableData p = new PowerableData();
        p.position = v;
        p.id = id;
        p.powered = true;
        UpdateData up = new UpdateData(id, p);

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
