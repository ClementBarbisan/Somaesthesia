using System.Diagnostics;
using System.Runtime.InteropServices;
using Intel.RealSense.Math;
using Kino;
using NuitrackSDK;
using Unity.Collections;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class PointCloudGPU : MonoBehaviour {

    static public PointCloudGPU Instance;
    public Material matPointCloud;
    public Material matMesh;
    private short[] particles;
    ComputeBuffer buffer;
    Texture2D texture;
    int width = 0;
    int height = 0;
    private Camera cam;

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
        cam = Camera.main;
    }

    void HandleOnColorSensorUpdateEvent(nuitrack.ColorFrame frame)
    {
        if (texture == null)
        {
            nuitrack.OutputMode ouput = NuitrackManager.ColorSensor.GetOutputMode();
            texture = new Texture2D(frame.Cols, frame.Rows, TextureFormat.RGB24, false);
            matPointCloud.SetTexture("_MainTex", texture);
            matPointCloud.SetInt("_WidthTex", frame.Cols);
            matPointCloud.SetInt("_HeightTex", frame.Rows);
        }
        texture.LoadRawTextureData(frame.Data, frame.DataSize);
        texture.Apply();
    }


    // Update is called once per frame
    void HandleOnDepthSensorUpdateEvent(nuitrack.DepthFrame frame) {
        
        if (buffer == null)
        {
            Debug.Log("Initialize compute buffer");
            particles = new short[frame.Cols * frame.Rows];
            buffer = new ComputeBuffer(frame.Cols * frame.Rows, sizeof(float));
            width = frame.Cols;
            height = frame.Rows;
            matPointCloud.SetVector("_CamPos", cam.transform.position);
            matPointCloud.SetBuffer("particleBuffer", buffer);
            matPointCloud.SetInt("_Width", width);
            matPointCloud.SetInt("_Height", height);
          
        }
        Marshal.Copy(frame.Data, particles, 0, frame.Cols * frame.Rows);
        buffer.SetData(particles);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    void OnRenderObject()
    {
        matPointCloud.SetPass(0);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, width * height);
        matPointCloud.SetPass(1);
        Graphics.DrawProceduralNow(MeshTopology.Points, 1, 18);
    }

    private void OnDestroy()
    {
        buffer?.Release();
        // NuitrackManager.DepthSensor.OnUpdateEvent -= HandleOnDepthSensorUpdateEvent;
        // NuitrackManager.ColorSensor.OnUpdateEvent -= HandleOnColorSensorUpdateEvent;
    }
}
