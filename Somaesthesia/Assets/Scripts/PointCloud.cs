using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using nuitrack;
using TMPro.EditorUtilities;
using Unity.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class PointCloud : MonoBehaviour
{
    [DllImport("kernel32.dll", EntryPoint = "CopyMemory", SetLastError = false)]
    public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);
    
    private GraphicsBuffer _depthBuffer;
    private Texture2D _color;
    [SerializeField]
    private VisualEffect _vfx;

    private float[] _particles;
    private uint _width;

    private uint _height;

    private GraphicsBuffer _segmentBuffer;
    
    private int[] _outSegment;

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
            _vfx.SetGraphicsBuffer(Shader.PropertyToID("Segment"), _segmentBuffer);
            _outSegment = new int[frame.Cols * frame.Rows];
        }

        for (int i = 0; i < frame.Cols * frame.Rows; i++)
        {
            _outSegment[i] = frame[i];
        }

        // Marshal.Copy(frame.Data, _outSegment, 0, frame.Cols * frame.Rows);
        _segmentBuffer.SetData(_outSegment);
    }

    private void HandleOnDepthSensorUpdateEvent(DepthFrame frame)
    {
        if (_depthBuffer == null)
        {
            _depthBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, frame.Cols * frame.Rows, sizeof(float));
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

    private void OnDestroy()
    {
        _segmentBuffer?.Release();
        _depthBuffer?.Release();
    }
}