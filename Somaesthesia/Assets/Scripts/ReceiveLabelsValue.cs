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
    IPEndPoint localAdd;
    // TcpListener listener;
    UdpClient client;
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
    [SerializeField]private LaunchProcess _clientTcp;
    private void Start()
    {
       
        ThreadStart ts = new ThreadStart(GetInfo);
        mThread = new Thread(ts);
        mThread.Start();
        _clientTcp.enabled = true;
    }

    void GetInfo()
    {
        localAdd = new IPEndPoint(IPAddress.Any, connectionPort);
        client = new UdpClient(connectionPort);
        running = true;
        while (running)
        {
            SendAndReceiveData();
        }
    }

    void OnApplicationQuit()
    {
        // listener?.Stop();
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
            byte[] buffer = client.Receive(ref localAdd);
            //---receiving Data from the Host----
            string dataReceived = Encoding.UTF8.GetString(buffer);
            byte[] msg = Encoding.ASCII.GetBytes("Received");//Converting byte data to string
            client.Send(msg, msg.Length, localAdd);
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