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
    WireManager wm;
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

        string blockJson = JsonConvert.SerializeObject(placeData.data);
        BlockData basicData = JsonConvert.DeserializeObject<BlockData>(blockJson);

        // Check if our dictionary already contains the block
        // and remove that block
        int id = basicData.id;
        if (blocks.ContainsKey(id))
        {
            RemoveBlock(new RemoveData(id));
            message = "Removed Duplicate Block";
        }

        // Place wires differently


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
            JsonConvert.PopulateObject(blockJson, bl.GetData());
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
        // Find the prefab, data loader, and data
        BlockPrefabEntry e = blockPrefabs.Find((v) => v.block == block);
        if (!blockPrefabs.Contains(e)) return new BMResponse(false, "No Prefab for: " + block);

        GameObject prefab = e.prefab;
        IDataLoader dl = prefab.GetComponent<IDataLoader>();
        if (dl == null) return new BMResponse(false, "Block had no Data Loader");

        BlockData d = dl.GetData();
        if (d == null) return new BMResponse(false, "Block had no Data");


        // Obtaine block's default data to pass to server
        BlockData data = d.GetDefaultState();

        // Change position to the requested position and set ID to zero
        //data.id = x * 100 + y * 10 + z;
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

    public BMResponse ClientRemoveBlock(int id)
    {
        // Unnecessary check here (server can just do it), but this helps reduce client-server communication
        GameObject target = blocks[id];
        if (!target) return new BMResponse(false, "Block Not Found");

        // Prepare and send request
        RemoveData requestData = new RemoveData(id);
        if (!jp) return new BMResponse(false, "No JSONParser");
        jp.SendRemoveRequest(requestData);

        return new BMResponse(true, "ok");
    }

    public BMResponse ClientUpdateBlock(int id, string block, object data)
    {
        // Unnecessary check here (server can just do it), but this helps reduce client-server communication
        GameObject target = blocks[id];
        if (!target) return new BMResponse(false, "Block Not Found");

        UpdateData requestData = new UpdateData(id, block, data);
        if (!jp) return new BMResponse(false, "No JSONParser");
        jp.SendUpdateRequest(requestData);

        return new BMResponse(true, "ok");
    }

    private void Awake()
    {
        tc = GetComponent<TCPClient>();
        jp = GetComponent<JSONParser>();
        wm = GetComponentInChildren<WireManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(BlockTest());
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

    // Block test waits a few seconds for the server to activate, then places/edits several blocks
    IEnumerator BlockTest()
    {
        yield return new WaitForSeconds(5);

        ClientPlaceBlock("block", 0, 2, 2);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("block", 2, 2, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("block", 2, 0, 2);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("block", 2, 2, 2);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 0, 0, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 2, 0, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 0, 2, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 0, 0, 2);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 0, 3, 0);
        yield return new WaitForFixedUpdate();

        ClientRemoveBlock(030);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("pixel", 0, 4, 0);
        yield return new WaitForFixedUpdate();

        ClientUpdateBlock(040, "pixel", new { powered = true });
    }
}
