using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;

public class LaunchProcess : MonoBehaviour
{
    // Start is called before the first frame update
    void OnEnable()
    {
        Process pr = new Process();
        ProcessStartInfo prs = new ProcessStartInfo();
        prs.FileName = @"python";
        prs.Arguments =
            "C:\\Git\\Somaesthesia\\mmaction2\\webcam_action_recognition.py c:\\tsn_imagenet-pretrained-r50_8xb32-1x1x8-100e_kinetics400-rgb.py" +
            " c:\\best_acc_top1_epoch_20.pth c:\\label_kinetics_tiny.txt --device cuda:0";
        pr.StartInfo = prs;

        ThreadStart ths = new ThreadStart(() => pr.Start());
        Thread th = new Thread(ths);
        th.Start();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
