//------------------------------------------------------------------------------
// <copyright file="MeshSkeleton.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    public class MeshSkeleton
    {
        private SkinnedMeshRenderer mesh;

        private Dictionary<string, Joint> joints;
        private Dictionary<string, Joint> Joints
        {
            get
            {
                if (this.joints == null)
                {
                    this.joints = new Dictionary<string, Joint>();
                }

                return this.joints;
            }
        }

        private Dictionary<string, Joint> defaultJoints;
        private Dictionary<string, Joint> DefaultJoints
        {
            get
            {
                if (this.defaultJoints == null)
                {
                    this.defaultJoints = new Dictionary<string, Joint>();
                }

                return this.defaultJoints;
            }
        }

        private List<string> boneNames;
        internal List<string> BoneNames
        {
            get
            {
                if (this.boneNames == null)
                {
                    this.boneNames = new List<string>();
                }

                return this.boneNames;
            }
        }

        internal void Init(SkinnedMeshRenderer mesh)
        {
            this.mesh = mesh;

            this.DefaultJoints.Clear();

            this.Joints.Clear();

            this.BoneNames.Clear();

            // generate the bone and joints based on model
            if(this.mesh != null)
            {
                foreach (var bone in this.mesh.bones)
                {
                    this.BoneNames.Add(bone.name);
                }

                CreateJoints();
            }
        }

        internal void Update()
        {
            UpdateJointList(this.Joints, this.mesh.rootBone);

            // amount of rotation not part of the heirarchy
            Vector3 offsetPosition = this.mesh.rootBone.position - this.mesh.rootBone.localPosition;
            Quaternion offsetRotation = this.mesh.rootBone.rotation * Quaternion.Inverse(this.mesh.rootBone.localRotation);

            // calculate the offsets
            this.Joints[this.mesh.rootBone.name].CalculateOffset(offsetPosition, offsetRotation);
        }

        internal Joint GetRootJoint(bool useBaseJoints)
        {
            return GetJoint(this.mesh.rootBone, useBaseJoints);
        }

        internal Joint GetJoint(Transform bone, bool useBaseJoints)
        {
            if (this.mesh == null || bone == null)
            {
                return null;
            }

            if(useBaseJoints)
            {
                return this.DefaultJoints.FirstOrDefault(x => x.Key == bone.name).Value;
            }
            else
            {
                return this.Joints.FirstOrDefault(x => x.Key == bone.name).Value;
            }
        }

        private void CreateJoints()
        {
            if (this.mesh == null)
            {
                return;
            }

            // create the joint structure for the default pose
            this.DefaultJoints.Clear();
            CreateJoint(this.DefaultJoints, this.mesh.rootBone, null);

            // create the joints that will be updated every frame
            this.Joints.Clear();
            CreateJoint(this.Joints, this.mesh.rootBone, null);

            // amount of rotation not part of the heirarchy
            Vector3 offsetPosition = this.mesh.rootBone.position - this.mesh.rootBone.localPosition;
            //Quaternion offsetRotation = this.mesh.rootBone.rotation * Quaternion.Inverse(this.mesh.rootBone.localRotation);
            Quaternion offsetRotation = Quaternion.identity;

            // calculate the offsets
            this.DefaultJoints[this.mesh.rootBone.name].CalculateOffset(offsetPosition, offsetRotation);
            this.Joints[this.mesh.rootBone.name].CalculateOffset(offsetPosition, offsetRotation);
        }

        private static void CreateJoint(Dictionary<string, Joint> list, Transform bone, Joint parent)
        {
            // check that it is not already created
            Joint node = list.FirstOrDefault(x => x.Key == bone.name).Value;
            if(node != null)
            {
                return;
            }

            // create the joint and add to the list
            node = new Joint();
            node.Init(bone.name);
            node.SetRawtData(bone.position, bone.rotation);
            list.Add(bone.name, node);

            // if this is a child joint, set the property
            if (parent != null)
            {
                parent.AddChild(node);
            }

            // traverse children to create tree
            foreach (Transform child in bone)
            {
                CreateJoint(list, child, node);
            }
        }

        private static void UpdateJoint(Dictionary<string, Joint> list, Transform bone)
        {
            Joint node = list.FirstOrDefault(x => x.Key == bone.name).Value;
            if(node != null)
            {
                node.SetRawtData(bone.position, bone.rotation);
            }
        }

        private static void UpdateJointList(Dictionary<string, Joint> list, Transform bone)
        {
            UpdateJoint(list, bone);

            foreach (Transform child in bone)
            {
                UpdateJointList(list, child);
            }
        }
    }
}