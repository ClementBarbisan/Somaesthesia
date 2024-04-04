using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using System.Threading;
using UnityEngine.Serialization;

public class ReceiveLabelsValue : MonoBehaviour
{
    Thread mThread;
    public string connectionIP = "127.0.0.1";
    public int connectionPort = 25001;
    IPAddress localAdd;
    TcpListener listener;
    TcpClient client;
    bool running;
    private bool quit;
    [HideInInspector]
    public float[] ValIA;
    [HideInInspector]
    public string[] TextIA;
    [HideInInspector]
    public List<Vector4> PointMove = new List<Vector4>();
    private bool _results = false;
    [HideInInspector]
    public bool ResultsDone = false;
    private bool _move = false;
    [HideInInspector]
    public bool MoveDone = false;
    private int _textIndex = 0;
    private int _valIndex = 0;
    private void Start()
    {
        for (int i = 0; i < Display.displays.Length; i++)
        {
            Display.displays[i].Activate();
        }
        ThreadStart ts = new ThreadStart(GetInfo);
        mThread = new Thread(ts);
        mThread.Start();
    }

    void GetInfo()
    {
        localAdd = IPAddress.Parse(connectionIP);
        listener = new TcpListener(localAdd, connectionPort);
        listener.Start();
        client = listener.AcceptTcpClient();
        while (!client.Connected)
        {
            
        }
        running = true;
        while (running && client.Connected)
        {
            SendAndReceiveData();
        }
    }

    void OnApplicationQuit()
    {
        listener?.Stop();
        client?.Close();
    }

    void ComputeStrings(string dataReceived)
    {
        if (dataReceived == "##")
        {
            if (_results)
            {
                _results = false;
                ResultsDone = true;
            }
            else
            {
                _move = false;
                MoveDone = true;
            }
        }
        if (_results)
        {
            if (ValIA == null || TextIA == null)
            {
                int nb = int.Parse(dataReceived);
                ValIA = new float[nb];
                TextIA = new string[nb];
                _textIndex = 0;
                _valIndex = 0;
            }
            else if (float.TryParse(dataReceived, out float val))
            {
                ValIA[_valIndex] = val;
                _valIndex++;
            }
            else
            {
                TextIA[_textIndex] = dataReceived;
                _textIndex++;
            }
        }
        if (_move)
        {
            string[] values = dataReceived.Split(",");
            PointMove.Add(new Vector4(float.Parse(values[0]), float.Parse(values[1]),
                float.Parse(values[2]),float.Parse(values[3])));
        }
        if (dataReceived == "results" && ResultsDone == false)
        {
            ValIA = null;
            TextIA = null;
            _results = true;
        }
        if (dataReceived == "contours" && MoveDone == false)
        {
            PointMove.Clear();
            _move = true;
        }
    }

    void SendAndReceiveData()
    {
        try
        {
            NetworkStream nwStream = client.GetStream();
            byte[] buffer = new byte[client.ReceiveBufferSize];
            //---receiving Data from the Host----
            int bytesRead = nwStream.Read(buffer, 0, client.ReceiveBufferSize); //Getting data in Bytes from Python
            string dataReceived = Encoding.UTF8.GetString(buffer, 0, bytesRead); //Converting byte data to string
            nwStream.Write(Encoding.ASCII.GetBytes("Received"));
            // Debug.Log(dataReceived);
            ComputeStrings(dataReceived);
        }
        catch (IOException e)
        {
            quit = true;
        }    
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            quit = true;
        }
        if (quit)
        {
            Application.Quit();
        }

    }
}