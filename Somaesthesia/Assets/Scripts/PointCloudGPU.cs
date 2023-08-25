using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Serialization;
using Debug = UnityEngine.Debug;
using Random = UnityEngine.Random;

public class PointCloudGPU : MonoBehaviour {

    public static PointCloudGPU Instance;
    public Material matPointCloud;
    public Material matTriangles;
    public Material matCurlNoise;
    public ComputeShader curlNoise;
    public ComputeShader fall;
    public const int maxFrameDepth = 15;
    private short[] _particles;
    ComputeBuffer _buffer;
    ComputeBuffer _particleBuffer;
    Texture2D _texture;
    int _width = 0;
    int _height = 0;
    private Camera _cam;
    private int _indexDepth = 0;
    private int _instanceCount;
    private Particle[] _particlesCurl;
    private int _kernelCurl;
    private int _kernelFall;

    [SerializeField] private bool computeCurl;
    [SerializeField] private bool cubes;
    [SerializeField] private bool particles;
    [FormerlySerializedAs("curlNoiseActive")] [SerializeField] private bool computeShader;
    [SerializeField] private bool contours;
    [SerializeField] private bool triangles;
    [SerializeField] private float speedCurl;
    [SerializeField] private bool debug;

    // private ComputeBuffer _curlMatParticles;
    struct Particle
    {
        public Vector3 position;
        Vector3 velocity;
        float life;
    };
    
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
            matTriangles.SetTexture("_MixTex", _texture);
            matTriangles.SetInt("_WidthTex", frame.Cols);
            matTriangles.SetInt("_HeightTex", frame.Rows);
            matCurlNoise.SetTexture("_MixTex", _texture);
            matCurlNoise.SetInt("_WidthTex", frame.Cols);
            matCurlNoise.SetInt("_HeightTex", frame.Rows);
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
                _particlesCurl = new Particle[cols * rows];
                _buffer = new ComputeBuffer(cols * rows * maxFrameDepth, sizeof(float));
                _particleBuffer = new ComputeBuffer(cols * rows, sizeof(float) * 7);
                Graphics.SetRandomWriteTarget(1, _particleBuffer, true);
                _particleBuffer.SetData(_particlesCurl);
                _width = cols;
                _height = rows;
                matPointCloud.SetVector("_CamPos", _cam.transform.position);
                matTriangles.SetVector("_CamPos", _cam.transform.position);
                matTriangles.SetBuffer("particleBuffer", _buffer);
                matPointCloud.SetBuffer("particleBuffer", _buffer);
                matCurlNoise.SetBuffer("depth", _buffer);
                matPointCloud.SetInteger("_Width", _width);
                matPointCloud.SetInteger("_Height", _height);
                matTriangles.SetInteger("_Width", _width);
                matTriangles.SetInteger("_Height", _height);
                _instanceCount = _width * _height;
                _kernelCurl = curlNoise.FindKernel("CSParticle");
                _kernelFall = fall.FindKernel("CSParticle");
                curlNoise.SetBuffer(_kernelCurl, "positionBuffer", _buffer);
                curlNoise.SetBuffer(_kernelCurl, "particleBuffer", _particleBuffer);
                curlNoise.SetInt("_Height", _height);
                curlNoise.SetInt("_Width", _width);
                curlNoise.SetVector("_CamPos", _cam.transform.position);
                curlNoise.SetFloat("deltaTime", Time.deltaTime);
                fall.SetBuffer(_kernelCurl, "positionBuffer", _buffer);
                fall.SetBuffer(_kernelCurl, "particleBuffer", _particleBuffer);
                fall.SetInt("_Height", _height);
                fall.SetInt("_Width", _width);
                fall.SetVector("_CamPos", _cam.transform.position);
                fall.SetFloat("deltaTime", Time.deltaTime);
                matCurlNoise.SetBuffer("particles", _particleBuffer);
                matTriangles.SetBuffer("particles", _particleBuffer);
                matCurlNoise.SetInt("_Height", _height);
                matCurlNoise.SetInt("_Width", _width);
                matCurlNoise.SetVector("_CamPos", _cam.transform.position);
                Shader.SetGlobalInteger("_MaxFrame", maxFrameDepth);
            }
            Marshal.Copy(frame.Data, _particles, 0, _instanceCount);
            // void* managedBuffer = UnsafeUtility.AddressOf(ref _particles[0]);
            // UnsafeUtility.MemCpy(managedBuffer, (void *)frame.Data, frame.DataSize);
            _buffer.SetData(_particles, 0, _instanceCount * _indexDepth, _instanceCount);
            // matPointCloud.SetInt("_CurrentFrame", _indexDepth);// == 0 ? maxFrameDepth - 1 : _indexDepth - 1);
            _indexDepth = (_indexDepth + 1) % maxFrameDepth;
        }
    }


    private void DispatchCurlNoise()
    {
        if (_buffer != null)
        {
            if (debug)
            {
                curlNoise.SetFloat("_speed", speedCurl);
            }
            curlNoise.Dispatch(_kernelCurl, 1200, 1 ,1);
        }
    }
    
    private void DispatchFall()
    {
        if (_buffer != null)
        {
            if (debug)
            {
                fall.SetFloat("_speed", speedCurl);
            }
            fall.Dispatch(_kernelFall, 1200, 1 ,1);
        }
    }

    private void Update()
    {
        if (computeCurl)
        {
            DispatchCurlNoise();
        }
        else
        {
            DispatchFall();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }
    
    private void OnRenderObject()
    {
        if (cubes)
        {
            matPointCloud.SetPass(1);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, 18);
        }
        if (particles)
        {
            matPointCloud.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, _instanceCount);
        }
        if (computeShader)
        {
            matCurlNoise.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, _instanceCount);
        }

        if (triangles)
        {
            matTriangles.SetPass(0);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, _instanceCount);
        }

        // matPointCloud.SetPass(3);
        // Graphics.DrawProceduralNow(MeshTopology.Points, 1, _instanceCount);
        if (contours)
        {
            matPointCloud.SetPass(2);
            Graphics.DrawProceduralNow(MeshTopology.Points, 1, _instanceCount);
        }
    }
    
    private void OnDestroy()
    {
        _buffer?.Release();
        _particleBuffer?.Release();
    }
}
