using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JSONParser : MonoBehaviour
{
    public PlayerManager pm;    // MUST SET THIS IN UNITY
    public BlockManager bm;
    public TCPClient tc;

    // Queue to transfer JSON commands from background thread to maing thread
    public Queue<string> incomingActions = new Queue<string>();

    // Classes for interacting with actions in JSON
    public class Action
    {
        public string action;
    }
    public class PlaceAction : Action
    {
        public PlaceData[] data;

        public PlaceAction(string action, PlaceData[] data)
        {
            this.action = action;
            this.data = data;
        }
    }
    public class UpdateAction : Action
    {
        public UpdateData[] data;

        public UpdateAction(string action, UpdateData[] data)
        {
            this.action = action;
            this.data = data;
        }
    }
    public class RemoveAction : Action
    {
        public RemoveData[] data;

        public RemoveAction(string action, RemoveData[] data)
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

    private void Update()
    {
        // Perform up to a maximum number of queued operations per frame
        int ops = 0;
        while (incomingActions.Count > 0 && ops < 50)
        {
            HandleMessage(incomingActions.Dequeue());
            ops += 1;
        }
    }


    // Determine which type of request a given JSON is and perform the appropriate action with it
    public void HandleMessage(string json)
    {
        PlayerManager.PlayerData[] p_data = new PlayerManager.PlayerData[0] { };
        JsonConvert.PopulateObject(json, p_data);

        if (p_data.Length > 0)
        {
            pm.UpdatePlayerList(p_data);
        } else
        {
            PerformAction(json);
        }
    }


    // Take a request and call attached BlockManager's appropriate action
    // Also sends a BMResponse reflecting the success of the action
    public void PerformAction(string json)
    {
        Action action = JsonConvert.DeserializeObject<Action>(json);
        if (action.action == null) return;  // Do nothing for a non-action
        if (!bm) return;


        // Depending on the action, unpack the JSON into the correct set of actions
        if (!tc) return;
        switch (action.action)
        {
            case "place":
                {
                    PlaceAction act = JsonConvert.DeserializeObject<PlaceAction>(json);
                    foreach (PlaceData data in act.data)
                    {
                        BMResponse resp = bm.PlaceBlock(data);
                        tc.SendJson(JsonUtility.ToJson(resp));
                    }
                }
                break;
            case "remove":
                {
                    RemoveAction act = JsonConvert.DeserializeObject<RemoveAction>(json);
                    foreach (RemoveData data in act.data)
                    {
                        BMResponse resp = bm.RemoveBlock(data);
                        tc.SendJson(JsonUtility.ToJson(resp));
                    }
                }
                break;
            case "update":
                {
                    UpdateAction act = JsonConvert.DeserializeObject<UpdateAction>(json);
                    foreach (UpdateData data in act.data)
                    {
                        BMResponse resp = bm.UpdateBlock(data);
                        tc.SendJson(JsonUtility.ToJson(resp));
                    }
                }
                break;
            default:
                BMResponse res = new BMResponse(false, "Invalid Action");
                tc.SendJson(JsonUtility.ToJson(res));
                break;
        }
    }

    // Convert local request into JSON and send request
    public void SendPlaceRequest(PlaceData data)
    {
        SendPlaceRequest(new PlaceData[1] { data });
    }
    public void SendPlaceRequest(PlaceData[] data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new PlaceAction("place", data)));
    }
    public void SendRemoveRequest(RemoveData data)
    {
        SendRemoveRequest(new RemoveData[1] { data });
    }
    public void SendRemoveRequest(RemoveData[] data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new RemoveAction("remove", data)));
    }
    public void SendUpdateRequest(UpdateData data)
    {
        SendUpdateRequest(new UpdateData[1] { data });
    }
    public void SendUpdateRequest(UpdateData[] data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new UpdateAction("update", data)));
    }
}
