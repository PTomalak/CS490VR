using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONParser : MonoBehaviour
{
    BlockManager bm;
    TCPClient tc;

    // Classes for interacting with actions in JSON
    [System.Serializable]
    public class Action
    {
        public string action;
        public ActionData data;

        public Action(string a, ActionData d)
        {
            action = a;
            data = d;
        }
    }

    [System.Serializable]
    public class ActionData
    {
    } 

    [System.Serializable]
    public class PlaceData : ActionData
    {
        public string block;
        public object data;

        public PlaceData(string v, object d)
        {
            block = v;
            data = d;
        }
    }

    [System.Serializable]
    public class RemoveData : ActionData
    {
        public int id;
        public RemoveData(int i)
        {
            id = i;
        }
    }

    [System.Serializable]
    public class UpdateData : ActionData
    {
        public int id;
        public object data;

        public UpdateData(int i, object d)
        {
            id = i;
            data = d;
        }
    }

    [System.Serializable]
    public class BMResponse
    {
        public bool ok;
        public string message;

        public BMResponse(bool ok, string message)
        {
            this.ok = ok;
            this.message = message;
        }

        public override string ToString()
        {
            return ok.ToString()+": "+message;
        }
    }

    // Take a request and call attached BlockManager's appropriate action
    // Also sends a BMResponse reflecting the success of the action
    public void PerformJson(string json)
    {
        Action action = JsonUtility.FromJson<Action>(json);
        if (action.action == null) return;

        if (!bm) bm = GetComponent<BlockManager>();
        if (!bm) return;

        BMResponse res = action.action switch
        {
            "place" => bm.PlaceBlock((PlaceData)action.data),
            "remove" => bm.RemoveBlock((RemoveData)action.data),
            "update" => bm.UpdateBlock((UpdateData)action.data),
            _ => new BMResponse(false, "Invalid Action"),// Nothing performed for invalid JSON
        };

        if (!tc) tc = GetComponent<TCPClient>();
        if (!tc) return;

        tc.SendJson(JsonUtility.ToJson(res));
    }

    // Convert local request into JSON and send request
    public void SendPlaceRequest(string request, PlaceData data)
    {
        if (!tc) tc = GetComponent<TCPClient>();
        if (!tc) return;

        string json = JsonUtility.ToJson(new Action(request, data));

        tc.SendJson(json);
    }
}
