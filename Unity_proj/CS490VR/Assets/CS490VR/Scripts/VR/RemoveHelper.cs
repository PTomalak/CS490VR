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
        Destroy(newBlock, 1f);

        RaycastHit[] hit = Physics.RaycastAll(blockPosition, Vector3.up, 1.5f, layermask);
        // get the nearest raycast by sorting through them
        if (hit.Length > 0)
        {
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
