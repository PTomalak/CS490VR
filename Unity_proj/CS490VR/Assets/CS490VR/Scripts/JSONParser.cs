using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONParser : MonoBehaviour
{
    BlockManager bm;

    // Classes for interacting with actions in JSON
    [System.Serializable]
    public struct Action
    {
        public string action;
        public object data;

        public Action(string a, object d)
        {
            action = a;
            data = d;
        }
    }

    [System.Serializable]
    public struct PlaceData
    {
        public string voxel;
        public object data;

        public PlaceData(string v, object d)
        {
            voxel = v;
            data = d;
        }
    }

    [System.Serializable]
    public struct RemoveData
    {
        public string id;
        public RemoveData(string i)
        {
            id = i;
        }
    }

    [System.Serializable]
    public struct UpdateData
    {
        public string id;
        public object data;

        public UpdateData(string i, object d)
        {
            id = i;
            data = d;
        }
    }

    // Take a request and call attached BlockManager's appropriate action
    public void PerformJson(string json)
    {
        Action action = JsonUtility.FromJson<Action>(json);
        if (action.action == null) return;

        if (!bm) bm = GetComponent<BlockManager>();
        if (!bm) return;

        switch (action.action)
        {
            case "place":
                bm.PlaceBlock((PlaceData)action.data);
                break;
            case "remove":
                bm.RemoveBlock((RemoveData)action.data);
                break;
            case "update":
                bm.UpdateBlock((UpdateData)action.data);
                break;
            default:
                // Nothing performed for invalid JSON
                Debug.LogWarning("Invalid JSON Request: " + json);
                break;
        }
    }

    // Convert local request into JSON and send request
    public void SendRequest(string request, object data)
    {
        
    }
}
