﻿using System;
using UnityEngine;
using VRC.Dynamics;
using VRC.SDK3.Dynamics.Contact.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;

namespace Hai.ComboGesture.Scripts.Components
{
    public class ComboGestureDynamics : MonoBehaviour
    {
        public Animator previewAnimator;
        public ComboGestureDynamicsItem[] items;
    }

    [Serializable]
    public struct ComboGestureDynamicsItem
    {
        public ComboGestureDynamicsEffect effect;
        public AnimationClip clip;
        public bool bothEyesClosed;
        public ComboGestureMoodSet moodSet;

        public ComboGestureDynamicsSource source;
        public VRCContactReceiver contactReceiver;
        public VRCPhysBone physBone;
        public ComboGestureDynamicsPhysBoneSource physBoneSource;
        public string parameterName;
        public ComboGestureDynamicsParameterType parameterType;
        public ComboGestureDynamicsCondition condition;
        public float threshold;
        public bool isHardThreshold;

        public CgeDynamicsDescriptor ToDescriptor()
        {
            return new CgeDynamicsDescriptor
            {
                parameter = DynamicsResolveParameter(),
                condition = condition,
                threshold = threshold,
                isHardThreshold = isHardThreshold,
                parameterType = DynamicsResolveParameterType()
            };
        }

        private string DynamicsResolveParameter()
        {
            switch (source)
            {
                case ComboGestureDynamicsSource.Contact:
                    return contactReceiver.parameter;
                case ComboGestureDynamicsSource.PhysBone:
                    return $"{physBone.parameter}_{ToSuffix()}";
                case ComboGestureDynamicsSource.Parameter:
                    return parameterName;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ComboGestureDynamicsParameterType DynamicsResolveParameterType()
        {
            switch (source)
            {
                case ComboGestureDynamicsSource.Contact:
                    return contactReceiver.receiverType == ContactReceiver.ReceiverType.Proximity
                        ? ComboGestureDynamicsParameterType.Float
                        : parameterType;
                case ComboGestureDynamicsSource.PhysBone:
                    return physBoneSource != ComboGestureDynamicsPhysBoneSource.IsGrabbed
                        ? ComboGestureDynamicsParameterType.Float
                        : parameterType;
                case ComboGestureDynamicsSource.Parameter:
                    return parameterType;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string ToSuffix()
        {
            return Enum.GetName(typeof(ComboGestureDynamicsPhysBoneSource), physBoneSource);
        }
    }

    [Serializable]
    public enum ComboGestureDynamicsEffect
    {
        Clip, MoodSet
    }

    [Serializable]
    public enum ComboGestureDynamicsSource
    {
        Contact, PhysBone, Parameter
    }

    [Serializable]
    public enum ComboGestureDynamicsParameterType
    {
        Bool, Int, Float
    }

    [Serializable]
    public enum ComboGestureDynamicsPhysBoneSource
    {
        Stretch, Angle, IsGrabbed
    }

    [Serializable]
    public enum ComboGestureDynamicsCondition
    {
        IsAboveThreshold, IsBelowOrEqualThreshold
    }

    public struct CgeDynamicsRankedDescriptor
    {
        public int rank;
        public CgeDynamicsDescriptor descriptor;
    }

    public struct CgeDynamicsDescriptor
    {
        public string parameter;
        public float threshold;
        public ComboGestureDynamicsParameterType parameterType;
        public ComboGestureDynamicsCondition condition;
        public bool isHardThreshold;
    }
}