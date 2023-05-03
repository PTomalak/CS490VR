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

        float raycastSize = 1.5f;
        Vector3 raycastDir = transform.rotation * Vector3.forward;

        RaycastHit[] hit = Physics.RaycastAll(blockPosition, raycastDir, raycastSize, layermask);
        // get the nearest raycast by sorting through them
        if (hit.Length > 0)
        {
            GameObject newBlock = Instantiate(prefab, blockPosition+(raycastDir.normalized*raycastSize), Quaternion.identity);
            newBlock.transform.localScale = Vector3.one * 0.05f;
            Destroy(newBlock, 1f);
            
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
