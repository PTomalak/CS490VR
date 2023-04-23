using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataLoader : MonoBehaviour
{
    // Class to load and update block data, as well as store some important data that all blocks share
    public string id;

    // Map of script components to reference for data collection
    public Dictionary<string, Data> dataComponents = new Dictionary<string, Data>();

    private void Awake()
    {
        // Initialize data map
        foreach (Data d in GetComponents<Data>())
        {
            if (d.Name() == "") continue;
            if (dataComponents.ContainsKey(d.Name()))
            {
                Debug.LogWarning("Duplicated data: " + d.Name());
            }

            dataComponents[d.Name()] = d;
        }
    }
}
