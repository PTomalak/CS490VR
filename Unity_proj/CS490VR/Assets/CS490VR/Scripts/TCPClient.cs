using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    #region components
    JSONParser jp;
    public PlayerManager pm;
    #endregion

    #region fields
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    bool connected = false;

    int PORT = 39876;
    string IP = "vr.ptomalak.com";
    string FALLBACK_IP = "192.168.118.230";
    #endregion

    ///// ADAPTED FROM BOILERPLATE TCP CLIENT CODE /////

    // Start is called before the first frame update
    void Start()
    {
        jp = GetComponent<JSONParser>();
        Connect(IP, PORT);
    }

    private void Update()
    {
        if (socketConnection != null && !connected)
        {
            pm.InitializePlayer(DateTime.Now.Millisecond.ToString(), Vector3.zero, Vector3.zero);
            connected = true;
        }
    }

    private void OnDestroy()
    {
        if (clientReceiveThread != null)
        {
            clientReceiveThread.Abort();
        }
    }

    public void Connect(string ip, int port)
    {
        IP = ip;
        PORT = port;
        if (!jp) return;

        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("CONNECTION ERR: " + e);
        }
    }

    private void ListenForData()
    {
        try
        {
            try
            {
                socketConnection = new TcpClient(IP, PORT);
            }
            catch (SocketException e)
            {
                socketConnection = new TcpClient(FALLBACK_IP, PORT);
                Debug.Log("CONNECTION: Trying Fallback IP");
            }

            Byte[] bytes = new Byte[1024];
            while (true)
            {
                // Get a stream object for readings
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;

                    string compiledJson = "";

                    // Read incomming stream into byte arrary. 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        // Convert byte array to string message. 						
                        string serverMessage = Encoding.ASCII.GetString(incommingData);

                        // Check if we got multiple JSON objects in one message
                        // String manipulation is poor for performance, but I don't really care
                        compiledJson += serverMessage;
                        int index = compiledJson.IndexOf("}{");
                        while (index >= 0)
                        {
                            // We have collected one json and started a second one
                            string json = compiledJson.Substring(0, index+1);
                            compiledJson = compiledJson[(index + 1)..];

                            // Receive action
                            //Debug.Log("RECEIVE(A): " + json);
                            jp.incomingActions.Enqueue(json);

                            index = compiledJson.IndexOf("}{");
                        }

                        // Check if our overall compiled JSON message is a single JSON object
                        // Also poor for performance, also don't care
                        int opening = compiledJson.Count(t => t == '{');
                        int closing = compiledJson.Count(t => t == '}');
                        if (opening == closing)
                        {
                            // Receive action
                            //Debug.Log("RECEIVE(B): " + compiledJson);
                            jp.incomingActions.Enqueue(compiledJson);

                            compiledJson = "";
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("CONNECTION ERR: " + socketException);
        }
    }

    public void SendJson(string request)
    {
        //Debug.Log("SEND: " + request);

        if (socketConnection == null)
        {
            return;
        }
        try
        {
            // Get a stream object for writing. 			
            NetworkStream stream = socketConnection.GetStream();
            if (stream.CanWrite)
            {
                string clientMessage = request;
                clientMessage += '\0';          // Null terminate client messages
                // Convert string message to byte array.                 
                byte[] clientMessageAsByteArray = Encoding.ASCII.GetBytes(clientMessage);
                // Write byte array to socketConnection stream.                 
                stream.Write(clientMessageAsByteArray, 0, clientMessageAsByteArray.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket Write exception: " + socketException);
        }
    }
}
