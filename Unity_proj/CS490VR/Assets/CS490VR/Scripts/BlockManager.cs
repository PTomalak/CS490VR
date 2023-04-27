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
    public WireManager wm;
    #endregion

    #region fields
    // Stores all blocks (not voxels) by ID
    public Dictionary<int, GameObject> blocks = new Dictionary<int, GameObject>();

    // Queue to transfer block placing commands from background thread to maing thread
    public Queue<string> actions = new Queue<string>();

    // Interact with in editor to add string / gameObject pairs
    // The string is the block's name, the gameObject is that block's prefab
    [SerializeField]
    public BlockDictionary block_dict;
    #endregion




    ///// FROM SERVER FUNCTIONS /////
    #region server
    // Places a block into the world
    public BMResponse PlaceBlock(PlaceData placeData)
    {
        string message = "ok";

        string blockJson = JsonConvert.SerializeObject(placeData.data);
        BlockData basicData = JsonConvert.DeserializeObject<BlockData>(blockJson);

        // Check if our dictionary already contains the block (or wire)
        // and remove that block (or wire)
        int id = basicData.id;
        if (blocks.ContainsKey(id) || wm.wire_ids.ContainsKey(id))
        {
            RemoveBlock(new RemoveData(id));
            message = "Removed Duplicate Block";
        }

        // Place wires using the WireManager
        if (placeData.block == "wire")
        {
            PowerableData wire_data = JsonConvert.DeserializeObject<PowerableData>(blockJson);
            bool res = wm.PlaceWire(wire_data);
            return new BMResponse(res, (res) ? "ok (wire)" : "Wire Manager couldn't place wire");
        }

        // Instantiate this voxel if we have a prefab for it
        BlockDictionary.BlockDictionaryObject e = block_dict.LIST.Find((v) => v.block == placeData.block);
        if (block_dict.LIST.Contains(e))
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
    public BMResponse UpdateBlock(UpdateData updateData)
    {
        string blockJson = JsonConvert.SerializeObject(updateData.data);

        // See if we are updating a wire and intercept the update
        if (wm.wire_ids.ContainsKey(updateData.id))
        {
            // Obtain our existing data
            WireManager.WireData existing_data = wm.wire_map[wm.wire_ids[updateData.id]];
            PowerableData wire_data = new PowerableData();
            wire_data.id = existing_data.id;
            wire_data.position = wm.wire_ids[updateData.id];
            wire_data.powered = existing_data.powered;

            // Obtain modified version of existing data and update wires
            JsonConvert.PopulateObject(blockJson, wire_data);
            bool res = wm.UpdateWire(updateData.id, wire_data);
            return new BMResponse(res, (res) ? "ok (wire)" : "Wire Manager couldn't update wire");
        }

        // Check if we have that block in our system
        GameObject target = blocks[updateData.id];
        if (!target) return new BMResponse(false, "Block Not Found");

        // Update block loader
        IDataLoader bl = target.GetComponent<IDataLoader>();
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
            PowerableData wire_data = new PowerableData();
            wire_data.position = new int[] { x, y, z };
            wire_data.id = 0;
            wire_data.id = x * 100 + y * 10 + z;
            wire_data.powered = false;

            // Prepare and send request
            PlaceData wirePlaceData = new PlaceData(block, wire_data);
            if (!jp) return new BMResponse(false, "No JSONParser");
            jp.SendPlaceRequest(wirePlaceData);

            return new BMResponse(true, "ok");
        }

        // Find the prefab, data loader, and data
        BlockDictionary.BlockDictionaryObject e = block_dict.LIST.Find((v) => v.block == block);
        if (!block_dict.LIST.Contains(e)) return new BMResponse(false, "No Prefab for: " + block);

        GameObject prefab = e.prefab;
        IDataLoader dl = prefab.GetComponent<IDataLoader>();
        if (dl == null) return new BMResponse(false, "Block had no Data Loader");

        BlockData d = dl.GetData();
        if (d == null) return new BMResponse(false, "Block had no Data");


        // Obtaine block's default data to pass to server
        BlockData data = d.GetDefaultState();

        // Change position to the requested position and set ID to zero
        data.id = 0;
        data.id = x * 100 + y * 10 + z;
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
        int[] pos = new int[3] { p.x, p.y, p.z };

        if (wm.wire_map.ContainsKey(pos))
        {
            return wm.wire_map[pos].id;
        }
        return 0;
    }

    public BMResponse ClientUpdateBlock(int id, string block, object data)
    {
        // Unnecessary check here (server can just do it), but this helps reduce client-server communication
        if (!blocks.ContainsKey(id) && !wm.wire_ids.ContainsKey(id)) return new BMResponse(false, "Block Not Found");

        UpdateData requestData = new UpdateData(id, block, data);
        if (!jp) return new BMResponse(false, "No JSONParser");
        jp.SendUpdateRequest(requestData);

        return new BMResponse(true, "ok");
    }
    #endregion

    private void Awake()
    {
        tc = GetComponent<TCPClient>();
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
        // Perform up to a maximum number of queued operations per frame
        int ops = 0;
        while(actions.Count > 0 && ops < 50)
        {
            jp.PerformJson(actions.Dequeue());
            ops += 1;
        }

        // Reload the wire mesh only if it has changed
        if (wm.hasChanged)
        {
            wm.ReloadMesh();
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

        ClientPlaceBlock("wire", 1, 0, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("wire", 0, 1, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("wire", 0, 0, 1);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("wire", 2, 1, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("wire", 1, 0, 2);
        yield return new WaitForFixedUpdate();

        ClientRemoveBlock(030);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("toggle", 0, 4, 0);
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("wire", 1, 4, 0);
        yield return new WaitForFixedUpdate();

        ClientUpdateBlock(140, "wire", new { powered = true });
        yield return new WaitForFixedUpdate();

        ClientPlaceBlock("and_gate", 1, 5, 1);
        yield return new WaitForFixedUpdate();
    }
}
