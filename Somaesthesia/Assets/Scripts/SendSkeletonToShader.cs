using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nuitrack;
using nuitrack.device;
using NuitrackSDK;
using NuitrackSDK.Avatar;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;
using Joint = nuitrack.Joint;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public struct Joints
{
    public Vector3 Pos;
    public float3x3 Matrice;
    public float Size;
}

public class SendSkeletonToShader : MonoBehaviour
{
    private Skeleton _skeleton;
    private BoundingBox _boxUser;
    private ComputeBuffer _buffer;
    private ComputeBuffer _bufferMove;
    private ReceiveLabelsValue _data;
    private List<Vector4> listZero = new List<Vector4>();
    [SerializeField] private Material matClear;
    [SerializeField] private Color col;
    nuitrack.JointType[] _jointsInfo = new nuitrack.JointType[]
    {
        nuitrack.JointType.Head,
        nuitrack.JointType.LeftCollar,
        nuitrack.JointType.LeftShoulder,
        nuitrack.JointType.LeftElbow,
        nuitrack.JointType.LeftWrist,
        nuitrack.JointType.RightCollar,
        nuitrack.JointType.RightShoulder,
        nuitrack.JointType.RightElbow,
        nuitrack.JointType.RightWrist,
        nuitrack.JointType.Waist,
        nuitrack.JointType.LeftHip,
        nuitrack.JointType.LeftKnee,
        nuitrack.JointType.LeftAnkle,
        nuitrack.JointType.LeftFoot,
        nuitrack.JointType.RightHip,
        nuitrack.JointType.RightKnee,
        nuitrack.JointType.RightAnkle,
        nuitrack.JointType.RightFoot,
    };
    Joints[] jointsList;
    private int _id = -1;
    private Camera cam;
    [SerializeField] private int maxMove = 15;
    [SerializeField] private float sizeSkeleton = 0.25f;
    [SerializeField] private float maxSkeleton = 15;
    private RenderTexture tmpTex = null;
    // [SerializeField] private MeshFilter meshBubble;
    // [SerializeField] private Material matBubble;
    // [SerializeField] private float scaleBubbles = 0.25f;
    // private Vector3 posMax;
    // private Vector3 posMin;
    // private Vector3 center;

    // Start is called before the first frame update
    void Start()
    {
        _data = GetComponent<ReceiveLabelsValue>();
        jointsList = new Joints[_jointsInfo.Length];
        cam = Camera.main;
        NuitrackManager.onUserTrackerUpdate += UserTrackerOnOnUpdateEvent;
        NuitrackManager.SkeletonTracker.OnSkeletonUpdateEvent += SkeletonTrackerOnOnSkeletonUpdateEvent;
        for (int i = 0; i < maxMove; i++)
        {
            listZero.Add(Vector4.zero);
        }
    }

    private void UserTrackerOnOnUpdateEvent(UserFrame frame)
    {
        if (frame.NumUsers > 0 )
        {
            _id = frame.Users[0].ID;
        }
    }

    public static double StandardDeviation(IEnumerable<float> values)
    {
        double avg = values.Average();
        return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
    }
    
    private float CalculateStdDev(float[] list)
    {
        float mean = 0;
        for (int i = 0; i < list.Length; i++)
        {
            mean += list[i] * 10f;
        }
        mean /= list.Length;
        float[] squareDist = new float[list.Length];    
        for (int i = 0; i < list.Length; i++)
        {
            squareDist[i] = Mathf.Pow(Mathf.Abs(list[i] * 10f - mean), 2);
        }
        float val = 0;
        for (int i = 0; i < squareDist.Length; i++)
        {
            val += squareDist[i];
        }
        val /= squareDist.Length;
        return (Mathf.Sqrt(val));
    }
    
    float Rand(int val, Vector3 pos)
    {
        if (val == 0)
        {
            return Mathf.Cos(Time.time);
        }
        else if (val == 1)
        {
            return Mathf.Sin(Time.time);
        }
        else
        {
            return Mathf.Cos(Time.time) * Mathf.Sin(Time.time);
        }
    }
    
    private void Update()
    {
        if (_data.ResultsDone)
        {
            float val = 50;
            val -= (float)StandardDeviation(_data.ValIA);
            // val += 0.05f;
            // val *= _data.ValIA.Length;
            sizeSkeleton = val / maxSkeleton;
            _data.ResultsDone = false;
        }

        if (_data.MoveDone)
        {
            if (_bufferMove == null)
            {
                _bufferMove = new ComputeBuffer(maxMove, sizeof(float) * 4);
                matClear.SetBuffer("_UVs", _bufferMove);
            }

            int nb = Mathf.Min(_data.PointMove.Count, maxMove);
            _bufferMove.SetData(_data.PointMove, 0, 0, nb);
            if (nb < maxMove)
            {
                _bufferMove.SetData(listZero, 0, nb, maxMove - nb);
            }

            _data.MoveDone = false;
        }
/*
        Random.InitState(42);
        for (int i = 0; i < jointsList.Length; i++)
        {
            int size = Mathf.CeilToInt(sizeSkeleton * 5);
            Matrix4x4[] matrices = new Matrix4x4[size];
            Vector3 pos = jointsList[i].Pos;
            for (int j = 0; j < size; j++)
            {
                matrices[j].m00 = scaleBubbles;
                matrices[j].m11 = scaleBubbles;
                matrices[j].m22 = scaleBubbles;
                matrices[j].SetColumn(3, new Vector4(pos.x + sizeSkeleton * (-0.3f + 0.3f * Random.value) * Rand(Random.Range(0,3), pos),
                    pos.y + sizeSkeleton * (-0.3f +  0.3f * Random.value) * Rand(Random.Range(0,3), pos), pos.z +
                    sizeSkeleton * 0.3f * Random.value * Rand(Random.Range(0,3), pos), 1f));
            }
            Graphics.DrawMeshInstanced(meshBubble.sharedMesh, 0, matBubble, matrices);
        }
        */
    }

    
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        col.a = EasingFunction.EaseInCubicD(0.25f, 1f, (1 - sizeSkeleton / maxSkeleton));
        matClear.color = col;
        if (tmpTex == null)
        {
            tmpTex = new RenderTexture(Screen.width, Screen.height, src.depth, src.graphicsFormat);
        }
        Graphics.Blit(src, tmpTex, matClear, 0);
        Graphics.Blit(tmpTex, dest, matClear, 1);
    }

    private void OnDestroy()
    {
        _buffer?.Release();
        _bufferMove?.Release();
        // NuitrackManager.SkeletonTracker.OnSkeletonUpdateEvent -= SkeletonTrackerOnOnSkeletonUpdateEvent;
        NuitrackManager.onUserTrackerUpdate -= UserTrackerOnOnUpdateEvent;
    }

    private void SkeletonTrackerOnOnSkeletonUpdateEvent(SkeletonData skeletondata)
    {
        if (_id == -1)
        {
            return;
        }
        _skeleton = skeletondata.GetSkeletonByID(_id);
        if (_buffer == null)
        {
            _buffer = new ComputeBuffer(_jointsInfo.Length, sizeof(float) * 13);
        }
        if (_skeleton != null)
        {
            Vector3 posCam = cam.transform.position;
            // posMax = Vector3.negativeInfinity;
            // posMin = Vector3.positiveInfinity;
            // center = Vector3.zero;
            for (int i = 0; i < _jointsInfo.Length; i++)
            {
                Joints newJoint = new Joints();
                Joint joint = _skeleton.GetJoint(_jointsInfo[i]);
                float3x3 matrice = new float3x3();
                matrice.c0 = new float3(joint.Orient.Matrix[0], joint.Orient.Matrix[1], joint.Orient.Matrix[2]); 
                matrice.c1 = new float3(joint.Orient.Matrix[3], joint.Orient.Matrix[4], joint.Orient.Matrix[5]); 
                matrice.c2 = new float3(joint.Orient.Matrix[6], joint.Orient.Matrix[7], joint.Orient.Matrix[8]); 
                newJoint.Matrice = math.inverse(matrice);
                newJoint.Pos = joint.Real.ToVector3();
                newJoint.Pos = new Vector3(posCam.x - newJoint.Pos.x / 450f, posCam.y + newJoint.Pos.y / 450f,
                    posCam.z - newJoint.Pos.z / 650f);
                newJoint.Size = sizeSkeleton;
                // if (_jointsInfo[i] == JointType.Waist)
                // {
                    // center = newJoint.Pos;
                // }
                // posMax = new Vector3(Mathf.Max(posMax.x, newJoint.Pos.x), Mathf.Max(posMax.y,
                    // newJoint.Pos.y), Mathf.Max(posMax.z, newJoint.Pos.z));
                // posMin = new Vector3(Mathf.Min(posMin.x, newJoint.Pos.x), Mathf.Min(posMin.y,
                    // newJoint.Pos.y), Mathf.Min(posMin.z, newJoint.Pos.z));
                jointsList[i] = newJoint;
            }
            _buffer.SetData(jointsList);
            Shader.SetGlobalBuffer("_Skeleton", _buffer);
            Shader.SetGlobalFloat("_SkeletonSize", sizeSkeleton);
        }
    }
}
