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

        RaycastHit hit;
        if (Physics.Raycast(blockPosition, Vector3.up, out hit, 1.5f, layermask))
        {
            // Create a temporary visual effect for the removed block
            GameObject newBlock = Instantiate(prefab, blockPosition, Quaternion.identity);
            Destroy(newBlock, 1f);

            // Attempt to get the block ID from the hit game object
            IDataLoader dl = hit.transform.GetComponent<IDataLoader>();
            if (dl != null)
            {
                int id = dl.GetData().id;
                blockManager.ClientRemoveBlock(id);
            }
        }
    }

}
