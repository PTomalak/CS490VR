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
    public class UpdateAction : Action
    {
        public object[] data;
        public UpdateAction(object up)
        {
            this.action = "BothRequestUpdateBlocks";
            this.data = new object[1] { up };
        }
    }
    public class RemoveAction : Action
    {
        public RemoveData[] data;

        public RemoveAction(RemoveData[] data)
        {
            this.action = "BothRequestRemoveBlocks";
            this.data = data;
        }
    }
    [System.Serializable]
    public class ServerAction : Action
    {
        public ServerData data;
        public ServerAction(PlayerManager.PlayerData[] data)
        {
            this.action = "ServerResponseMetadata";
            this.data.clients = data;
        }
    }
    [System.Serializable]
    public class JoinAction : Action
    {
        public PlayerManager.PlayerData data;

        public JoinAction(PlayerManager.PlayerData data)
        {
            this.action = "ClientRequestJoin";
            this.data = data;
        }
    }
    public class ResponseAction : Action
    {
        public BMResponse data;

        public ResponseAction(BMResponse data)
        {
            this.action = "BothResponse";
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
    [System.Serializable]
    public class ServerData
    {
        public int ticks = 0;
        public PlayerManager.PlayerData[] clients;
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
        if (!tc) return;

        // Depending on the action, unpack the JSON into the correct set of actions
        switch (action.action)
        {
            case "BothRequestPlaceBlocks":
                {
                    PlaceUpdateAction act = JsonConvert.DeserializeObject<PlaceUpdateAction>(json);
                    foreach (object data in act.data)
                    {
                        BMResponse resp = bm.PlaceBlock(data);
                        tc.SendJson(JsonUtility.ToJson(new ResponseAction(resp)));
                    }
                }
                break;
            case "BothRequestRemoveBlocks":
                {
                    RemoveAction act = JsonConvert.DeserializeObject<RemoveAction>(json);
                    foreach (RemoveData data in act.data)
                    {
                        BMResponse resp = bm.RemoveBlock(data);
                        tc.SendJson(JsonUtility.ToJson(new ResponseAction(resp)));
                    }
                }
                break;
            case "BothRequestUpdateBlocks":
                {
                    PlaceUpdateAction act = JsonConvert.DeserializeObject<PlaceUpdateAction>(json);
                    foreach (object data in act.data)
                    {
                        BMResponse resp = bm.UpdateBlock(data);

                        tc.SendJson(JsonUtility.ToJson(new ResponseAction(resp)));
                    }
                }
                break;
            case "ServerResponseMetadata":
                {
                    ServerAction act = JsonUtility.FromJson<ServerAction>(json);
                    bm.tick = act.data.ticks;
                    pm.UpdatePlayerList(act.data.clients);
                    break;
                }
            default:
                BMResponse res = new BMResponse(false, "Invalid Action");
                //tc.SendJson(JsonUtility.ToJson(new ResponseAction(res)));
                break;
        }
    }

    public void SendRequest(string req, BlockData data)
    {
        SendRequest(req, new BlockData[1] { data });
    }
    public void SendRequest(string req, BlockData[] data)
    {
        if (!tc) return;
        switch (req)
        {
            case "place":
                tc.SendJson(JsonConvert.SerializeObject(new PlaceUpdateAction("BothRequestPlaceBlocks",data)));
                break;
            case "update":
                tc.SendJson(JsonConvert.SerializeObject(new PlaceUpdateAction("BothRequestUpdateBlocks",data)));
                break;
        }
    }

    public void SendUpdateRequest(object data)
    {
        tc.SendJson(JsonConvert.SerializeObject(new UpdateAction(data)));
    }

    public void SendJoinRequest(PlayerManager.PlayerData data)
    {
        tc.SendJson(JsonUtility.ToJson(new JoinAction(data)));
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
