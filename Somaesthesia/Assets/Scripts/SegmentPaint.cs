using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Intel.RealSense;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class SegmentPaint : MonoBehaviour
{
    //public int samples;
    private int _indexSegment = 0;
    ComputeBuffer _segmentBuffer;
    int[] _outSegment;
    private int _width;
    private int _height;
    // unsafe void* managedBuffer;

    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += ColorizeUser;
        NuitrackManager.DepthSensor.SetMirror(true);
    }

    void OnDestroy()
    {
        _segmentBuffer?.Release();
        NuitrackManager.onUserTrackerUpdate -= ColorizeUser;
    }

    void ColorizeUser(nuitrack.UserFrame frame)
    {
        // unsafe
        {
            if (_segmentBuffer == null)
            {
                _width = frame.Cols;
                _height = frame.Rows;
                _segmentBuffer = new ComputeBuffer(_width * _height * PointCloudGPU.maxFrameDepth, sizeof(int));
                _outSegment = new int[_width * _height]; 
                // managedBuffer = UnsafeUtility.AddressOf(ref _outSegment[0]);
                PointCloudGPU.Instance.matPointCloud.SetBuffer("segmentBuffer", _segmentBuffer);
            }
            for (int i = 0; i < (_width * _height); i++)
            {
               _outSegment[i] = frame[i];
            }
            // UnsafeUtility.MemCpy(managedBuffer, (void *)frame.Data, frame.DataSize);
            _segmentBuffer.SetData(_outSegment, 0, (_width * _height) * _indexSegment, (_width * _height));
            Shader.SetGlobalInteger("_CurrentFrame", _indexSegment);// == 0 ? PointCloudGPU.maxFrameDepth - 1 : _indexSegment - 1);
            _indexSegment = (_indexSegment + 1) % PointCloudGPU.maxFrameDepth;
        }
    }  
}