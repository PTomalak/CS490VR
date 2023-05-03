using UnityEngine;

public class PlaceHelper : MonoBehaviour
{
    public LayerMask layermask;
    public BlockManager blockManager;
    public BlockDictionary block_list;
    public GameObject prefab;

    public void PlaceBlock()
    {
        // Get the position of the calling game object
        Vector3 blockPosition = transform.position;

        // Instantiate a new block at the spawn position
        blockManager.ClientPlaceBlockGlobal("block", blockPosition.x, blockPosition.y, blockPosition.z);

        GameObject newBlock = Instantiate(prefab, blockPosition, Quaternion.identity);
        newBlock.transform.localScale = Vector3.one * 0.05f;
        Destroy(newBlock, 1f);
    }
}
