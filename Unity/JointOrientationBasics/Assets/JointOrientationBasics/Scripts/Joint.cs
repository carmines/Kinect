//------------------------------------------------------------------------------
// <copyright file="Joint.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------
namespace JointOrientationBasics
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public class Joint
    {
        public string Name;

        public Joint Parent;

        public Vector3 LocalPosition;

        public Quaternion LocalRotation;

        public Vector3 RawPosition;

        public Quaternion RawRotation;

        private List<Joint> children;
        public List<Joint> Children
        {
            get
            {
                if (this.children == null)
                {
                    this.children = new List<Joint>();
                }

                return this.children;
            }
        }

        /// <summary>
        /// returns the postion of the joint from the parent
        /// </summary>
        public Vector3 Position
        {
            get
            {
                Vector3 position = this.LocalPosition;

                // traverse tree to get all positions to this joint
                if (this.Parent != null)
                {
                    position = Parent.Position + position;
                }

                return position;
            }
        }

        /// <summary>
        /// returns the total rotation from all the parents and itself
        /// </summary>
        public Quaternion Rotation
        {
            get
            {
                Quaternion rotation = this.LocalRotation;

                // traverse tree to get all rotations to this joint
                if (this.Parent != null)
                {
                    rotation = Parent.Rotation * this.LocalRotation;
                }

                return rotation;
            }
        }

        /// <summary>
        /// Initialize the joint
        /// </summary>
        /// <param name="name">joint name</param>
        public void Init(string name)
        {
            this.Name = name;

            this.Parent = null;

            this.Children.Clear();

            this.LocalPosition = Vector3.zero;

            this.LocalRotation = Quaternion.identity;

            this.RawPosition = Vector3.zero;

            this.RawRotation = Quaternion.identity;
        }

        public void AddChild(Joint joint)
        {
            joint.Parent = this;

            this.Children.Add(joint);
        }

        public void SetRawtData(Vector3 position, Quaternion rotation)
        {
            this.RawPosition = position;
            this.RawRotation = rotation;
        }

        public void CalculateOffset(
            UnityEngine.Vector3 globalOffsetPosition, 
            UnityEngine.Quaternion globalOffsetRotation)
        {
            // local position from parent
            this.LocalPosition = this.RawPosition;
            if(this.Parent != null)
            {
                this.LocalPosition -= this.Parent.Position;
            }

            // to calculate local rotation from parent
            this.LocalRotation = this.RawRotation;
            if(this.Parent != null)
            {
                this.LocalRotation = Quaternion.Inverse(this.Parent.Rotation) * this.RawRotation;
            }

            // update children
            if (this.Children != null)
            {
                foreach (var bone in this.Children)
                {
                    Vector3 newPosition = this.LocalPosition;
                    Quaternion newRotation = this.LocalRotation;
                    if(this.Parent != null)
                    {
                        newPosition = this.Parent.Position + this.LocalPosition;
                        newRotation = this.Parent.Rotation * this.LocalRotation;
                    }

                    bone.CalculateOffset(newPosition, newRotation);
                }
            }

            if(this.Parent == null)
            {
                this.LocalPosition = Vector3.up * (this.RawPosition - globalOffsetPosition).y;
                this.LocalRotation = Quaternion.Inverse(globalOffsetRotation) * this.RawRotation;
            }
        }
    }
}