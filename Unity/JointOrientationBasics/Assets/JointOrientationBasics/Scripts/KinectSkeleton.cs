//------------------------------------------------------------------------------
// <copyright file="KinectSkeleton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using UnityEngine;
    using Kinect = Windows.Kinect;

    public class KinectSkeleton
    {
        internal enum SegmentType { Body, Head, LeftArm, LeftHand, RightArm, RightHand, LeftLeg, RightLeg };

        private Dictionary<Kinect.JointType, Joint> joints;
        private Dictionary<Kinect.JointType, Joint> Joints
        {
            get
            {
                if (this.joints == null)
                {
                    this.joints = new Dictionary<Kinect.JointType, Joint>();
                }

                return this.joints;
            }
        }

        private DoubleExponentialFilter jointSmoother;

        internal void Init()
        {
            if (this.Joints.Count == 0)
            {
                BuildHeirarchy();
            }

            if (this.jointSmoother == null)
            {
                this.jointSmoother = new DoubleExponentialFilter();
            }
        }

        internal void UpdateJointsFromKinectBody(Kinect.Body body)
        {
            if (body == null)
            {
                return;
            }

            if (this.Joints.Count == 0 || this.jointSmoother == null)
            {
                Init();
            }

            // update joint data based on the body
            UpdateJoints(body);
        }

        internal Joint GetJoint(Kinect.JointType type)
        {
            return this.Joints.FirstOrDefault(x => x.Key == type).Value;
        }

        internal Joint GetRootJoint()
        {
            return GetJoint(Kinect.JointType.SpineBase);
        }

        private void BuildHeirarchy()
        {
            // ensure a collection exists
            if (this.Joints.Count == 0)
            {
                CreateJoints();
            }

            // left leg
            this.Joints[Kinect.JointType.SpineBase].AddChild(this.Joints[Kinect.JointType.HipLeft]);
            this.Joints[Kinect.JointType.HipLeft].AddChild(this.Joints[Kinect.JointType.KneeLeft]);
            this.Joints[Kinect.JointType.KneeLeft].AddChild(this.Joints[Kinect.JointType.AnkleLeft]);
            this.Joints[Kinect.JointType.AnkleLeft].AddChild(this.Joints[Kinect.JointType.FootLeft]);

            // right leg
            this.Joints[Kinect.JointType.SpineBase].AddChild(this.Joints[Kinect.JointType.HipRight]);
            this.Joints[Kinect.JointType.HipRight].AddChild(this.Joints[Kinect.JointType.KneeRight]);
            this.Joints[Kinect.JointType.KneeRight].AddChild(this.Joints[Kinect.JointType.AnkleRight]);
            this.Joints[Kinect.JointType.AnkleRight].AddChild(this.Joints[Kinect.JointType.FootRight]);

            // spine to head
            this.Joints[Kinect.JointType.SpineBase].AddChild(this.Joints[Kinect.JointType.SpineMid]);
            this.Joints[Kinect.JointType.SpineMid].AddChild(this.Joints[Kinect.JointType.SpineShoulder]);
            this.Joints[Kinect.JointType.SpineShoulder].AddChild(this.Joints[Kinect.JointType.Neck]);
            this.Joints[Kinect.JointType.Neck].AddChild(this.Joints[Kinect.JointType.Head]);

            // left arm
            this.Joints[Kinect.JointType.SpineShoulder].AddChild(this.Joints[Kinect.JointType.ShoulderLeft]);
            this.Joints[Kinect.JointType.ShoulderLeft].AddChild(this.Joints[Kinect.JointType.ElbowLeft]);
            this.Joints[Kinect.JointType.ElbowLeft].AddChild(this.Joints[Kinect.JointType.WristLeft]);
            this.Joints[Kinect.JointType.WristLeft].AddChild(this.Joints[Kinect.JointType.HandLeft]);
            this.Joints[Kinect.JointType.HandLeft].AddChild(this.Joints[Kinect.JointType.HandTipLeft]);
            this.Joints[Kinect.JointType.WristLeft].AddChild(this.Joints[Kinect.JointType.ThumbLeft]);

            // right arm
            this.Joints[Kinect.JointType.SpineShoulder].AddChild(this.Joints[Kinect.JointType.ShoulderRight]);
            this.Joints[Kinect.JointType.ShoulderRight].AddChild(this.Joints[Kinect.JointType.ElbowRight]);
            this.Joints[Kinect.JointType.ElbowRight].AddChild(this.Joints[Kinect.JointType.WristRight]);
            this.Joints[Kinect.JointType.WristRight].AddChild(this.Joints[Kinect.JointType.HandRight]);
            this.Joints[Kinect.JointType.HandRight].AddChild(this.Joints[Kinect.JointType.HandTipRight]);

            this.Joints[Kinect.JointType.WristRight].AddChild(this.Joints[Kinect.JointType.ThumbRight]);
        }

        private void CreateJoints()
        {
            this.Joints.Clear();

            foreach (Kinect.JointType type in Enum.GetValues(typeof(Kinect.JointType)))
            {
                Joint joint = GetJoint(type);
                if (joint == null)
                {
                    joint = new Joint();
                    joint.Init(type.ToString());
                }

                this.Joints.Add(type, joint);
            }
        }

        private static string[] jointNames;
        public static string[] JointNames
        {
            get
            {
                if (KinectSkeleton.jointNames == null || KinectSkeleton.jointNames.Length == 0)
                {
                    KinectSkeleton.jointNames = Enum.GetNames(typeof(Kinect.JointType));
                }

                return KinectSkeleton.jointNames;
            }
        }

        private void UpdateJoints(Kinect.Body body)
        {
            if (body == null)
            {
                return;
            }

            DoubleExponentialFilter.TRANSFORM_SMOOTH_PARAMETERS smoothingParams = jointSmoother.SmoothingParameters;

            // get the floorClipPlace from the body information
            Vector4 floorClipPlane = Helpers.FloorClipPlane;

            // get rotation of camera
            Quaternion cameraRotation = Helpers.CalculateFloorRotationCorrection(floorClipPlane);

            // generate a vertical offset from floor plane
            Vector3 floorOffset = cameraRotation * Vector3.up * floorClipPlane.w;

            foreach (Kinect.JointType jt in Enum.GetValues(typeof(Kinect.JointType)))
            {
                // If inferred, we smooth a bit more by using a bigger jitter radius
                Windows.Kinect.Joint kinectJoint = body.Joints[jt];
                if (kinectJoint.TrackingState == Kinect.TrackingState.Inferred)
                {
                    smoothingParams.fJitterRadius *= 2.0f;
                    smoothingParams.fMaxDeviationRadius *= 2.0f;
                }

                Joint parent = this.Joints[jt].Parent;

                // set initial joint value from Kinect
                Vector3 rawPosition = cameraRotation * ConvertJointPositionToUnityVector3(body, jt) + floorOffset;
                Quaternion rawRotation = cameraRotation * ConvertJointQuaternionToUnityQuaterion(body, jt);
                if (Helpers.QuaternionZero.Equals(rawRotation) && parent != null)
                {
                    Vector3 direction = rawPosition - parent.RawPosition;
                    Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
                    Vector3 normal = Vector3.Cross(perpendicular, direction);

                    // calculate a rotation, Y forward for Kinect
                    if (normal.magnitude != 0)
                    {
                        rawRotation = Quaternion.LookRotation(normal, direction);
                    }
                    else
                    {
                        rawRotation = Quaternion.identity;
                    }
                }

                DoubleExponentialFilter.Joint fj 
                    = new DoubleExponentialFilter.Joint(rawPosition,  rawRotation);

                fj = jointSmoother.UpdateJoint(jt, fj, smoothingParams);

                // set the raw joint value for the node
                this.Joints[jt].SetRawtData(fj.Position, fj.Rotation);
            }

            //Vector3 offsetPosition = this.Joints[Kinect.JointType.SpineBase].RawPosition - floorOffset;
            Vector3 offsetPosition = Vector3.zero;
            Quaternion offsetRotation = Quaternion.identity;

            // calculate the relative joint and rotation
            this.Joints[Kinect.JointType.SpineBase].CalculateOffset(offsetPosition, offsetRotation);
        }

        internal static KinectSkeleton.SegmentType GetSegmentType(Kinect.JointType type)
        {
            KinectSkeleton.SegmentType segment = KinectSkeleton.SegmentType.Body;

            if (type == Kinect.JointType.Neck || type == Kinect.JointType.Head)
            {
                segment = KinectSkeleton.SegmentType.Head;
            }
            else if (type == Kinect.JointType.ShoulderLeft || type == Kinect.JointType.ElbowLeft || type == Kinect.JointType.WristLeft || type == Kinect.JointType.HandLeft)
            {
                segment = KinectSkeleton.SegmentType.LeftArm;
            }
            else if (type == Kinect.JointType.HandLeft || type == Kinect.JointType.ThumbLeft || type == Kinect.JointType.HandTipLeft)
            {
                segment = KinectSkeleton.SegmentType.LeftHand;
            }
            else if (type == Kinect.JointType.ShoulderRight || type == Kinect.JointType.ElbowRight || type == Kinect.JointType.WristRight)
            {
                segment = KinectSkeleton.SegmentType.RightArm;
            }
            else if (type == Kinect.JointType.HandRight || type == Kinect.JointType.ThumbRight || type == Kinect.JointType.HandTipRight)
            {
                segment = KinectSkeleton.SegmentType.RightHand;
            }
            else if (type == Kinect.JointType.HipLeft || type == Kinect.JointType.KneeLeft || type == Kinect.JointType.AnkleLeft || type == Kinect.JointType.FootLeft)
            {
                segment = KinectSkeleton.SegmentType.LeftLeg;
            }
            else if (type == Kinect.JointType.HipRight || type == Kinect.JointType.KneeRight || type == Kinect.JointType.AnkleRight || type == Kinect.JointType.FootRight)
            {
                segment = KinectSkeleton.SegmentType.RightLeg;
            }

            return segment;
        }

        internal static Vector3 ConvertJointPositionToUnityVector3(Kinect.Body body, Kinect.JointType type, bool mirror = true)
        {
            Vector3 position = new Vector3(body.Joints[type].Position.X,
                body.Joints[type].Position.Y,
                body.Joints[type].Position.Z);

            // translate -x
            if (mirror)
            {
                position.x *= -1;
            }

            return position;
        }

        internal static Quaternion ConvertJointQuaternionToUnityQuaterion(Kinect.Body body, Kinect.JointType jt, bool mirror = true)
        {
            Quaternion rotation = new Quaternion(body.JointOrientations[jt].Orientation.X,
                body.JointOrientations[jt].Orientation.Y,
                body.JointOrientations[jt].Orientation.Z,
                body.JointOrientations[jt].Orientation.W);

            // flip rotation
            if (mirror)
            {
                rotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
            }

            return rotation;
        }

    }
}