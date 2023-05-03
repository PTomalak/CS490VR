using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BlockPicker : MonoBehaviour
{
    public LayerMask layermask;
    public BlockManager blockManager;
    public BlockDictionary block_list;
    public GameObject prefab;
    public InputActionProperty AButton;
    public string block_name;
    int index;
    // Start is called before the first frame update
    void Start()
    {
        index = 0;
        block_name = block_list.LIST[index].block;
    }

    // Update is called once per frame
    void Update()
    {
        if (AButton.action.WasPressedThisFrame()) {
            PlaceBlock();
            // Get the length of the list
            int list_length = block_list.LIST.Count;
            index = (index + 1) % list_length;
            block_name = block_list.LIST[index].block;

        }
    }


    public void PlaceBlock()
    {
        // Get the position of the calling game object
        Vector3 blockPosition = transform.position;

        GameObject newBlock = Instantiate(block_list.LIST[index].prefab, blockPosition, Quaternion.identity);
        newBlock.transform.localScale = Vector3.one * 0.05f;
        Destroy(newBlock, 1f);
    }
}
