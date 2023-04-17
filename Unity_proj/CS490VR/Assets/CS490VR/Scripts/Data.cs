using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    // Abstract class that is used to load data
    // NOTE: only data that affects how a block is rendered matters!

    public virtual void Load() { }

    private void Awake()
    {
        Load();
    }
}
