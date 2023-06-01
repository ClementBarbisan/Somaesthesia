using System;
using System.Collections;
using System.Collections.Generic;
using nuitrack;
using nuitrack.device;
using NuitrackSDK;
using NuitrackSDK.Avatar;
using Unity.Mathematics;
using UnityEngine;
using Joint = nuitrack.Joint;
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
    List<Joints> jointsList = new List<Joints>();
    private BoundingBox _boxUser;
    private ComputeBuffer _buffer;
    nuitrack.JointType[] _jointsInfo = new nuitrack.JointType[]
    {
        nuitrack.JointType.Head,
        nuitrack.JointType.Neck,
        nuitrack.JointType.LeftCollar,
        nuitrack.JointType.Torso,
        nuitrack.JointType.Waist,
        nuitrack.JointType.LeftShoulder,
        nuitrack.JointType.RightShoulder,
        nuitrack.JointType.LeftElbow,
        nuitrack.JointType.RightElbow,
        nuitrack.JointType.LeftWrist,
        nuitrack.JointType.RightWrist,
        nuitrack.JointType.LeftHand,
        nuitrack.JointType.RightHand,
        nuitrack.JointType.LeftHip,
        nuitrack.JointType.RightHip,
        nuitrack.JointType.LeftKnee,
        nuitrack.JointType.RightKnee,
        nuitrack.JointType.LeftAnkle,
        nuitrack.JointType.RightAnkle
    };
    private int _id = -1;
    private Camera cam;
    // Start is called before the first frame update
    void Start()
    {
        cam = Camera.main;
        if (NuitrackManager.Instance.NuitrackInitialized)
        {
            NuitrackManager.onUserTrackerUpdate += UserTrackerOnOnUpdateEvent;
            NuitrackManager.SkeletonTracker.OnSkeletonUpdateEvent += SkeletonTrackerOnOnSkeletonUpdateEvent;
        }
    }

    private void UserTrackerOnOnUpdateEvent(UserFrame frame)
    {
        if (frame.NumUsers > 0 )
        {

            _id = frame.Users[0].ID;
        }
    }

    private void OnDestroy()
    {
        _buffer?.Release();
        if (NuitrackManager.Instance.NuitrackInitialized)
        {
            // NuitrackManager.SkeletonTracker.OnSkeletonUpdateEvent -= SkeletonTrackerOnOnSkeletonUpdateEvent;
            NuitrackManager.onUserTrackerUpdate -= UserTrackerOnOnUpdateEvent;
        }
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
            jointsList.Clear();
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
                newJoint.Pos = new Vector3(posCam.x - newJoint.Pos.x / 500f, posCam.y + newJoint.Pos.y / 500f,
                    posCam.z - newJoint.Pos.z / 600f);
                newJoint.Size = 0.25f;
                jointsList.Add(newJoint);
            }
            _buffer.SetData(jointsList);
            Shader.SetGlobalBuffer("_Skeleton", _buffer);
        }
    }
}
