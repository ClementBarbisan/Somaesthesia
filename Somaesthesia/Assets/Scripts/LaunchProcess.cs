using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Xml;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class LaunchProcess : MonoBehaviour
{
    [DllImport("user32.dll")] static extern uint GetActiveWindow();
    [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr hWnd); 
    // Start is called before the first frame update
    private IntPtr windowUnity;
    Process pr = new Process();
    [SerializeField] private string _nameFile = "model.xml";
    [SerializeField] private string _nameProcess = "tanet_imagenet-pretrained-r50_8xb8-dense-1x1x8-100e_kinetics400-rgb";
    [SerializeField] private string _nameLabels = "label_map_k400";
    [SerializeField] private bool image;
    private StreamWriter outputStream;
    void OnEnable()
    {
        outputStream = new StreamWriter("C:\\log.txt");
        XmlDocument doc = new XmlDocument();
        doc.Load(Application.streamingAssetsPath + "\\" + _nameFile);
        XmlNode node = doc.FirstChild;
        _nameProcess = node.SelectSingleNode("model").InnerText;
        _nameLabels = node.SelectSingleNode("labels").InnerText;
        windowUnity = (IntPtr) GetActiveWindow();
       
        ProcessStartInfo prs = new ProcessStartInfo();
        prs.FileName = "C:\\movement\\movement.exe";
        prs.UseShellExecute = false;
        prs.Verb = "runas";
        prs.Arguments =
            "C:\\" + _nameProcess + ".py" +
            " C:\\" + _nameProcess + ".pth" +
            " C:\\" + _nameLabels + ".txt --device cuda:0";
        if (image)
        {
            prs.Arguments += " --image True";
        }

        prs.RedirectStandardError = true;
        prs.RedirectStandardOutput = true;
        pr.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                outputStream.WriteLine(e.Data);
            }
        });
        pr.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                outputStream.WriteLine(e.Data);
            }
        });
        pr.StartInfo = prs;

        ThreadStart ths = new ThreadStart(() =>
        {
            pr.Start();
            pr.BeginOutputReadLine();
            pr.BeginErrorReadLine();
            pr.WaitForExit();
            pr.Close();
            outputStream.Close();
        });
        Thread th = new Thread(ths);
        th.Start();
        StartCoroutine(ForegroundWindow());
    }

    private IEnumerator ForegroundWindow()
    {
        yield return new WaitForSeconds(2f);
        SetForegroundWindow(windowUnity);
    }

    private void OnApplicationQuit()
    {
        pr?.Close();
        outputStream?.Close();
    }
}
