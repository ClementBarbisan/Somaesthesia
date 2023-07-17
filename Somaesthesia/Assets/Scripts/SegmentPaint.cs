using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
       
        if (_segmentBuffer == null)
        {
            _width = frame.Cols;
            _height = frame.Rows;
            _segmentBuffer = new ComputeBuffer(_width * _height * PointCloudGPU.maxFrameDepth, sizeof(int));
            _outSegment = new int[_width * _height]; 
            PointCloudGPU.Instance.matPointCloud.SetBuffer("segmentBuffer", _segmentBuffer);
        }
        for (int i = 0; i < (_width * _height); i++)
        {
            _outSegment[i] = frame[i];
        }
        // Marshal.Copy(frame.Data, _outSegment, 0, _width * _height);
        _segmentBuffer.SetData(_outSegment, 0, _width * _height * _indexSegment, _width * _height);
        _indexSegment = (_indexSegment + 1) % PointCloudGPU.maxFrameDepth;
        // PointCloudGPU.Instance.matPointCloud.SetInt("_CurrentFrame", _indexSegment);
    }  
}