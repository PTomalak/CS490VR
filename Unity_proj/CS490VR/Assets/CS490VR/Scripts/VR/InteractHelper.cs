using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractHelper : MonoBehaviour
{
    public LayerMask layermask;
    public InputActionProperty XButton;
    public InputActionProperty YButton;

    void Update()
    {
        if (XButton.action.WasPressedThisFrame())
        {
            InteractBlock(false);
        } else if (YButton.action.WasPressedThisFrame())
        {
            InteractBlock(true);
        }
    }

    public void InteractBlock(bool alt)
    {
        // Get the position of the calling game object
        Vector3 blockPosition = transform.position;

        float raycastSize = 1.5f;
        Vector3 raycastDir = transform.rotation * Vector3.forward;
        Vector3 offset = (raycastDir.normalized * -0.25f * raycastSize);

        RaycastHit[] hit = Physics.RaycastAll(blockPosition + offset, raycastDir, raycastSize, layermask);

        if (hit.Length == 0) return;

        RaycastHit nearest = hit[0];
        float dist = hit[0].distance;
        foreach (RaycastHit h in hit)
        {
            if (h.distance < dist)
            {
                nearest = h;
                dist = h.distance;
            }
        }

        if (alt)
        {
            AltInteractable a = nearest.transform.GetComponent<AltInteractable>();
            if (!a)
            {
                Interactable i = nearest.transform.GetComponent<Interactable>();
                if (!i)
                {
                    return;
                }
                i.Interact();
            }
            a.AltInteract();
        } else
        {
            Interactable i = nearest.transform.GetComponent<Interactable>();
            if (!i)
            {
                return;
            }
            i.Interact();
        }
    }
}
