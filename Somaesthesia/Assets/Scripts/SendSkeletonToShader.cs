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
    [SerializeField] private GameObject character;
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

    Queue<Joints> jointsList = new Queue<Joints>();
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

    private List<TextMeshProUGUI> _listTexts = new List<TextMeshProUGUI>();

    private int _currentFrame = 0;

    [SerializeField] private bool debug;

    [SerializeField] private List<string> _positiveLabels;

    [SerializeField] private List<string> _negativeLabels;

    // Start is called before the first frame update
    void Start()
    {
        _data = GetComponent<ReceiveLabelsValue>();
        _rectTr = _parentCanvas.GetComponent<RectTransform>();
        // jointsList = new Joints[_jointsInfo.Length];
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
        if (frame.NumUsers > 0)
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
        return Math.Sqrt(values.Average(v => Math.Pow(v - avg, 2)));
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
        PointCloudGPU.Instance.matPointCloud.SetInt("_Offset", Mathf.Clamp((int) (maxSkeleton
            - sizeSkeleton) * 2, 2, (int) maxSkeleton * 2));
        PointCloudGPU.Instance.curlNoise.SetFloat("_speed", Mathf.Clamp(sizeSkeleton / maxSkeleton, 0, 0.5f));
        PointCloudGPU.Instance.fall.SetFloat("_speed", Mathf.Clamp(sizeSkeleton / maxSkeleton, 0, 0.5f));
        if (_id == -1 && !debug)
        {
            if (_bufferMove != null)
            {
                _bufferMove.SetData(listZero, 0, 0, maxMove);
            }

            PointCloudGPU.Instance.skeleton = false;
            sizeSkeleton = 0;
            character.SetActive(false);
            return;
        }
        PointCloudGPU.Instance.skeleton = true;
        character.SetActive(true);
        if (_data.ResultsDone)
        {
            int index = 0;
            float max = 0;
            for (int i = 0; i < _data.ValIA.Length; i++)
            {
                if (_data.ValIA[i] > max)
                {
                    index = i;
                    max = _data.ValIA[i];
                }
            }

            if (_positiveLabels.Contains(_data.TextIA[index]))
            {
                sizeSkeleton += Time.deltaTime * (1 / _speed) * maxSkeleton * Mathf.Clamp01(_data.ValIA[index] / 100);
            }
            else if (_negativeLabels.Contains(_data.TextIA[index]))
            {
                sizeSkeleton -= Time.deltaTime * (1 / _speed) * maxSkeleton * 2 * Mathf.Clamp01(_data.ValIA[index] / 100);
            }
            sizeSkeleton = Mathf.Clamp(sizeSkeleton, 0, maxSkeleton);
            // float val = Mathf.Clamp(90 - (_data.ValIA.Max() - _data.ValIA.Min()), 0, 90);
            // float[] arrayVal = _data.ValIA.Where(x => Mathf.Abs(x - _data.ValIA.Min()) > 10 &&
            //         Mathf.Abs(x - _data.ValIA.Max()) > 1).ToArray();
            // val *= Mathf.Clamp(arrayVal.Length, 1, 100);
            // val += 0.05f;
            // val *= _data.ValIA.Length;
            // if (val >= sizeSkeleton)
            // {
            // sizeSkeleton = Mathf.Lerp(sizeSkeleton, val / maxSkeleton, Time.deltaTime * (1 / _speed));
            // }
            // else
            // {
            // sizeSkeleton = Mathf.Lerp(sizeSkeleton, val / maxSkeleton, 0.05f * (1 / _speed));
            // }

            if (sizeSkeleton / maxSkeleton <= 1f && _prefabText != null)
            {
                if (_listTexts == null)
                {
                    _listTexts = new List<TextMeshProUGUI>(_data.TextIA.Length);
                }

                for (int j = 0; j < _data.TextIA.Length; j++)
                {
                    if (_listTexts.Count <= j)
                    {
                        _listTexts.Add(Instantiate(_prefabText, _parentCanvas.transform));
                        _listTexts[^1].rectTransform.offsetMin = Vector2.zero;
                        _listTexts[^1].rectTransform.anchorMin = Vector2.zero;
                        _listTexts[^1].rectTransform.offsetMax = Vector2.zero;
                        _listTexts[^1].rectTransform.anchorMax = Vector2.one;
                    }
                    else if (_listTexts[j].color.a > 0)
                    {
                        continue;
                    }

                    _listTexts[j].text = _data.TextIA[j];
                    if (_data.ValIA[j] < 0.05f)
                    {
                        continue;
                    }

                    _listTexts[j].transform.localPosition = new Vector3(
                                                                Random.Range(-0.5f, 0.5f) * _rectTr.sizeDelta.x / 2,
                                                                Random.Range(-0.5f, 0.5f) * _rectTr.sizeDelta.y / 2,
                                                                0) * Mathf.Clamp01(_data.ValIA[j] / 100)
                                                            - new Vector3(_rectTr.sizeDelta.x / 2,
                                                                _rectTr.sizeDelta.y / 2, 0);
                    _listTexts[j].fontSize = 100 * (Mathf.Clamp01(_data.ValIA[j] / 100));
                    Color color = _listTexts[j].color;
                    color.a = 1 - sizeSkeleton / maxSkeleton / 5;
                    _listTexts[j].color = color;
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

        Shader.SetGlobalFloat("_SkeletonSize", sizeSkeleton);
    }

    private void OnPreRender()
    {
        // matClear.SetFloat("_RandValue", 1.1f - sizeSkeleton / maxSkeleton);
        col.a = Mathf.Clamp(1 - sizeSkeleton / maxSkeleton, 0.02f, 1f);
        matClear.color = col;
        Graphics.Blit(tmpTex, matClear, 1);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        // Graphics.Blit(src, dest, matClear, 0);
        Graphics.Blit(src, tmpTex); //, matClear, 0);
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
            Shader.SetGlobalBuffer("_Skeleton", _buffer);
            Shader.SetGlobalFloat("_MaxSize", maxSkeleton);
        }

        if (_skeleton != null)
        {
            Vector3 posCam = cam.transform.position;
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
                jointsList.Enqueue(newJoint);
            }

            while (jointsList.Count > _jointsInfo.Length)
            {
                jointsList.Dequeue();
            }

            _buffer.SetData(jointsList.ToArray()); //, 0, jointsList.Length * _currentFrame, jointsList.Length);
            // _currentFrame = (_currentFrame + 1) % PointCloudGPU.maxFrameDepth;
        }
    }
}