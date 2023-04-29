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
    public class PlaceUpdateAction : Action
    {
        public object[] data;

        public PlaceUpdateAction(string action, object[] data)
        {
            this.action = action;
            this.data = data;
        }
    }
    public class RemoveAction : Action
    {
        public RemoveData[] data;

        public RemoveAction(RemoveData[] data)
        {
            this.action = "remove";
            this.data = data;
        }
    }
    public class PlayersAction : Action
    {
        public PlayerManager.PlayerData[] data;

        public PlayersAction(PlayerManager.PlayerData[] data)
        {
            this.action = "players";
            this.data = data;
        }
    }

    // Classes for serializing data for certain types of actions
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
            PerformAction(incomingActions.Dequeue());
            ops += 1;
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
                    PlaceUpdateAction act = JsonConvert.DeserializeObject<PlaceUpdateAction>(json);
                    foreach (object data in act.data)
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
                    PlaceUpdateAction act = JsonConvert.DeserializeObject<PlaceUpdateAction>(json);
                    foreach (object data in act.data)
                    {
                        BMResponse resp = bm.UpdateBlock(data);
                        tc.SendJson(JsonUtility.ToJson(resp));
                    }
                }
                break;
            case "players":
                {
                    PlayersAction act = JsonConvert.DeserializeObject<PlayersAction>(json);
                    pm.UpdatePlayerList(act.data);
                    break;
                }
            default:
                BMResponse res = new BMResponse(false, "Invalid Action");
                tc.SendJson(JsonUtility.ToJson(res));
                break;
        }
    }

    public void SendRequest(string req, object data)
    {
        SendRequest(req, new object[1] { data });
    }
    public void SendRequest(string req, object[] data)
    {
        if (!tc) return;
        switch (req)
        {
            case "place":
                tc.SendJson(JsonConvert.SerializeObject(new PlaceUpdateAction("place",data)));
                break;
            case "update":
                tc.SendJson(JsonConvert.SerializeObject(new PlaceUpdateAction("update",data)));
                break;
        }
    }

    // Convert local request into JSON and send request
    public void SendRemoveRequest(RemoveData data)
    {
        SendRemoveRequest(new RemoveData[1] { data });
    }
    public void SendRemoveRequest(RemoveData[] data)
    {
        if (!tc) return;
        tc.SendJson(JsonConvert.SerializeObject(new RemoveAction(data)));
    }
}
