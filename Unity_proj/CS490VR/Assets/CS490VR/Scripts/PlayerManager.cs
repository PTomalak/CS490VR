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
    public int myId;
    public Vector3 myPosition;
    public Vector3 myRotation;
    public Dictionary<int, GameObject> playerList;

    // Data to handle sending the position/rotation updates
    float SEND_FREQUENCY = 0.05f;
    float prev_send_time;

    public class PlayerData
    {
        public int id;
        public Vector3 position;
        public Vector3 rotation;

        public PlayerData(int id, Vector3 pos, Vector3 rot)
        {
            this.id = id;
            this.position = pos;
            this.rotation = rot;
        }
    }

    public void UpdatePlayerList(PlayerData[] data)
    {
        // Set up unused ids to remove nonexistent players
        List<int> unusedIds = new List<int>();
        foreach (int id in playerList.Keys)
        {
            unusedIds.Add(id);
        }

        // Place or move every player
        foreach (PlayerData d in data)
        {
            if (d.id == myId) continue;
            unusedIds.Remove(d.id);
            Quaternion rot = Quaternion.Euler(d.rotation);

            if (playerList.ContainsKey(d.id))
            {
                // Move the object to the correct point
                playerList[d.id].transform.localPosition = d.position;
                playerList[d.id].transform.localRotation = rot;
            }
            else
            {
                // Spawn an object at the correct point
                GameObject newPlayer = Instantiate(playerPrefab, transform);
                newPlayer.transform.localPosition = d.position;
                newPlayer.transform.localRotation = rot;

                playerList.Add(d.id, newPlayer);
            }
        }

        // Remove objects associated with unused ids
        foreach (int id in unusedIds)
        {
            if (!playerList.ContainsKey(id)) continue;
            Destroy(playerList[id]);
        }
    }

    public void UpdatePositionRotation(Vector3 pos, Vector3 rot)
    {
        myPosition = pos;
        myRotation = rot;
    }

    private void Awake()
    {
        prev_send_time = Time.time;
    }

    // Periodically send updates with our position and rotation
    private void FixedUpdate()
    {
        if (prev_send_time+SEND_FREQUENCY < Time.time)
        {
            tc.SendJson(JsonConvert.SerializeObject(new PlayerData(myId, myPosition, myRotation)));
            prev_send_time = Time.time;
        }
    }
}
