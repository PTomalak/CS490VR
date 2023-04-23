using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Data : MonoBehaviour
{
    // Abstract class that is used to load data
    // NOTE: only data that affects how a block is rendered matters.

    // Name the data loader would use to refer to this data. Usually, this is what the JSON uses.
    public virtual string Name() { return ""; }

    // Update a Unity object with the current data variables.
    public virtual void Load() { }

    // Update this component's data from an input string.
    public virtual void FromString(string input) { }

    // Compare this component's data to another one.
    public virtual bool EqualData(Data other) { return true; }

    private void Awake()
    {
        Load();
    }
}
