using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour
{
    public LayerMask layermask;
    public BlockManager blockManager;
    public BlockDictionary block_list;

    // Start is called before the first frame update
    void Start()
    {
        //StartCoroutine(TestCoroutine());
    }

    void FixedUpdate()
    {
        // check if button is pressed
        if (Input.GetAxis("button") > 0)
        {
            // get position/rotation of controller
            
            // get raycast input / direction

            // call raycast from below
        }
    }

    IEnumerator TestCoroutine()
    {
        yield return new WaitForSeconds(5f);

        RaycastHit[] hit = Physics.RaycastAll(new Vector3(0, -1, 0), Vector3.up, 1.5f, layermask);
        // get the nearest raycast by sorting through them
        if (hit.Length > 0)
        {
            // wire example
            bool isWire = (hit[0].transform.gameObject.name == "Wire Controller");
            Vector3 v = hit[0].point;
            int id_wire = blockManager.GetWireAtGlobalPosition(v.x, v.y, v.z);
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
                object updateData = new { position = new Vector3Int(9, 9, 9) };

                blockManager.ClientUpdateBlock(id, updateData);
                blockManager.ClientRemoveBlock(id);
            }

            // Place block example
            blockManager.ClientPlaceBlockGlobal("block", 0, 0, 0);

            // Toggle / pulse block interaction example
            Interactable interactable = hit[0].transform.GetComponent<Interactable>();
            if (interactable)
            {
                interactable.Interact();
            }
        }
    }
}
