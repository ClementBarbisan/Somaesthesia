using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SegmentPaint : MonoBehaviour
{
    //public int samples;
    ComputeBuffer segmentBuffer;
    int[] outSegment;

    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += ColorizeUser;
        NuitrackManager.DepthSensor.SetMirror(true);
    }

    void OnDestroy()
    {
        segmentBuffer?.Release();
        NuitrackManager.onUserTrackerUpdate -= ColorizeUser;
    }

    void ColorizeUser(nuitrack.UserFrame frame)
    {
        int cols = frame.Cols;
        int rows = frame.Rows;
        if (segmentBuffer == null)
        {
            segmentBuffer = new ComputeBuffer(cols * rows, 4);
            outSegment = new int[cols * rows]; 
            PointCloudGPU.Instance.matPointCloud.SetBuffer("segmentBuffer", segmentBuffer);
        }
        for (int i = 0; i < (cols * rows); i++)
        {
            outSegment[i] = 0;
            if (frame[i] == 1)
            {
                outSegment[i] = 1;
            }
        }
        segmentBuffer.SetData(outSegment);
    }  
}