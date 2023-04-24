using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONParser
{
    // Classes for interacting with actions in JSON
    [System.Serializable]
    public struct Action
    {
        public string action;
        public object data;
    }

    [System.Serializable]
    public struct PlaceData
    {
        public string voxel;
        public object data;
    }

    [System.Serializable]
    public struct RemoveData
    {
        public string id;
    }

    [System.Serializable]
    public struct UpdateData
    {
        public string id;
        public object data;
    }

    [System.Serializable]
    public struct InteractData
    {
        public string id;
    }
}
