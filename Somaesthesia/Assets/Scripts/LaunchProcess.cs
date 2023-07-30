using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LaunchProcess : MonoBehaviour
{
    [DllImport("user32.dll")] static extern uint GetActiveWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd); 
    // Start is called before the first frame update
    private IntPtr windowUnity;
    Process pr = new Process();
    void OnEnable()
    {
        windowUnity = (IntPtr) GetActiveWindow();
       
        ProcessStartInfo prs = new ProcessStartInfo();
        prs.FileName = "C:\\webcam_action_recognition\\webcam_action_recognition.exe";
        prs.UseShellExecute = true;
        prs.Arguments =
            "C:\\tanet_imagenet-pretrained-r50_8xb8-dense-1x1x8-100e_kinetics400-rgb.py" +
            " C:\\tanet_imagenet-pretrained-r50_8xb8-dense-1x1x8-100e_kinetics400-rgb_20220919-a34346bc.pth" +
            " C:\\label_map_k400.txt --device cuda:0 --inference-fps 60 --image True";
        pr.StartInfo = prs;

        ThreadStart ths = new ThreadStart(() => pr.Start());
        Thread th = new Thread(ths);
        th.Start();

    }

    private void Start()
    {
        SetForegroundWindow(windowUnity);
    }

    // Update is called once per frame
    void Update()
    {
    }
}
