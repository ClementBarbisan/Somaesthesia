using System;
using System.Collections;
using System.Collections.Generic;
using nuitrack;
using NuitrackSDK;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public struct Joints
{
    public Vector3 Pos;
    public Vector3 Dir;
}

public class SendSkeletonToShader : MonoBehaviour
{
    private UserData.SkeletonData _skeleton;
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
    // Start is called before the first frame update
    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += NuitrackManagerOnonUserTrackerUpdate;
    }

    private void OnDestroy()
    {
        _buffer?.Release();
        NuitrackManager.onUserTrackerUpdate -= NuitrackManagerOnonUserTrackerUpdate;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void NuitrackManagerOnonUserTrackerUpdate(UserFrame frame)
    {
        if (frame.NumUsers > 0 && NuitrackManager.UsersList.Count > 0)
        {
            if (_buffer == null)
            {
                _buffer = new ComputeBuffer(_jointsInfo.Length, sizeof(float) * 6);
            }
            _skeleton = NuitrackManager.UsersList[0].GetUser(frame.Users[0].ID).Skeleton;
            _boxUser = frame.Users[0].Box;
            List<Joints> jointsList = new List<Joints>();
            for (int i = 0; i < _jointsInfo.Length; i++)
            {
                Joints newJoint = new Joints();
                newJoint.Pos = _skeleton.GetJoint(_jointsInfo[i]).Position;
                newJoint.Dir = _skeleton.GetJoint(_jointsInfo[i]).Rotation * Vector3.forward;
                jointsList.Add(newJoint);
            }
            _buffer.SetData(jointsList);
            Shader.SetGlobalBuffer("_Skeleton", _buffer);
        }
    }
}
