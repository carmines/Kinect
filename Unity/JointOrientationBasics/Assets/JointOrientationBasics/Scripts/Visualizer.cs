//------------------------------------------------------------------------------
// <copyright file="Visualizer.cs" company="Microsoft">
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

    /// <summary>
    /// helper method to draw the mesh models for the JointMapping
    /// </summary>
    public class Visualizer : MonoBehaviour
    {
        private enum MeshType { Kinect, Mesh };

        private class Bone
        {
            public GameObject BoneMesh;
            public GameObject JointMesh;

            public Bone(GameObject bone, GameObject joint)
            {
                this.BoneMesh = bone;
                this.JointMesh = joint;
            }
        }

        /// <summary>
        /// instance of the joint mappings
        /// </summary>
        public JointMapping JointMapper;

        /// <summary>
        /// joint model to draw to represent the orientation
        /// </summary>
        public GameObject JointModel;
        public float JointScale = 0.03f;

        /// <summary>
        /// bone model to illustrate direction of the bone
        /// </summary>
        public GameObject BoneModel;
        public Vector3 BoneScale = Vector3.one * 0.2f;

        public bool DrawJoint = false;
        public bool DrawBoneModel = true;
        public bool DebugLines = true;
        public bool ApplyRotataion = false;
        public bool ApplyIdentity = false;

        public Quaternion MeshToIdentityRotation = Quaternion.identity;

        /// <summary>
        /// reference of the list to allow for edits while in Play mode
        /// </summary>
        public List<Map> JointList;

        private GameObject kinectVisualizerParent;
        private GameObject KinectVisualizerParent
        {
            get
            {
                if (this.kinectVisualizerParent == null)
                {
                    CreateKinectSkeletonModel();
                }

                return this.kinectVisualizerParent;
            }
        }

        private Dictionary<string, Bone> kinectBodyModel;
        private Dictionary<string, Bone> KinectBodyModel
        {
            get
            {
                if (this.kinectBodyModel == null)
                {
                    this.kinectBodyModel = new Dictionary<string, Bone>();
                }

                return this.kinectBodyModel;
            }
        }

        private GameObject meshVisualizerParent;
        private GameObject MeshVisualizerParent
        {
            get
            {
                if (this.meshVisualizerParent == null)
                {
                    CreateMeshSkeletonModel();
                }

                return this.meshVisualizerParent;
            }
        }

        private Dictionary<string, Bone> meshBodyModel;
        private Dictionary<string, Bone> MeshBodyModel
        {
            get
            {
                if (this.meshBodyModel == null)
                {
                    this.meshBodyModel = new Dictionary<string, Bone>();
                }

                return this.meshBodyModel;
            }
        }

        // Use this for initialization
        void Start()
        {
            // if jointmapper was not set, try to find one.
            if(this.JointMapper == null)
            {
                GameObject[] objs = UnityEngine.Object.FindObjectsOfType(typeof(GameObject)) as GameObject[];
                foreach(var obj in objs)
                {
                    this.JointMapper = obj.GetComponent<JointMapping>(); ;
                    if(this.JointMapper != null)
                    {
                        break;
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (this.JointMapper == null ||
                this.JointModel == null ||
                this.BoneModel == null)
            {
                return;
            }

            // expose the list so we can edit the adjustment on each joint
            this.JointList = this.JointMapper.MappedList;

            // update the visual 
            this.JointMapper.ApplyToMesh = this.ApplyRotataion;
            this.JointMapper.ApplyIdentity = this.ApplyIdentity;

            // inverse the direction
            this.JointMapper.meshToIdentity = this.MeshToIdentityRotation;

            // update the visual models
            VisualizeJoints(MeshType.Kinect);

            VisualizeJoints(MeshType.Mesh);
        }

        /// <summary>
        /// recursive function to iterate the tree of joints
        /// </summary>
        /// <param name="joint">root joint to start from</param>
        private void VisualizeJoints(MeshType type)
        {
            // check to ensure the visualizer was created
            if (this.KinectVisualizerParent == null)
            {
                CreateKinectSkeletonModel();
            }

            if (this.MeshVisualizerParent == null)
            {
                CreateMeshSkeletonModel();
            }

            Joint root = null;
            switch (type)
            {
                case MeshType.Kinect:
                    root = this.JointMapper.GetRootJointFromKinect();
                    if(root != null)
                    {
                        // get the floorClipPlace from the body information
                        Vector4 floorClipPlane = Helpers.FloorClipPlane;

                        // generate a vertical offset from floor plane
                        Vector3 floorOffset = Vector3.up * floorClipPlane.w;

                        UpdateKinectJointVisual(root, 
                            this.KinectVisualizerParent.transform.position,
                            this.KinectVisualizerParent.transform.rotation);
                    }
                    break;

                case MeshType.Mesh:
                    root = this.JointMapper.GetRootBoneFromMesh();
                    if (root != null)
                    {
                        UpdateMeshJointVisual(root,
                            this.MeshVisualizerParent.transform.position,
                            this.MeshVisualizerParent.transform.rotation);
                    }
                    break;
            }
        }

        /// <summary>
        /// based on a joint location, will draw the bone visual for that joint
        /// </summary>
        /// <param name="joint">joint to show</param>
        private void UpdateKinectJointVisual(Joint joint, Vector3 parentPosition, Quaternion parentRotation)
        {
            if (joint == null)
            {
                return;
            }

            // get the rotation of the joint from Kinect
            // in the case of tip, we calculate an rotation amount
            Quaternion boneDirection = Quaternion.identity;
            if (Helpers.QuaternionZero.Equals(joint.RawRotation))
            {
                // generate a rotation if its a tip with no orientation from Kinect
                Vector3 perpendicular = Vector3.Cross(joint.LocalPosition, Vector3.up);
                Vector3 normal = Vector3.Cross(perpendicular, joint.LocalPosition);

                // calculate a rotation
                boneDirection.SetLookRotation(normal, joint.LocalPosition);
            }
            else
            {
                // Y - is the direction of the bone
                // Z - normal
                // X - bi-normal
                boneDirection = joint.Rotation;
            }

            // kinect joint identity aligns with the world so no correction needed
            Quaternion worldRotation = parentRotation * boneDirection;

            Vector3 worldPosition = parentPosition;

            // length is the distance from its parent
            float lengthOfBone = joint.LocalPosition.magnitude;

            // update the visual
            // draw debug lines
            if (this.DebugLines)
            {
                // visualize the rotation in world space from the parent
                Helpers.DrawDebugBoneYDirection(worldPosition, lengthOfBone, worldRotation);
            }

            // get the joint visual from the collection
            Bone model = this.KinectBodyModel.FirstOrDefault(x => x.Key == joint.Name).Value;
            if (model != null)
            {
                GameObject boneMesh = model.BoneMesh;
                Helpers.SetVisible(boneMesh, this.DrawBoneModel);

                GameObject jointMesh = model.JointMesh;
                Helpers.SetVisible(jointMesh, this.DrawJoint);

                // update bone mesh
                UpdateBoneMesh(boneMesh, worldPosition, worldRotation, lengthOfBone, this.BoneScale);

                // update joint mesh
                UpdateJointMesh(jointMesh, worldPosition, worldRotation, this.JointScale);
            }

            // for every child we have the corrected orientation from its parent 
            // and cacluated new position from the parent
            if (joint.Children != null)
            {
                foreach (var j in joint.Children)
                {
                    // calculate the forward direction
                    Quaternion lookTo = Quaternion.identity;
                    if (lengthOfBone != 0)
                    {
                        lookTo = Quaternion.LookRotation(joint.LocalPosition, Vector3.up);
                    }

                    Vector3 toJointPosition = lookTo * Vector3.forward * lengthOfBone;

                    UpdateKinectJointVisual(j, worldPosition + toJointPosition, parentRotation);
                }
            }
        }

        /// <summary>
        /// update the mesh model single joint
        /// </summary>
        /// <param name="joint">joint information from the mesh model</param>
        private void UpdateMeshJointVisual(Joint joint, Vector3 parentPosition, Quaternion parentRotation)
        {
            if (joint == null)
            {
                return;
            }

            // mesh joints are the position and rotation of the bone
            Vector3 worldPosition = joint.Position;
            Quaternion worldRotation = joint.Rotation;

            // the old local position can be used to determine the length offset
            float lengthOfBone = joint.LocalPosition.magnitude;

            // update the visual
            // get the joint visual from the collection
            Bone model = this.MeshBodyModel.FirstOrDefault(x => x.Key == joint.Name).Value;
            if (model != null)
            {
                GameObject bone = model.BoneMesh;
                Helpers.SetVisible(bone, this.DrawBoneModel);

                GameObject jm = model.JointMesh;
                Helpers.SetVisible(jm, this.DrawJoint);

                // draw debug lines
                if (DebugLines)
                {
                    // visualize the rotation in world space
                    Helpers.DrawDebugBoneYDirection(worldPosition, lengthOfBone, worldRotation * this.MeshToIdentityRotation);
                }

                // update model
                UpdateBoneMesh(bone, worldPosition, worldRotation * this.MeshToIdentityRotation, lengthOfBone, this.BoneScale);

                // visualize the mesh model
                UpdateJointMesh(jm, worldPosition, worldRotation * this.MeshToIdentityRotation, this.JointScale);
            }

            //// set joint position based on direction of bone
            //if (joint.Children != null)
            //{
            //    // calculate bone length
            //    Vector3 end = Vector3.zero;
            //    int count = 0;
            //    if(joint.Children.Count > 0)
            //    {
            //        foreach(var child in joint.Children)
            //        {
            //            end += child.Position;
            //            count++;
            //        }
            //        end = end / count;
            //    }

            //    Vector3 direction = end - parentPosition + joint.LocalPosition;
            //    float length = direction.magnitude;

            //    // get world rotation for the bone
            //    Quaternion directionRotation = joint.Rotation * this.MeshToIdentityRotation;

            //}

            if (joint.Children != null)
            {
                foreach (var j in joint.Children)
                {
                    // calculate the forward direction
                    Quaternion lookTo = Quaternion.identity;
                    if (lengthOfBone != 0)
                    {
                        lookTo = Quaternion.LookRotation(joint.LocalPosition, Vector3.up);
                    }

                    Vector3 toJointPosition = lookTo * Vector3.forward * lengthOfBone;

                    UpdateMeshJointVisual(j, worldPosition + toJointPosition, parentRotation * joint.LocalRotation);
                }

            }
        }

        /// <summary>
        /// helper method to draw the Kinect skeleton structure
        /// </summary>
        private void CreateKinectSkeletonModel()
        {
            if (this.kinectVisualizerParent != null)
            {
                DestroyObject(this.KinectVisualizerParent);
                this.kinectVisualizerParent = null;
            }

            // Create debug skeleton mapping
            this.KinectBodyModel.Clear();

            // create parent gameObject for debug controls
            this.kinectVisualizerParent = new GameObject();
            this.kinectVisualizerParent.name = "KinectSkeletonModel";
            // create visualizer
            foreach (Kinect.JointType jt in Enum.GetValues(typeof(Kinect.JointType)))
            {
                // skip the spineBase since the parent is the camera
                if(jt == Kinect.JointType.SpineBase)
                {
                    continue;
                }

                var joint = (GameObject)Instantiate(this.JointModel, transform.position, transform.rotation);
                joint.name = string.Format("{0}", jt);
                joint.transform.localScale = Vector3.one * this.JointScale;
                joint.gameObject.transform.parent = this.KinectVisualizerParent.gameObject.transform;

                // build bone visual
                var bone = (GameObject)Instantiate(this.BoneModel, transform.position, transform.rotation);
                bone.name = string.Format("{0}", jt);
                bone.transform.localScale = this.BoneScale;

                // clean-up hierachy to display under one parent
                bone.gameObject.transform.parent = this.KinectVisualizerParent.gameObject.transform;

                // add to collection for the model
                this.KinectBodyModel.Add(jt.ToString(), new Bone(bone, joint));
            }
        }

        /// <summary>
        /// Method to create the visual of the Mesh model joints
        /// </summary>
        private void CreateMeshSkeletonModel()
        {
            if (this.JointList == null || this.JointList.Count == 0)
            {
                return;
            }

            if (this.meshVisualizerParent != null)
            {
                DestroyObject(this.meshVisualizerParent);
                this.meshVisualizerParent = null;
            }

            // Create debug skeleton for mesh
            this.MeshBodyModel.Clear();

            // create parent gameObject for debug controls
            this.meshVisualizerParent = new GameObject();
            this.meshVisualizerParent.name = "MeshSkeletonModel";

            // create visualizer for mapped mesh model
            foreach (var jm in this.JointMapper.MappedList)
            {
                Transform meshBone = jm.Bone;

                // create a mesh for the joint
                var joint = (GameObject)Instantiate(this.JointModel, transform.position, transform.rotation);
                joint.name = string.Format("mesh_Joint_{0}", meshBone.name);
                joint.transform.position = jm.Bone.position;
                joint.transform.rotation = jm.Bone.rotation;
                joint.transform.localScale = Vector3.one * this.JointScale;

                // parent to the visualizer
                joint.gameObject.transform.parent = this.MeshVisualizerParent.gameObject.transform;

                // create a mesh for the bone
                var bone = (GameObject)Instantiate(this.BoneModel, transform.position, transform.rotation);
                bone.name = string.Format("mesh_Bone_{0}", meshBone.name);
                bone.transform.position = jm.Bone.position;
                bone.transform.rotation = jm.Bone.rotation;
                bone.transform.localScale = this.BoneScale;

                // clean-up hierachy to display under one parent
                bone.gameObject.transform.parent = this.MeshVisualizerParent.gameObject.transform;

                // add both models to the KinectBoneVisualizer
                this.MeshBodyModel.Add(meshBone.name, new Bone(bone, joint));
            }
        }

        /// <summary>
        /// Helper method to extend the bone in the direction to child
        /// </summary>
        /// <param name="bone">model used to visualize bone</param>
        /// <param name="position">start position for the model</param>
        /// <param name="rotation">the rotation to apply to the model</param>
        /// <param name="length">the distance to the child</param>
        /// <param name="boneScale">scale to apply to the model</param>
        private static void UpdateBoneMesh(GameObject bone, Vector3 position, Quaternion rotation, float length, Vector3 boneScale)
        {
            // get mesh verticies;
            MeshFilter meshFilter = (MeshFilter)bone.GetComponent("MeshFilter");
            var verticies = meshFilter.mesh.vertices;

            // get the forward vector from rotation - kinect Y is the forward
            Vector3 fwdLength = Vector3.up * length / boneScale.y; ;

            // update verticies of the tip
            // bone is oriented Y-up so no conversion needed
            verticies[4] = fwdLength;
            verticies[7] = verticies[4];
            verticies[10] = verticies[4];
            verticies[13] = verticies[4];
            meshFilter.mesh.vertices = verticies;
            meshFilter.mesh.RecalculateBounds();
            meshFilter.mesh.RecalculateNormals();

            // move bone into position
            bone.transform.position = position;
            bone.transform.rotation = rotation;
            bone.transform.localScale = boneScale;
        }

        /// <summary>
        /// apply transformations to the model
        /// </summary>
        /// <param name="joint">the game object to apply transformation to</param>
        /// <param name="position">the postion of the joint in world space</param>
        /// <param name="rotation">rotation to apply</param>
        /// <param name="jointScale">joint scale to adjust the size</param>
        private static void UpdateJointMesh(GameObject joint, Vector3 position, Quaternion rotation, float jointScale)
        {
            joint.transform.position = position;
            joint.transform.rotation = rotation;
            joint.transform.localScale = Vector3.one * jointScale;
        }
    }
}