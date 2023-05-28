using Intel.RealSense.Math;
using Kino;
using NuitrackSDK;
using UnityEngine;

public class PointCloudGPU : MonoBehaviour {

    static public PointCloudGPU Instance;
    public Material matPointCloud;
    [HideInInspector]
    public float[] particles;
    ComputeBuffer buffer;
    Texture2D texture;
    int width = 0;
    int height = 0;
    float multiplier = -1f;
    float elapsedTime = 0;
    private Camera cam;

    private void Awake()
    {
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
            particles = new float[frame.Cols * frame.Rows];
            buffer = new ComputeBuffer(frame.Cols * frame.Rows, 12);
            width = frame.Cols;
            height = frame.Rows;
            matPointCloud.SetVector("_CamPos", cam.transform.position);
            matPointCloud.SetBuffer("particleBuffer", buffer);
            matPointCloud.SetInt("_Width", width);
            matPointCloud.SetInt("_Height", height);
          
        }
        for (int i = 0; i < frame.Rows; i++)
        {
            for (int j = 0; j < frame.Cols; j++)
            {
                particles[i * frame.Cols + j] = frame[i, j];
            }
        }
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
        // matPointCloud.SetPass(1);
        // Graphics.DrawProceduralNow(MeshTopology.Points, 1, width * height);
    }

    private void OnDestroy()
    {
        buffer.Release();
    }
}
