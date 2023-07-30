using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using nuitrack;
using nuitrack.device;
using NuitrackSDK;
using NuitrackSDK.Avatar;
using TMPro;
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

    [SerializeField] private float _speed = 5f;
    [SerializeField] private TextMeshProUGUI _prefabText;
    [SerializeField] private Canvas _parentCanvas;

    private RectTransform _rectTr;
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
        _rectTr = _parentCanvas.GetComponent<RectTransform>();
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
        else
        {
            _id = -1;
        }
    }

    public static double StandardDeviation(IEnumerable<float> values)
    {
        double avg = values.Average();
        return Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));
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
        PointCloudGPU.Instance.matPointCloud.SetInt("_Offset", Mathf.Clamp(((int)maxSkeleton
                                                                           - (int)sizeSkeleton) * 2, 2, (int)maxSkeleton * 2));
        if (_id == -1)
        {
            if (_bufferMove != null)
            {
                _bufferMove.SetData(listZero, 0, 0, maxMove);
            }
            sizeSkeleton = 0;
            return;
        }

        if (_data.ResultsDone)
        {
            float val = Mathf.Clamp(80 - (_data.ValIA.Max() - _data.ValIA.Min()), 0, 80);
            float[] arrayVal = _data.ValIA.Where(x => Mathf.Abs(x - _data.ValIA.Min()) > 10 &&
                    Mathf.Abs(x - _data.ValIA.Max()) > 1).ToArray();
            val *= Mathf.Clamp(arrayVal.Length, 1, 100);
            // val += 0.05f;
            // val *= _data.ValIA.Length;
            if (val >= sizeSkeleton)
            {
                sizeSkeleton = Mathf.Lerp(sizeSkeleton, val / maxSkeleton, Time.deltaTime  * (1 /_speed));
            }
            else
            {
                sizeSkeleton = Mathf.Lerp(sizeSkeleton, val / maxSkeleton, 0.05f * (1 / _speed));
            }

            if (sizeSkeleton / maxSkeleton < 1f)
            {
                for (int j = 0; j < _data.TextIA.Length; j++)
                {
                    for (int i = 0; i < Mathf.CeilToInt((int)(maxSkeleton - (maxSkeleton - val)) / 10); i++)
                    {
                        TextMeshProUGUI obj = Instantiate(_prefabText, _parentCanvas.transform);
                        obj.transform.localPosition = new Vector3(Random.Range(-0.5f, 0.5f) * _rectTr.sizeDelta.x,
                            Random.Range(-0.5f, 0.5f) * _rectTr.sizeDelta.y, 0);
                        obj.text = _data.TextIA[j];
                        Color col = obj.color;
                        col.a = val / maxSkeleton / 5;
                        obj.color = col;
                    }
                }

               
            }

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

    private void OnPreRender()
    {
        col.a = Mathf.Clamp(EasingFunction.EaseInCubicD(0.1f, 1f, (1 - sizeSkeleton / maxSkeleton)),
            0.1f, 1f);
        matClear.color = col;
        Graphics.Blit(tmpTex, matClear, 1);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        Graphics.Blit(src, dest, matClear, 0);
        Graphics.Blit(src, tmpTex, matClear, 0);
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
