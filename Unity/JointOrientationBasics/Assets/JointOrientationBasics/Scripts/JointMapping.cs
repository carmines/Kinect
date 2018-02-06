//------------------------------------------------------------------------------
// <copyright file="JointMapping.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    using UnityEngine;
    using Kinect = Windows.Kinect;

    [System.Serializable]
    public class JointMapping : MonoBehaviour
    {
        /// <summary>
        /// mesh object this mapping goes with
        /// needs to be define as public without properties for 
        /// Unity to de/serialize between editor and play mode
        /// </summary>
        [SerializeField]
        public SkinnedMeshRenderer mesh;
        public SkinnedMeshRenderer Mesh
        {
            get { return this.mesh; }
            set
            {
                if (value == this.mesh)
                {
                    return;
                }

                Init(value);
            }
        }

        /// <summary>
        /// List of the Kinect joint to SkinnedMesh mappings
        /// needs to be define as public without properties for 
        /// Unity to de/serialize between editor and play mode
        /// </summary>
        [SerializeField]
        public List<Map> List;
        public List<Map> MappedList
        {
            get
            {
                if (this.List == null)
                {
                    this.List = new List<Map>();
                }

                return this.List;
            }
        }

        /// <summary>
        /// Kinect BodySourceManager to get body frames from
        /// </summary>
        public BodySourceManager BodySourceManager;

        public Quaternion meshToIdentity = Quaternion.identity;

        public bool ApplyIdentity = false;

        public bool ApplyToMesh = false;

        public void Start()
        {
            // common for someone to forget to set this so,
            // if body source manager wasn't set try to find it
            if (null == this.BodySourceManager)
            {
                BodySourceManager[] objs = FindObjectsOfType(typeof(BodySourceManager)) as BodySourceManager[];
                if (objs.Length > 0)
                {
                    this.BodySourceManager = objs[0];
                }
            }
        }

        public void Update()
        {
            // Get the closest body
            Kinect.Body body = this.BodySourceManager.FindClosestBody();
            if (null == body)
            {
                return;
            }

            Vector3 position = KinectSkeleton.ConvertJointPositionToUnityVector3(body, Kinect.JointType.SpineBase);
            Quaternion rotation = KinectSkeleton.ConvertJointQuaternionToUnityQuaterion(body, Kinect.JointType.SpineBase);

            this.transform.position = position;
            this.transform.rotation = rotation;

            // update the skeleton with the new body joint/orientation information
            UpdateSkeletons(body);
        }

        /// <summary>
        /// construct of the hierarchical body information for mapping
        /// </summary>
        private KinectSkeleton kinectSkeleton;
        private KinectSkeleton KinectSkeleton
        {
            get
            {
                if (this.kinectSkeleton == null)
                {
                    this.kinectSkeleton = new KinectSkeleton();
                    this.kinectSkeleton.Init();
                }

                return this.kinectSkeleton;
            }
        }

        /// <summary>
        /// same construct to store bind pose and details about mesh
        /// </summary>
        private MeshSkeleton meshSkeleton;
        private MeshSkeleton MeshSkeleton
        {
            get
            {
                if (this.meshSkeleton == null && this.Mesh != null)
                {
                    this.meshSkeleton = new MeshSkeleton();
                    this.meshSkeleton.Init(this.Mesh);
                }

                return this.meshSkeleton;
            }
        }

        /// <summary>
        /// list of joint names to use in UI
        /// </summary>
        public string[] JointTypeNames
        {
            get
            {
                return KinectSkeleton.JointNames;
            }
        }

        /// <summary>
        /// list of the BoneNames for the mesh
        /// </summary>
        public List<string> BoneNames
        {
            get
            {
                if (this.MeshSkeleton != null)
                {
                    return this.MeshSkeleton.BoneNames;
                }

                return null;
            }
        }


        public void OnEnable()
        {
            hideFlags = HideFlags.HideAndDontSave;
        }

        public void Init(SkinnedMeshRenderer mesh)
        {
            this.mesh = mesh;

            this.name = GenerateName(this.mesh);

            // reset anything associated with the old model
            this.MappedList.Clear();

            this.meshSkeleton = null;

            this.kinectSkeleton = null;
        }

        internal Map GetMapFromJointType(Kinect.JointType type)
        {
            Predicate<Map> finder = (Map m) => { return m.Type == type; };

            return this.MappedList.Find(finder);
        }

        internal Map GetMapFromTypeName(string typeName)
        {
            return GetMapFromJointType(Helpers.ParseEnum<Kinect.JointType>(typeName));
        }

        internal Map GetMapFromBone(Transform bone)
        {
            Predicate<Map> finder = (Map m) => { return m.Bone == bone; };

            return this.MappedList.Find(finder);
        }

        internal Map GetMapFromBoneName(string boneName)
        {
            return GetMapFromBone(GetBoneFromName(boneName));
        }

        private Transform GetBoneFromName(string boneName)
        {
            if (string.IsNullOrEmpty(boneName) || this.MeshSkeleton == null)
            {
                return null;
            }

            Transform foundBone = null;

            if (this.Mesh != null)
            {
                foreach (var bone in this.Mesh.bones)
                {
                    if (bone.name == boneName)
                    {
                        foundBone = bone;
                        break;
                    }
                }
            }

            return foundBone;
        }

        public void AddMapping(string typeName, string boneName)
        {
            Kinect.JointType? type = Helpers.ParseEnum<Kinect.JointType>(typeName);
            Transform bone = GetBoneFromName(boneName);

            if (type != null && bone != null)
            {
                AddMapping(type.Value, bone);
            }
        }

        private void AddMapping(Kinect.JointType type, Transform bone)
        {
            if (bone == null)
            {
                return;
            }

            // check for existing mapping for type and bone
            Map foundMapping = GetMapFromJointType(type);
            Map foundBoneMapping = GetMapFromBone(bone);

            if (foundBoneMapping != null)
            {
                UpdateMapping(foundBoneMapping, null); // reset the bone to null
            }

            if (foundMapping == null && bone != null)
            {
                // wasn't found, so create one
                foundMapping = new Map(type, bone);

                // add the entry to the mapping
                this.MappedList.Add(foundMapping);
            }
            else
            {
                UpdateMapping(foundMapping, bone);
            }
        }

        public void UpdateMapping(Map map, int boneSelectedIndex)
        {
            Transform bone = GetBoneFromName(this.MeshSkeleton.BoneNames[boneSelectedIndex]);
            if (bone == null)
            {
                return;
            }

            UpdateMapping(map, bone);
        }

        public void UpdateMapping(Map jointMap, Transform bone)
        {
            // is it already selected by another map
            var foundBoneMapping = GetMapFromBone(bone);
            if (foundBoneMapping != null && foundBoneMapping != jointMap)
            {
                // swap the bone for this one
                foundBoneMapping.Bone = jointMap.Bone;
            }

            if (bone == null)
            {
                // remove it from the map
                RemoveMapping(jointMap);
            }
            else
            {
                // map the bone to this jointMap
                jointMap.Bone = bone;
            }
        }

        public void RemoveMapping(int boneSelectedIndex)
        {
            Transform bone = GetBoneFromName(this.MeshSkeleton.BoneNames[boneSelectedIndex]);
            if (bone == null)
            {
                return;
            }

            Map map = GetMapFromBone(bone);
            if (bone != null) // cannot delete in the loop
            {
                RemoveMapping(map);
            }

        }

        private void RemoveMapping(Map jointMap)
        {
            this.MappedList.Remove(jointMap);
        }

        internal void UpdateSkeletons(Kinect.Body body)
        {
            // update the skeleton with the new body joint/orientation information
            this.KinectSkeleton.UpdateJointsFromKinectBody(body);

            if(this.MeshSkeleton != null)
            {
                Quaternion localRotation = this.mesh.rootBone.localRotation;
                
                Quaternion rotation = this.mesh.rootBone.rotation;

                Quaternion offsetRoatation = rotation * Quaternion.Inverse(localRotation);

                this.MeshSkeleton.Update();

                UpdateMesh(this.mesh.rootBone);
            }
        }

        private void UpdateMesh(Transform bone)
        {
            if (bone == null)
            {
                return;
            }

            // if this is a mapped bone apply rotation to the bone
            Map map = GetMapFromBoneName(bone.name);
            if (map != null)
            {
                Joint kn = this.KinectSkeleton.GetJoint(map.Type);
                Joint mn = this.MeshSkeleton.GetJoint(map.Bone, false);
                if (kn != null && mn != null)
                {
                    // set joint position based on direction of bone
                    if (map.Bone.parent != null)
                    {
                        Vector3 direction = map.Bone.position - map.Bone.parent.position;
                        float length = direction.magnitude;

                        Quaternion forwardRotation = Quaternion.LookRotation(direction);
                        Vector3 position = forwardRotation * (Vector3.forward * length);

                        map.Bone.position = map.Bone.parent.position + position;
                    }
                    else
                    {
                        map.Bone.position = mn.Position;
                    }

                    // do we need to include parent rotation
                    Quaternion appliedRotation = kn.Rotation 
                        * new Quaternion(-.5f, .5f, -.5f, -.5f)
                        * Quaternion.Inverse(map.AdjustmentToMesh);

                    map.Bone.rotation = appliedRotation;
                }
            }

            foreach (Transform child in bone)
            {
                UpdateMesh(child);
            }
        }

        internal Joint GetRootJointFromKinect()
        {
            return this.KinectSkeleton.GetRootJoint();
        }

        internal Joint GetRootBoneFromMesh()
        {
            if(this.MeshSkeleton != null)
            {
                return this.MeshSkeleton.GetRootJoint(this.ApplyToMesh);
            }

            return null;
        }

        public static JointMapping Create(GameObject baseObject)
        {
            // create joint mapper script and attach to GameObject
            JointMapping jm = new GameObject().AddComponent<JointMapping>() as JointMapping;
            DontDestroyOnLoad(jm.gameObject);

            // if a object was selected when creating, attach to that and find the bones/mesh object for that object
            SkinnedMeshRenderer smr = null;
            if (baseObject != null)
            {
                var allChildren = baseObject.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach (var child in allChildren)
                {
                    if (child != null)
                    {
                        smr = child as SkinnedMeshRenderer;
                        break;
                    }
                }

                if (smr != null)
                {
                    jm.Mesh = smr;
                }
            }

            // create name based smr
            jm.name = GenerateName(smr);

            return jm;
        }

        private static string GenerateName(SkinnedMeshRenderer smr)
        {
            String name = "JointMapping";

            if (smr != null)
            {
                name += "_" + smr.name;
            }

            return name;
        }
    }
}
