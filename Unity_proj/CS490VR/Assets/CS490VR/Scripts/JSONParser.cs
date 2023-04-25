using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONParser : MonoBehaviour
{
    BlockManager bm;
    TCPClient tc;

    // Classes for interacting with actions in JSON
    public class Action
    {
        public string action;
    }
    public class PlaceAction : Action
    {
        public PlaceData data;

        public PlaceAction(string action, PlaceData data)
        {
            this.action = action;
            this.data = data;
        }
    }
    public class UpdateAction : Action
    {
        public UpdateData data;

        public UpdateAction(string action, UpdateData data)
        {
            this.action = action;
            this.data = data;
        }
    }
    public class RemoveAction : Action
    {
        public RemoveData data;

        public RemoveAction(string action, RemoveData data)
        {
            this.action = action;
            this.data = data;
        }
    }

    // Classes for serializing data for certain types of actions
    public class PlaceData
    {
        public string block;
        public object data;

        public PlaceData(string v, BlockData d)
        {
            block = v;
            data = d;
        }
    }
    public class RemoveData
    {
        public int id;
        public RemoveData(int i)
        {
            id = i;
        }
    }
    public class UpdateData
    {
        public int id;
        public string block;
        public object data;

        public UpdateData(int i, string b, object d)
        {
            id = i;
            block = b;
            data = d;
        }
    }

    // Class for serializing the BlockManager's response to a given action
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

    ///// UNITY FUNCTIONS /////
    private void Awake()
    {
        bm = GetComponent<BlockManager>();
        tc = GetComponent<TCPClient>();
    }

    // Take a request and call attached BlockManager's appropriate action
    // Also sends a BMResponse reflecting the success of the action
    public void PerformJson(string json)
    {
        Action action = JsonConvert.DeserializeObject<Action>(json);
        if (action.action == null) return;  // Do nothing for a non-action
        if (!bm) return;


        // Unpack the BlockData for a place/update action
        

        Debug.Log("PlaceData: " + JsonConvert.SerializeObject(JsonConvert.DeserializeObject<PlaceAction>(json).data));
        BMResponse res = action.action switch
        {
            "place" => bm.PlaceBlock(JsonConvert.DeserializeObject<PlaceAction>(json).data),
            "remove" => bm.RemoveBlock(JsonConvert.DeserializeObject<RemoveAction>(json).data),
            "update" => bm.UpdateBlock(JsonConvert.DeserializeObject<UpdateAction>(json).data),
            _ => new BMResponse(false, "Invalid Action"),// Nothing performed for invalid JSON
        };

        if (!tc) return;
        tc.SendJson(JsonUtility.ToJson(res));
    }

    // Convert local request into JSON and send request
    public void SendPlaceRequest(PlaceData data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new PlaceAction("place", data)));
    }
    public void SendRemoveRequest(RemoveData data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new RemoveAction("remove", data)));
    }
    public void SendUpdateRequest(UpdateData data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new UpdateAction("update", data)));
    }
}
