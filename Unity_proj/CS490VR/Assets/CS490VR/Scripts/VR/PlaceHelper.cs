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
        blockManager.ClientPlaceBlockGlobal("block", Mathf.RoundToInt(blockPosition.x), Mathf.RoundToInt(blockPosition.y), Mathf.RoundToInt(blockPosition.z));
        blockManager.ClientPlaceBlockGlobal("block", 0, 0, 0);

        GameObject newBlock = Instantiate(prefab, blockPosition, Quaternion.identity);
        newBlock.transform.localScale = Vector3.one * 0.05f;
        Destroy(newBlock, 1f);

    }
}
