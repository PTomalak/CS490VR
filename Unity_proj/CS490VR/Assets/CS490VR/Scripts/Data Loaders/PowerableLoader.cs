using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerableLoader : MonoBehaviour, IDataLoader
{
    // Two materials (textures) for whether it is on or off
    [SerializeField]
    public Material ON_MATERIAL;
    [SerializeField]
    public Material OFF_MATERIAL;

    // Store the powerable data (which references this component for textures)
    public PowerableData data;

    public void Load()
    {
        data.UpdateObject(gameObject);
    }

    public void SetData(object o)
    {
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(o), data);
    }

    public object GetData()
    {
        return data;
    }
}
