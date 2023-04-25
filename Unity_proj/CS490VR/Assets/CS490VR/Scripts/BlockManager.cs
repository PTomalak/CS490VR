using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using static JSONParser;

public class BlockManager : MonoBehaviour
{
    #region components
    JSONParser jp;
    TCPClient tc;
    #endregion

    #region fields
    // Stores all blocks (not voxels) by ID
    public Dictionary<int, GameObject> blocks = new Dictionary<int, GameObject>();

    // Queue to transfer block placing commands from background thread to maing thread
    public Queue<string> actions = new Queue<string>();

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
    #endregion



    ///// FROM SERVER FUNCTIONS /////
    // Places a block into the world
    public BMResponse PlaceBlock(PlaceData placeData)
    {
        string message = "ok";

        // Check if our dictionary already contains the block
        // and remove that block
        int id = (placeData.data).id;
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

            // Obtain the respective prefab's block loader
            IDataLoader bl = newObject.GetComponent<IDataLoader>();
            if (bl == null)
            {
                Destroy(newObject);
                return new BMResponse(false, "Block had no Data Loader");
            }

            // Update/load unity block data
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(placeData.data), bl.GetData());
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

        // Update block loader
        IDataLoader bl = target.GetComponent<IDataLoader>();
        if (bl == null) return new BMResponse(false, "Block had no Data Loader");
        JsonConvert.PopulateObject(JsonConvert.SerializeObject(updateData.data), bl.GetData());
        bl.Load();

        return new BMResponse(true, "ok");
    }


    
    ///// CLIENT SENDING METHODS /////
    
    // Used by client to place a block at a given local XYZ position
    public BMResponse ClientPlaceBlock(string block, int x, int y, int z)
    {
        BlockPrefabEntry e = blockPrefabs.Find((v) => v.block == block);
        if (!blockPrefabs.Contains(e)) return new BMResponse(false, "No Prefab for: " + block);

        GameObject prefab = e.prefab;
        IDataLoader dl = prefab.GetComponent<IDataLoader>();
        if (dl == null) return new BMResponse(false, "Block had no Data Loader");

        BlockData d = dl.GetData();
        if (d == null) return new BMResponse(false, "Block had no Data");

        // Obtaine block default data to pass to server
        BlockData data = d.GetDefaultState();

        // Change position to the requested position and set ID to zero
        data.id = 0;
        data.position = new int[3] { x, y, z };

        // Prepare and send request
        PlaceData placeData = new PlaceData(block, data);

        if (!jp) return new BMResponse(false, "No JSONParser");
        
        jp.SendPlaceRequest(placeData);

        return new BMResponse(true, "ok");
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
            BlockData v = new BlockData();
            v.position = new int[3] { x, y, z };
            v.id = x*100+y*10+z;
            d = new PlaceData(voxel, v);
        } else
        {
            PowerableData p = new PowerableData();
            p.position = new int[3] { x, y, z };
            p.id = x * 100 + y * 10 + z;
            p.powered = false;
            d = new PlaceData(voxel, p);
        }
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
        int id = x * 100 + y * 10 + z;
        PowerableData p = new PowerableData();
        p.position = new int[3] { x, y, z };
        p.id = id;
        p.powered = true;
        UpdateData up = new UpdateData(id, "pixel", p);

        UpdateBlock(up);
    }

    private void Awake()
    {
        tc = GetComponent<TCPClient>();
        jp = GetComponent<JSONParser>();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(PlaceBlockTest2());

        // For now, call the temporary tests again
        PlaceBlockTest(0, 0, 3, "pixel");
        //PlaceBlockTest(0, 0, 1, "pixel");
        //PlaceBlockTest(0, 0, 2, "pixel");
        //PlaceBlockTest(0, 0, 3, "pixel");
        //RemoveBlockTest(0, 0, 2);
        UpdateBlockTest(0, 0, 3);
    }

    private void FixedUpdate()
    {
        // Perform up to a maximum number of queued operations per frame
        int ops = 0;
        while(actions.Count > 0 && ops < 50)
        {
            jp.PerformJson(actions.Dequeue());
            ops += 1;
        }
    }

    IEnumerator PlaceBlockTest2()
    {
        yield return new WaitForSeconds(5);

        ClientPlaceBlock("block", 1, 2, 3);
    }
}
