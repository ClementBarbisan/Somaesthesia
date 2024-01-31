using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using nuitrack;
using TMPro.EditorUtilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.VFX;

public class PointCloud : MonoBehaviour
{
    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);

    public static PointCloud Instance;
    private GraphicsBuffer _depthBuffer;
    private Texture2D _color;
    [SerializeField]
    private VisualEffect _vfx;

    private float[] _particles;
    private uint _width;

    private uint _height;

    private GraphicsBuffer _segmentBuffer;
    private ComputeBuffer _segment;
    private ComputeBuffer _depth;
    
    private int[] _outSegment;
    [FormerlySerializedAs("_contours")] [SerializeField] public Material contours;

    private void Awake()
    {
        if (Instance)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        _vfx = GetComponent<VisualEffect>();
        NuitrackManager.DepthSensor.OnUpdateEvent += HandleOnDepthSensorUpdateEvent;
        NuitrackManager.ColorSensor.OnUpdateEvent += HandleOnColorSensorUpdateEvent;    
        NuitrackManager.onUserTrackerUpdate += ColorizeUser;
    }

    private void ColorizeUser(UserFrame frame)
    {
        if (_segmentBuffer == null)
        {
            _segmentBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, frame.Cols * frame.Rows, sizeof(int));
            _segment = new ComputeBuffer(frame.Cols * frame.Rows, sizeof(int));
            contours.SetBuffer("segmentBuffer", _segment);
            contours.SetInt("_WidthTex", frame.Cols);
            contours.SetInt("_HeightTex", frame.Rows);
            _vfx.SetGraphicsBuffer(Shader.PropertyToID("Segment"), _segmentBuffer);
            _outSegment = new int[frame.Cols * frame.Rows];
        }

        for (int i = 0; i < frame.Cols * frame.Rows; i++)
        {
            _outSegment[i] = frame[i];
        }

        // Marshal.Copy(frame.Data, _outSegment, 0, frame.Cols * frame.Rows);
        _segmentBuffer.SetData(_outSegment);
        _segment.SetData(_outSegment);
    }

    private void HandleOnDepthSensorUpdateEvent(DepthFrame frame)
    {
        if (_depthBuffer == null)
        {
            _depthBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, frame.Cols * frame.Rows, sizeof(float));
            _depth = new ComputeBuffer(frame.Cols * frame.Rows, sizeof(float));
            contours.SetBuffer("particleBuffer", _depth);
            contours.SetInt("_Width", frame.Cols);
            contours.SetInt("_Height", frame.Rows);
            _vfx.SetGraphicsBuffer(Shader.PropertyToID("Depth"), _depthBuffer);
            _particles = new float[frame.Cols * frame.Rows];
            _width = (uint) frame.Cols;
            _height = (uint) frame.Rows;
            _vfx.SetUInt(Shader.PropertyToID("Width"), _width);
            _vfx.SetUInt(Shader.PropertyToID("Height"), _height);
        }
        // Marshal.Copy(frame.Data, _particles, 0, (int)(_width * _height));
        for (int i = 0; i < frame.Cols * frame.Rows; i++)
        {
            _particles[i] = frame[i];
        }
        _depthBuffer.SetData(_particles, 0, 0,(int)(_width * _height));
        _depth.SetData(_particles);
    }

    private void HandleOnColorSensorUpdateEvent(ColorFrame frame)
    {
        if (_color == null)
        {
            _color = new Texture2D(frame.Cols, frame.Rows, TextureFormat.RGB24, false);
            _vfx.SetTexture(Shader.PropertyToID("TexColor"), _color);
        }
        _color.LoadRawTextureData(frame.Data, frame.DataSize);
        _color.Apply();
    }

    private void OnRenderObject()
    {
        contours.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, 18);
        contours.SetPass(2);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, (int)(_width * _height));
    }

    private void OnDestroy()
    {
        _segmentBuffer?.Release();
        _depthBuffer?.Release();
        _depth?.Release();
        _segment?.Release();
    }
}