using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class PlayerManager : MonoBehaviour
{
    #region FILL IN EDITOR
    public TCPClient tc;
    public GameObject playerPrefab;
    #endregion

    // Data to track player position/rotation
    public string myName = "";
    public Vector3 myPosition;
    public Vector3 myRotation;
    public Dictionary<string, GameObject> playerList;

    // Data to handle sending the position/rotation updates
    float SEND_FREQUENCY = 0.05f;
    float prev_send_time;

    public class PlayerData
    {
        public string name;
        public Vector3 position;
        public Vector3 direction;

        public PlayerData(string name, Vector3 pos, Vector3 rot)
        {
            this.name = name;
            this.position = pos;
            this.direction = rot;
        }
    }

    public void UpdatePlayerList(PlayerData[] data)
    {
        // Set up unused ids to remove nonexistent players
        List<string> unusedIds = new List<string>();
        foreach (string name in playerList.Keys)
        {
            unusedIds.Add(name);
        }

        // Place or move every player
        foreach (PlayerData d in data)
        {
            if (d.name == myName) continue;
            unusedIds.Remove(d.name);
            Quaternion rot = Quaternion.Euler(d.direction);

            if (playerList.ContainsKey(d.name))
            {
                // Move the object to the correct point
                playerList[d.name].transform.localPosition = d.position;
                playerList[d.name].transform.localRotation = rot;
            }
            else
            {
                // Spawn an object at the correct point
                GameObject newPlayer = Instantiate(playerPrefab, transform);
                newPlayer.transform.localPosition = d.position;
                newPlayer.transform.localRotation = rot;

                playerList.Add(d.name, newPlayer);
            }
        }

        // Remove objects associated with unused ids
        foreach (string name in unusedIds)
        {
            if (!playerList.ContainsKey(name)) continue;
            Destroy(playerList[name]);
        }
    }

    public void UpdatePositionRotation(Vector3 pos, Vector3 rot)
    {
        myPosition = pos;
        myRotation = rot;
    }

    public void InitializePlayer(string name, Vector3 pos, Vector3 rot)
    {
        myName = name;
        myPosition = pos;
        myRotation = rot;
        tc.SendJson(JsonUtility.ToJson(new PlayerData(myName, myPosition, myRotation)));
    }

    private void Awake()
    {
        prev_send_time = Time.time;
    }

    // Periodically send updates with our position and rotation
    private void FixedUpdate()
    {
        if (myName != "" && prev_send_time+SEND_FREQUENCY < Time.time)
        {
            //tc.SendJson(JsonUtility.ToJson(new PlayerData(myName, myPosition, myRotation)));
            prev_send_time = Time.time;
        }
    }
}
