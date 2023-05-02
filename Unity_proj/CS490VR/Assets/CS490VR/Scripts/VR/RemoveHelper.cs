using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RemoveHelper : MonoBehaviour
{
    public LayerMask layermask;
    public BlockManager blockManager;
    public BlockDictionary block_list;
    public GameObject prefab;

    public void RemoveBlock()
    {
        // Get the position of the calling game object
        Vector3 blockPosition = transform.position;

        GameObject newBlock = Instantiate(prefab, blockPosition, Quaternion.identity);
        newBlock.transform.localScale = Vector3.one * 0.05f;

        RaycastHit[] hit = Physics.RaycastAll(blockPosition, Vector3.up, 1.5f, layermask);
        // get the nearest raycast by sorting through them
        if (hit.Length > 0)
        {
            // wire example
            bool isWire = (hit[0].transform.gameObject.name == "Wire Controller");
            Vector3 v = hit[0].point;
            int id_wire = blockManager.GetWireAtGlobalPosition(blockPosition.x, blockPosition.y, blockPosition.z);
            if (id_wire != 0)
            {
                // this is a wire
                blockManager.ClientRemoveBlock(id_wire);
            }

            // for normal blocks
            IDataLoader dl = hit[0].transform.GetComponent<IDataLoader>();
            if (dl != null)
            {
                int id = dl.GetData().id;
                object updateData = new { position = blockPosition };

                blockManager.ClientUpdateBlock(id, updateData);
                blockManager.ClientRemoveBlock(id);
            }
        }

    }
}
