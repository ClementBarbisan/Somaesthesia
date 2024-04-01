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
using UnityEngine.Serialization;
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
    // private ComputeBuffer _bufferMove;
    private ReceiveLabelsValue _data;
    // private List<Vector4> listZero = new List<Vector4>();
    // [SerializeField] private Material matClear;
    // [SerializeField] private Color col;

    [Serializable]
    struct Labels
    {
        public List<Label> labels;

        public bool Contains(string val)
        {
            foreach (Label lab in labels)
            {
                if (lab.label == val)
                {
                    return (true);
                }
            }

            return (false);
        }

        public int GetIndex(string val)
        {
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i].label == val)
                {
                    return (i);
                }
            }

            return (-1);
        }

        public string this[int index]
        {
            get
            {
                if (labels[index].values.Count > 0)
                    return (labels[index].values[Random.Range(0, labels[index].values.Count)]);
                return (labels[index].label);
            }

            set
            {
                Label label = labels[index];
                label.values.Add(value);
            }
        }

        public string this[string key]
        {
            get
            {
                foreach (Label lab in labels)
                {
                    if (lab.label == (string) key && lab.values.Count > 0)
                        return (lab.values[Random.Range(0, lab.values.Count)]);
                }

                return (key);
            }

            set
            {
                foreach (Label lab in labels)
                {
                    Label label = lab;
                    if (label.label == (string) key)
                        label.values.Add(value);
                }
            }
        }
    }

    [Serializable]
    class Label
    {
        public string label;
        public List<string> values = new List<string>();

    }

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
    private Camera _cam;
    // [SerializeField] private int maxMove = 15;
    [SerializeField] public float sizeSkeleton = 0.25f;
    [SerializeField] public float maxSkeleton = 15;
    // private RenderTexture tmpTex = null;

    [SerializeField] private float _speed = 5f;
    [SerializeField] private TextMeshProUGUI _prefabText;
    [SerializeField] private Canvas _parentCanvas;

    private RectTransform _rectTr;

    private List<TextMeshProUGUI> _listTexts = new List<TextMeshProUGUI>();

    // private int _currentFrame = 0;
    [FormerlySerializedAs("audioSource")] [SerializeField]
    private AudioSource audioSourceFirst;

    [SerializeField] private AudioSource audioSourceSecond;
    [SerializeField] private float _maxIntensity = 10;
    [SerializeField] private float _maxRand = 2.5f;
    [SerializeField] private float _speedAlpha = 3;
    [SerializeField] private bool debug;
    [SerializeField] private Labels _labelsPos;
    [SerializeField] private Labels _labelsNeg;
    // private Texture2D _texClear;

    // [SerializeField] private Material _matClear;

    private Camera _mainCamera;
    // [SerializeField] private List<string> _positiveLabels;
    //
    // [SerializeField] private List<string> _negativeLabels;

    // Start is called before the first frame update
    void Start()
    {
        _mainCamera = Camera.main;
        Cursor.visible = false;
        // _texClear = new Texture2D(Screen.width, Screen.height);
        // for (int i = 0; i < _texClear.width; i++)
        // {
        //     for (int j = 0; j < _texClear.height; j++)
        //     {
        //         _texClear.SetPixel(i, j, Color.black);
        //     }
        // }

        // RenderPipelineManager.beginCameraRendering += OnPreRenderCamera;
        RenderPipelineManager.endCameraRendering += OnRenderCamera;
        jointsList = new Joints[_jointsInfo.Length];
        _data = GetComponent<ReceiveLabelsValue>();
        _rectTr = _parentCanvas.GetComponent<RectTransform>();
        // jointsList = new Joints[_jointsInfo.Length];
        _cam = Camera.main;
        NuitrackManager.onUserTrackerUpdate += UserTrackerOnOnUpdateEvent;
        NuitrackManager.SkeletonTracker.OnSkeletonUpdateEvent += SkeletonTrackerOnOnSkeletonUpdateEvent;
        // for (int i = 0; i < maxMove; i++)
        // {
        //     listZero.Add(Vector4.zero);
        // }

        audioSourceFirst.volume = 0.5f;
        audioSourceSecond.volume = 0f;
        audioSourceFirst.Play();
        audioSourceSecond.Play();
        PointCloud.Instance.contours.SetVector("_CamPos", _cam.transform.position);
    }

    private void OnDisable()
    {
        // RenderPipelineManager.beginCameraRendering -= OnPreRenderCamera;
        RenderPipelineManager.endCameraRendering -= OnRenderCamera;
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
        PointCloud.Instance.contours.SetInt("_Offset", Mathf.Clamp((int) (maxSkeleton
            - sizeSkeleton) * 2, 2, (int) maxSkeleton * 2));
        PointCloud.Instance.vfx.SetFloat(Shader.PropertyToID("FieldIntensity"), sizeSkeleton / maxSkeleton * _maxIntensity);
        PointCloud.Instance.vfx.SetFloat(Shader.PropertyToID("RandLive"),0.5f + sizeSkeleton / maxSkeleton * _maxRand);
        PointCloud.Instance.vfx.SetFloat(Shader.PropertyToID("Alpha"),sizeSkeleton / maxSkeleton * _speedAlpha);
        Shader.SetGlobalFloat("_SkeletonSize", sizeSkeleton);
        if (_id == -1 && !debug)
        {
            // if (_bufferMove != null)
            // {
            //     _bufferMove.SetData(listZero, 0, 0, maxMove);
            // }
            sizeSkeleton = 0;
            audioSourceFirst.volume = 0.5f;
            audioSourceSecond.volume = 0f;
            // character.SetActive(false);
            return;
        }
        // character.SetActive(true);
        if (_data.ResultsDone)
        {
            int index = -1;
            float max = 0;
            for (int i = 0; i < _data.ValIA.Length; i++)
            {
                if (_data.ValIA[i] > max && (_labelsPos.Contains(_data.TextIA[i]) && _data.ValIA[i] > 60f) || 
                    (_labelsNeg.Contains(_data.TextIA[i]) && _data.ValIA[i] > 40f))
                {
                    index = i;
                    max = _data.ValIA[i];
                }
            }

            if (index != -1 && _labelsPos.Contains(_data.TextIA[index]))
            {
                sizeSkeleton += Time.deltaTime * (1 / _speed) * maxSkeleton * Mathf.Clamp01(_data.ValIA[index] / 100);
            }
            else if (index != -1 && _labelsNeg.Contains(_data.TextIA[index]))
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

            if (_prefabText != null)
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

                    if (_labelsPos.Contains(_data.TextIA[j]))
                    {
                        _listTexts[j].text = (_labelsPos[_data.TextIA[j]] != String.Empty ? _labelsPos[_data.TextIA[j]] : _data.TextIA[j]);
                    }   
                    else if (_labelsNeg.Contains(_data.TextIA[j]))
                    {
                        _listTexts[j].text = (_labelsNeg[_data.TextIA[j]] != String.Empty ? _labelsNeg[_data.TextIA[j]] : _data.TextIA[j]);
                    }
                    if (_data.ValIA[j] < 5f)
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
        audioSourceFirst.volume = Mathf.Clamp(0.5f - Mathf.Pow(sizeSkeleton + 1, 1.2f) / maxSkeleton, 0, 0.5f);
        audioSourceSecond.volume = Mathf.Clamp(Mathf.Pow(sizeSkeleton + 1, 1.25f) / maxSkeleton, 0, 1f);

        // if (_data.MoveDone)
        // {
        //     _data.MoveDone = false;
        // }

    }

    // private void OnAudioFilterRead(float[] data, int channels)
    // {
    //     System.Random rng = new System.Random();
    //     int dataLen = data.Length / channels;
    //     float average = data.Sum() / (dataLen / 5);
    //     int n = 0;
    //     while (n < dataLen)
    //     {
    //         int i = 0;
    //         while (i < channels)
    //         {
    //             data[n * channels + i] = average * (1 - sizeSkeleton/maxSkeleton) + data[n * channels + i] * (sizeSkeleton / maxSkeleton);
    //             i++;
    //         }
    //         n++;
    //     }
    // }

    // private void OnPreRenderCamera(ScriptableRenderContext scriptableRenderContext, Camera camera1)
    // {
        // if (camera1 != _mainCamera)
        // {
            // return;
        // }
        // Color alpha = _matClear.color;
        // alpha.a = 1f;//1f - sizeSkeleton / maxSkeleton;
        // _matClear.color = alpha;
        // Graphics.Blit(_texClear, RenderTexture.active, _matClear);
        // CommandBuffer cmd = CommandBufferPool.Get();
        // cmd.Blit(_texClear,  BuiltinRenderTextureType.CameraTarget, _matClear);
        // scriptableRenderContext.ExecuteCommandBuffer(cmd);
        // cmd.Clear();
        
        // CommandBufferPool.Release(cmd);
    // }

    private void OnRenderCamera(ScriptableRenderContext scriptableRenderContext, Camera camera1)
    {
        if (_id != -1 && camera1 == _mainCamera)
        {
            PointCloud.Instance.contours.SetPass(1);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, 18);
        }
    }

    private void OnDestroy()
    {
        _buffer?.Release();
        // _bufferMove?.Release();
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
            Vector3 posCam = _cam.transform.position;
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
                jointsList[i] = newJoint;
            }

            _buffer.SetData(jointsList); //, 0, jointsList.Length * _currentFrame, jointsList.Length);
            // _currentFrame = (_currentFrame + 1) % PointCloudGPU.maxFrameDepth;
        }
    }
}