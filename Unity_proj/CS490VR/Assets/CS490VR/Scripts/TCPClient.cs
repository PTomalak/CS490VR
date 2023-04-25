using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class TCPClient : MonoBehaviour
{
    #region components
    JSONParser jp;
    BlockManager bm;
    #endregion

    #region fields
    private TcpClient socketConnection;
    private Thread clientReceiveThread;

    int PORT = 33333;
    string IP = "localhost";
    #endregion

    ///// ADAPTED FROM BOILERPLATE TCP CLIENT CODE /////

    // Start is called before the first frame update
    void Awake()
    {
        jp = GetComponent<JSONParser>();
        bm = GetComponent<BlockManager>();
        Connect(IP, PORT);
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
        if (!bm) return;

        try
        {
            clientReceiveThread = new Thread(new ThreadStart(ListenForData));
            clientReceiveThread.IsBackground = true;
            clientReceiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.Log("On client connect exception " + e);
        }
    }

    private void ListenForData()
    {
        try
        {
            socketConnection = new TcpClient(IP, PORT);
            Byte[] bytes = new Byte[1024];
            while (true)
            {
                // Get a stream object for reading 				
                using (NetworkStream stream = socketConnection.GetStream())
                {
                    int length;
                    // Read incomming stream into byte arrary. 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        var incommingData = new byte[length];
                        Array.Copy(bytes, 0, incommingData, 0, length);
                        // Convert byte array to string message. 						
                        string serverMessage = Encoding.ASCII.GetString(incommingData);

                        string modifiedMessage = serverMessage.Replace("}{", "}||{");

                        // Split up the server message in case its multiple JSONs
                        foreach (string message in serverMessage.Replace("}{", "}||{").Split("||"))
                        {
                            // Enqueue the next action
                            bm.actions.Enqueue(serverMessage);
                        }
                    }
                }
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("Socket Listen exception: " + socketException);
        }
    }

    public void SendJson(string request)
    {
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
