using System;
using System.Collections;
using System.Collections.Generic;
using nuitrack;
using NuitrackSDK;
using NuitrackSDK.Avatar;
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

    private NuitrackAvatar _avatar;
    // private List<GameObject> _cubes = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        NuitrackManager.onUserTrackerUpdate += NuitrackManagerOnonUserTrackerUpdate;
        _avatar = GetComponent<NuitrackAvatar>();
    }

    private void OnDestroy()
    {
        _buffer?.Release();
        NuitrackManager.onUserTrackerUpdate -= NuitrackManagerOnonUserTrackerUpdate;
    }

    private void NuitrackManagerOnonUserTrackerUpdate(UserFrame frame)
    {
        if (frame.NumUsers > 0 && NuitrackManager.UsersList.Count > 0)
        {
            if (_buffer == null)
            {
                _buffer = new ComputeBuffer(_jointsInfo.Length, sizeof(float) * 6);
            }

            UserData data = NuitrackManager.UsersList[0].GetUser(NuitrackManager.UsersList[0].CurrentUserID);
            _skeleton = data.Skeleton;
            jointsList.Clear();
            for (int i = 0; i < _jointsInfo.Length; i++)
            {
                // if (_cubes.Count < _jointsInfo.Length)
                // {
                //     _cubes.Add(GameObject.CreatePrimitive(PrimitiveType.Cube));
                //     _cubes[^1].transform.localScale = Vector3.one * 0.1f;
                // }

                Joints newJoint = new Joints();
                newJoint.Pos = Quaternion.Euler(0, 180, 0) * (_skeleton.GetJoint(_jointsInfo[i]).Position);
                // newJoint.Pos.Scale(Vector3.one * 2f);
                // _cubes[i].transform.position = newJoint.Pos;
                newJoint.Dir = _skeleton.GetJoint(_jointsInfo[i]).Rotation * Vector3.forward;
                jointsList.Add(newJoint);
            }
            _buffer.SetData(jointsList);
            Shader.SetGlobalBuffer("_Skeleton", _buffer);
        }
    }
}
