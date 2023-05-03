using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

using static JSONParser;

public class BlockManager : MonoBehaviour
{
    #region components
    public JSONParser jp;
    public WireManager wm;
    #endregion

    #region fields
    // Stores all blocks (not voxels) by ID
    public Dictionary<int, GameObject> blocks = new Dictionary<int, GameObject>();

    // Dictionary associating blocks with their prefabs (shared w/ inventory)
    public BlockDictionary block_dict;

    public int tick = 0;
    #endregion

    ///// FROM SERVER FUNCTIONS /////
    #region server
    // Places a block into the world
    public BMResponse PlaceBlock(object data)
    {
        string message = "ok";

        string blockJson = JsonConvert.SerializeObject(data);
        BlockData basicData = JsonConvert.DeserializeObject<BlockData>(blockJson);

        Debug.Log("PLACE: " + blockJson);

        // Check if our dictionary already contains the block (or wire)
        // and remove that block (or wire)
        int id = basicData.id;
        if (blocks.ContainsKey(id) || wm.wire_ids.ContainsKey(id))
        {
            RemoveBlock(new RemoveData(id));
            message = "Removed Duplicate Block";
        }

        // Place wires using the WireManager
        if (basicData.data.block == "wire")
        {
            bool res = wm.PlaceWire(basicData);
            return new BMResponse(res, (res) ? "ok (wire)" : "Wire Manager couldn't place wire");
        }

        // Instantiate this voxel if we have a prefab for it
        BlockDictionary.BlockDictionaryObject e = block_dict.LIST.Find((v) => v.block == basicData.data.block);
        if (block_dict.LIST.Contains(e))
        {
            // Spawn the object in the world and make its name readable in the editor
            GameObject newObject = Instantiate(e.prefab, transform);
            if (!newObject) return new BMResponse(false, "Unity Object Creation Failed");
            newObject.name = id.ToString()+"\t"+basicData.data.block;

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
            return new BMResponse(false, "No Prefab for: "+basicData.data.block);
        }

        return new BMResponse(true, message);
    }

    // Removes a block from the world
    public BMResponse RemoveBlock(RemoveData removeData)
    {
        // See if we are removing a wire and intercept the removal
        if (wm.wire_ids.ContainsKey(removeData.id))
        {
            bool res = wm.RemoveWire(removeData.id);
            return new BMResponse(res, (res) ? "ok (wire)" : "Wire Manager couldn't remove wire");
        }

        // Check if we have that block in our system
        GameObject target = blocks[removeData.id];
        if (!target) return new BMResponse(false, "Block Not Found");

        // Unload the data of this block
        IDataLoader bl = target.GetComponent<IDataLoader>();
        if (bl == null)
        {
            return new BMResponse(false, "Block had no Data Loader");
        }
        bl.Unload();

        // Destroy gameobject
        blocks.Remove(removeData.id);
        Destroy(target);

        return new BMResponse(true, "ok");
    }

    // Updates the data of a block
    public BMResponse UpdateBlock(object data)
    {
        string blockJson = JsonConvert.SerializeObject(data);
        BlockData basicData = JsonConvert.DeserializeObject<BlockData>(blockJson);

        // See if we are updating a wire and intercept the update
        if (wm.wire_ids.ContainsKey(basicData.id))
        {
            // Obtain our existing data
            WireManager.WireData existing_data = wm.wire_map[wm.wire_ids[basicData.id]];
            BlockData wire_data = new BlockData();
            wire_data.id = existing_data.id;
            wire_data.position.Set(wm.wire_ids[basicData.id]);
            wire_data.SetAdditionalData("wire", new { powered = existing_data.powered });

            // Obtain modified version of existing data and update wires
            JsonConvert.PopulateObject(blockJson, wire_data);
            bool res = wm.UpdateWire(basicData.id, wire_data);
            return new BMResponse(res, (res) ? "ok (wire)" : "Wire Manager couldn't update wire");
        }

        // Check if we have that block in our system
        if (!blocks.ContainsKey(basicData.id)) return new BMResponse(false, "Block id "+basicData.id+" Not Found");
        GameObject target = blocks[basicData.id];

        // Unload and reload the block loader
        IDataLoader bl = target.GetComponent<IDataLoader>();
        bl.Unload();
        if (bl == null) return new BMResponse(false, "Block had no Data Loader");
        JsonConvert.PopulateObject(blockJson, bl.GetData());
        bl.Load();

        return new BMResponse(true, "ok");
    }
    #endregion



    ///// CLIENT SENDING METHODS /////
    #region client
    // Used by client to place a block at a given local XYZ position
    public BMResponse ClientPlaceBlock(string block, int x, int y, int z)
    {
        // Manually handle wire placement
        if (block == "wire")
        {
            // Construct wire data
            BlockData wire_data = new BlockData();
            wire_data.id = 0;
            wire_data.id = x * 100 + y * 10 + z; // ID for testing server. Actual server discards client IDs
            wire_data.position.Set(new Vector3Int(x, y, z));
            wire_data.SetAdditionalData("wire", new AdditionalData.Powered());

            // Prepare and send request
            if (!jp) return new BMResponse(false, "No JSONParser");
            jp.SendRequest("place", wire_data);

            return new BMResponse(true, "ok");
        }

        // Find the prefab, data loader, and data
        BlockDictionary.BlockDictionaryObject e = block_dict.LIST.Find((v) => v.block == block);
        if (!block_dict.LIST.Contains(e)) return new BMResponse(false, "No Prefab for: " + block);

        GameObject prefab = e.prefab;
        IDataLoader dl = prefab.GetComponent<IDataLoader>();
        if (dl == null) return new BMResponse(false, "Block had no Data Loader");


        // Obtaine block's default data to pass to server
        BlockData data = dl.GetDefaultState();
        dl.UpdateFromTick(data, this);

        // Set position and block type, and zero the ID
        data.id = 0;
        data.id = x * 100 + y * 10 + z; // ID for testing server. Actual server discards client IDs
        data.position.Set(new Vector3Int(x, y, z));
        data.data.block = block;

        // Prepare and send request
        if (!jp) return new BMResponse(false, "No JSONParser");
        jp.SendRequest("place", data);

        return new BMResponse(true, "ok");
    }

    // Global coordinate version of ClientPlaceBlock
    public void ClientPlaceBlockGlobal(string block, float x, float y, float z)
    {
        Vector3 lpos = transform.InverseTransformPoint(x, y, z);
        Vector3Int p = Vector3Int.RoundToInt(lpos);

        ClientPlaceBlock(block, p.x, p.y, p.z);
    }

    public BMResponse ClientRemoveBlock(int id)
    {
        // Unnecessary check here (server can just do it), but this helps reduce client-server communication
        if (!blocks.ContainsKey(id) && !wm.wire_ids.ContainsKey(id)) return new BMResponse(false, "Block Not Found");

        // Prepare and send request
        RemoveData requestData = new RemoveData(id);
        if (!jp) return new BMResponse(false, "No JSONParser");
        jp.SendRemoveRequest(requestData);

        return new BMResponse(true, "ok");
    }

    // Returns the ID of a wire at a given global position, or 0 if there isn't one.
    public int GetWireAtGlobalPosition(float x, float y, float z)
    {
        Vector3 lpos = transform.InverseTransformPoint(x, y, z);
        Vector3Int p = Vector3Int.RoundToInt(lpos);
        Vector3Int pos = new Vector3Int(p.x, p.y, p.z);

        if (wm.wire_map.ContainsKey(pos))
        {
            return wm.wire_map[pos].id;
        }
        return 0;
    }

    public BMResponse ClientUpdateBlock(int id, object data)
    {
        // Unnecessary check here (server can just do it), but this helps reduce client-server communication
        if (!blocks.ContainsKey(id) && !wm.wire_ids.ContainsKey(id)) return new BMResponse(false, "Block Not Found");

        if (wm.wire_ids.ContainsKey(id))
        {
            // Build and populate a PowerableData for wires
            WireManager.WireData target = wm.wire_map[wm.wire_ids[id]];
            if (target == null) return new BMResponse(false, "Wire Not Found");

            BlockData wire_data = new BlockData();
            wire_data.id = target.id;
            wire_data.position.Set(wm.wire_ids[id]);
            wire_data.SetAdditionalData("wire", new { powered = target.powered });

            if (wire_data == null) return new BMResponse(false, "Block had no Data");
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(data), wire_data);

            if (!jp) return new BMResponse(false, "No JSONParser");
            jp.SendRequest("update", wire_data);

        } else
        {
            // Obtain data loader via typical means
            GameObject target = blocks[id];
            if (!target) return new BMResponse(false, "Block Not Found");
            IDataLoader dl = target.GetComponent<IDataLoader>();
            if (dl == null) return new BMResponse(false, "Block had no Data Loader");

            // Populate actual data (pretty bad way to do it, I know) then restore its previous state without loading any changes
            if (!jp) return new BMResponse(false, "No JSONParser");
            string saved_data = JsonConvert.SerializeObject(dl.GetData());
            JsonConvert.PopulateObject(JsonConvert.SerializeObject(data), dl.GetData());
            jp.SendRequest("update", dl.GetData());
            JsonConvert.PopulateObject(saved_data, dl.GetData());

            // Altenatively just update request with the data
            
        }

        

        return new BMResponse(true, "ok");
    }
    #endregion

    private void Awake()
    {
        jp = GetComponent<JSONParser>();
        wm = GetComponentInChildren<WireManager>();
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(BlockTest());
    }

    private void FixedUpdate()
    {
        // Reload the wire mesh only if it has changed
        if (wm.hasChanged)
        {
            wm.ReloadMesh();
            wm.hasChanged = false;
        }
    }

    // Block test waits a few seconds for the server to activate, then places/edits several blocks
    IEnumerator BlockTest()
    {
        yield return new WaitForSeconds(0.5f);

        //ClientRemoveBlock(100);

        //ClientPlaceBlock("block", 0, 0, 0);

        // Place several blocks
        //yield return new WaitForSeconds(0.05f);
        //ClientPlaceBlock("toggle", 0, 0, 0);
        //yield return new WaitForSeconds(0.05f);
        //ClientPlaceBlock("clock", 2, 0, 0);
        //yield return new WaitForSeconds(0.05f);
        //ClientPlaceBlock("wire", 0, 0, 1);
        //yield return new WaitForSeconds(0.05f);
        ////ClientPlaceBlock("wire", 2, 0, 1);
        //yield return new WaitForSeconds(0.05f);
        //ClientPlaceBlock("and_gate", 1, 0, 3);
        //yield return new WaitForSeconds(0.05f);
        ////ClientPlaceBlock("wire", 1, 0, 5);
        //yield return new WaitForSeconds(0.05f);
        //ClientPlaceBlock("pixel", 1, 0, 6);

        //yield return new WaitForSeconds(2.5f);
        //ClientUpdateBlock(0, new { data = new { data = new { powered = true } } });
    }
}
