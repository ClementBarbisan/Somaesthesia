using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Intel.RealSense.Math;
using Kino;
using NuitrackSDK;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PointCloudGPU : MonoBehaviour {

    static public PointCloudGPU Instance;
    public Material matPointCloud;
    public Material matMesh;
    public const int maxFrameDepth = 20;
    private short[] _particles;
    ComputeBuffer _buffer;
    Texture2D _texture;
    int _width = 0;
    int _height = 0;
    private Camera _cam;
    private int _indexDepth = 0;
    public int instanceCount;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    // Use this for initialization
    void Start () 
    {
        NuitrackManager.DepthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
        NuitrackManager.ColorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;
        Cursor.visible = false;
        _cam = Camera.main;
    }

    void HandleOnColorSensorUpdateEvent(nuitrack.ColorFrame frame)
    {
        if (_texture == null)
        {
            _texture = new Texture2D(frame.Cols, frame.Rows, TextureFormat.RGB24, false);
            matPointCloud.SetTexture("_MixTex", _texture);
            matPointCloud.SetInt("_WidthTex", frame.Cols);
            matPointCloud.SetInt("_HeightTex", frame.Rows);
        }
        _texture.LoadRawTextureData(frame.Data, frame.DataSize);
        _texture.Apply();
    }


    // Update is called once per frame
    void HandleOnDepthSensorUpdateEvent(nuitrack.DepthFrame frame)
    {
        // unsafe
        {
            if (_buffer == null)
            {
                int cols = frame.Cols;
                int rows = frame.Rows;
                Debug.Log("Initialize compute buffer");
                _particles = new short[cols * rows];
                _buffer = new ComputeBuffer(cols * rows * maxFrameDepth, sizeof(float));
                _width = cols;
                _height = rows;
                matPointCloud.SetVector("_CamPos", _cam.transform.position);
                matPointCloud.SetBuffer("particleBuffer", _buffer);
                matPointCloud.SetInt("_MaxFrame", maxFrameDepth);
                matPointCloud.SetInt("_Width", _width);
                matPointCloud.SetInt("_Height", _height);
                instanceCount = _width * _height;
                Debug.Log("width = " + _width + ", height = " + _height);
            }
            Marshal.Copy(frame.Data, _particles, 0, _width * _height);
            // void* managedBuffer = UnsafeUtility.AddressOf(ref _particles[0]);
            // UnsafeUtility.MemCpy(managedBuffer, (void *)frame.Data, frame.DataSize);
            _buffer.SetData(_particles, 0, instanceCount * _indexDepth, instanceCount);
            // matPointCloud.SetInt("_CurrentFrame", _indexDepth);        
            matPointCloud.SetInt("_CurrentFrame", _indexDepth == 0 ? maxFrameDepth - 1 : _indexDepth - 1);
            _indexDepth = (_indexDepth + 1) % maxFrameDepth;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    private void OnRenderObject()
    {
        matPointCloud.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, instanceCount);
        matPointCloud.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, 18);
        matPointCloud.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, instanceCount);
    }
    
    private void OnDestroy()
    {
        _buffer?.Release();
        // NuitrackManager.DepthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
        // NuitrackManager.ColorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
    }
}
