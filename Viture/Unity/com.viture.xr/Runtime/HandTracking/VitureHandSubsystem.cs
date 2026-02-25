#if INCLUDE_UNITY_XR_HANDS
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Hands.ProviderImplementation;
using Viture.XR.InputDevices;

namespace Viture.XR
{
    /// <summary>
    /// VITURE XR hand tracking subsystem implementation for Unity XR Hands.
    /// </summary>
    [Preserve]
    public sealed class VitureHandSubsystem : XRHandSubsystem
    {
        internal const string k_HandSubsystemId = "Viture-Hands";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void RegisterDescriptor()
        {
            var descriptor = new XRHandSubsystemDescriptor.Cinfo
            {
                id = k_HandSubsystemId,
                providerType = typeof(VitureHandProvider),
                subsystemTypeOverride = typeof(VitureHandSubsystem)
            };

            XRHandSubsystemDescriptor.Register(descriptor);
        }

        XRHandProviderUtility.SubsystemUpdater m_Updater;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_Updater = new XRHandProviderUtility.SubsystemUpdater(this);
        }

        protected override void OnStart()
        {
            VitureXR.HandTracking.filterMode = VitureSettings.GetOrCreate().HandFilterMode;

            if (VitureSettings.GetOrCreate().ActivateHandTrackingOnStartup)
                VitureXR.HandTracking.Start();

            m_Updater.Start();
            base.OnStart();
        }

        protected override void OnStop()
        {
            VitureXR.HandTracking.Stop();
            m_Updater.Stop();
            base.OnStop();
        }

        protected override void OnDestroy()
        {
            m_Updater.Destroy();
            m_Updater = null;
            base.OnDestroy();
        }

        public class VitureHandProvider : XRHandSubsystemProvider
        {
            private readonly float[] m_RawHandData = new float[VitureNativeApi.HandTracking.k_HandDataLength];
            private readonly Vector3[] m_JointPositions = new Vector3[VitureNativeApi.HandTracking.k_HandJointCount];
            private readonly Quaternion[] m_JointRotations = new Quaternion[VitureNativeApi.HandTracking.k_HandJointCount];
            
            private Camera m_MainCamera;

            private static readonly Quaternion k_JointRot6L = Quaternion.AngleAxis(90f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.right);
            private static readonly Quaternion k_JointRot6R = Quaternion.AngleAxis(-90f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.right);
            private static readonly Quaternion k_JointRot3L = Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.AngleAxis(-90f, Vector3.up);
            private static readonly Quaternion k_JointRot3R = Quaternion.AngleAxis(180f, Vector3.forward) * Quaternion.AngleAxis(90f, Vector3.up);
            
            private static readonly Vector3 k_CenterEyeToHeadOffset = new(0f, 0f, -0.1f);
            private static readonly Vector3 k_HeadToLeftShoulderOffset = new(-0.15f, -0.3f, 0f);
            private static readonly Vector3 k_HeadToRightShoulderOffset = new(0.15f, -0.3f, 0f);

            public override void Start() {}

            public override void Stop() {}

            public override void Destroy() {}

            public override void GetHandLayout(NativeArray<bool> handJointsInLayout)
            {
                handJointsInLayout[XRHandJointID.Wrist.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.Palm.ToIndex()] = true;

                handJointsInLayout[XRHandJointID.ThumbMetacarpal.ToIndex()] = false;
                handJointsInLayout[XRHandJointID.ThumbProximal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.ThumbDistal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.ThumbTip.ToIndex()] = true;

                handJointsInLayout[XRHandJointID.IndexMetacarpal.ToIndex()] = false;
                handJointsInLayout[XRHandJointID.IndexProximal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.IndexIntermediate.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.IndexDistal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.IndexTip.ToIndex()] = true;

                handJointsInLayout[XRHandJointID.MiddleMetacarpal.ToIndex()] = false;
                handJointsInLayout[XRHandJointID.MiddleProximal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.MiddleIntermediate.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.MiddleDistal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.MiddleTip.ToIndex()] = true;

                handJointsInLayout[XRHandJointID.RingMetacarpal.ToIndex()] = false;
                handJointsInLayout[XRHandJointID.RingProximal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.RingIntermediate.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.RingDistal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.RingTip.ToIndex()] = true;

                handJointsInLayout[XRHandJointID.LittleMetacarpal.ToIndex()] = false;
                handJointsInLayout[XRHandJointID.LittleProximal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.LittleIntermediate.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.LittleDistal.ToIndex()] = true;
                handJointsInLayout[XRHandJointID.LittleTip.ToIndex()] = true;
            }

            public override UpdateSuccessFlags TryUpdateHands(UpdateType updateType,
                ref Pose leftHandRootPose, NativeArray<XRHandJoint> leftHandJoints,
                ref Pose rightHandRootPose, NativeArray<XRHandJoint> rightHandJoints)
            {
                var successFlags = UpdateSuccessFlags.None;

                if (!VitureNativeApi.HandTracking.ReadData(m_RawHandData))
                    return successFlags;

                if (TryUpdateHand(Handedness.Left, ref leftHandRootPose, leftHandJoints))
                    successFlags |= UpdateSuccessFlags.LeftHandRootPose | UpdateSuccessFlags.LeftHandJoints;

                if (TryUpdateHand(Handedness.Right, ref rightHandRootPose, rightHandJoints))
                    successFlags |= UpdateSuccessFlags.RightHandRootPose | UpdateSuccessFlags.RightHandJoints;

                return successFlags;
            }

            private bool TryUpdateHand(Handedness handedness, ref Pose rootPose, NativeArray<XRHandJoint> handJoints)
            {
                int baseIndex = handedness == Handedness.Left ? 0 : VitureNativeApi.HandTracking.k_HandDataLength / 2;

                if (!Mathf.Approximately(m_RawHandData[baseIndex], 1f))
                {
                    UpdateHandDevice(handedness, false);
                    return false;
                }

                var headTrackingCapability = VitureXR.HeadTracking.GetHeadTrackingCapability();
                if (headTrackingCapability == VitureHeadTrackingCapability.ThreeDoF)
                {
                    if (m_MainCamera == null)
                        m_MainCamera = Camera.main;

                    if (m_MainCamera == null)
                        return false;
                }

                for (int i = 0; i < VitureNativeApi.HandTracking.k_HandJointCount; i++)
                {
                    int jointIndex = baseIndex + 1 + i * 7;

                    float positionX = m_RawHandData[jointIndex];
                    float positionY = m_RawHandData[jointIndex + 1];
                    float positionZ = m_RawHandData[jointIndex + 2];
                    float rotationW = m_RawHandData[jointIndex + 3];
                    float rotationX = m_RawHandData[jointIndex + 4];
                    float rotationY = m_RawHandData[jointIndex + 5];
                    float rotationZ = m_RawHandData[jointIndex + 6];

                    if (headTrackingCapability == VitureHeadTrackingCapability.SixDoF)
                    {
                        m_JointPositions[i] = new Vector3(-positionX, positionZ, -positionY);
                        Quaternion jointRotation = new Quaternion(rotationX, -rotationZ, rotationY, rotationW);
                        m_JointRotations[i] = jointRotation * (handedness == Handedness.Left ? k_JointRot6L : k_JointRot6R);
                    }
                    else
                    {
                        Vector3 cameraLocalPosition = new Vector3(positionX, -positionY, positionZ);
                        Quaternion cameraLocalRotation = new Quaternion(-rotationX, rotationY, -rotationZ, rotationW);

                        m_JointPositions[i] = m_MainCamera.transform.position + m_MainCamera.transform.rotation * cameraLocalPosition;
                        Quaternion jointRotation = m_MainCamera.transform.rotation * cameraLocalRotation;
                        m_JointRotations[i] = jointRotation * (handedness == Handedness.Left ? k_JointRot3L : k_JointRot3R);
                    }
                    
                    VitureHandFilter.ProcessJoint(handedness, (VitureHandJointID)i, ref m_JointPositions[i], ref m_JointRotations[i]);
                }

                UpdateHandDevice(handedness, true);

                rootPose = new Pose(m_JointPositions[(int)VitureHandJointID.Wrist],
                    m_JointRotations[(int)VitureHandJointID.Wrist]);
                foreach (var mapping in s_JointMapping)
                {
                    var vitureJointId = mapping.Key;
                    var unityJointId = mapping.Value;

                    var pose = new Pose(m_JointPositions[(int)vitureJointId], m_JointRotations[(int)vitureJointId]);
                    var joint = XRHandProviderUtility.CreateJoint(handedness, XRHandJointTrackingState.Pose,
                        unityJointId, pose);
                    handJoints[unityJointId.ToIndex()] = joint;
                }

                return true;
            }

            private void UpdateHandDevice(Handedness handedness, bool isTracked)
            {
                if (!isTracked)
                {
                    VitureHandDevice.UpdateDeviceState(
                        handedness,
                        false,
                        InputTrackingState.None,
                        Vector3.zero,
                        Quaternion.identity,
                        new VitureHandState());
                    return;
                }

                int baseIndex = handedness == Handedness.Left ? 0 : VitureNativeApi.HandTracking.k_HandDataLength / 2;

                var wristPosition = m_JointPositions[(int)VitureHandJointID.Wrist];
                var wristRotation = m_JointRotations[(int)VitureHandJointID.Wrist];
                var thumbTipPosition = m_JointPositions[(int)VitureHandJointID.ThumbTip];
                var palmFacing = m_RawHandData[baseIndex + VitureNativeApi.HandTracking.k_PalmFacingOffset];
                var gesture = m_RawHandData[baseIndex + VitureNativeApi.HandTracking.k_GestureOffset];

                // Calculate aim position and rotation
                var palmPosition = m_JointPositions[(int)VitureHandJointID.Palm];
                if (m_MainCamera == null)
                    m_MainCamera = Camera.main;

                Vector3 aimDirection = Vector3.forward;
                if (m_MainCamera != null)
                {
                    Vector3 headPosition = m_MainCamera.transform.position +
                                           m_MainCamera.transform.TransformVector(k_CenterEyeToHeadOffset);
                    Quaternion headRotation = Quaternion.Euler(0f, m_MainCamera.transform.eulerAngles.y, 0f);
                    Vector3 shoulderPosition = handedness == Handedness.Left
                        ? headPosition + headRotation * k_HeadToLeftShoulderOffset
                        : headPosition + headRotation * k_HeadToRightShoulderOffset;
                    aimDirection = (palmPosition - shoulderPosition).normalized;
                }

                // Calculate poke position and rotation
                var indexTipPosition = m_JointPositions[(int)VitureHandJointID.IndexTip];
                var indexDistalPosition = m_JointPositions[(int)VitureHandJointID.IndexDistal];
                Vector3 pokeDirection = (indexTipPosition - indexDistalPosition).normalized;

                var state = new VitureHandState
                {
                    pinchPosition = thumbTipPosition,
                    aimPosition = palmPosition,
                    aimRotation = Quaternion.LookRotation(aimDirection),
                    pokePosition = indexTipPosition,
                    pokeRotation = Quaternion.LookRotation(pokeDirection),
                    select = Mathf.Approximately(gesture, (int)VitureGesture.Pinch),
                    gesture = (int)gesture,
                    palmFacing = (int)palmFacing
                };

                VitureHandDevice.UpdateDeviceState(
                    handedness,
                    true,
                    InputTrackingState.Position | InputTrackingState.Rotation,
                    wristPosition,
                    wristRotation,
                    state);
            }

            public ReadOnlySpan<float> GetRawHandData()
            {
                return new ReadOnlySpan<float>(m_RawHandData);
            }

            private static readonly Dictionary<VitureHandJointID, XRHandJointID> s_JointMapping = new()
            {
                { VitureHandJointID.Wrist, XRHandJointID.Wrist },
                { VitureHandJointID.Palm, XRHandJointID.Palm },

                { VitureHandJointID.ThumbProximal, XRHandJointID.ThumbProximal },
                { VitureHandJointID.ThumbDistal, XRHandJointID.ThumbDistal },
                { VitureHandJointID.ThumbTip, XRHandJointID.ThumbTip },

                { VitureHandJointID.IndexProximal, XRHandJointID.IndexProximal },
                { VitureHandJointID.IndexIntermediate, XRHandJointID.IndexIntermediate },
                { VitureHandJointID.IndexDistal, XRHandJointID.IndexDistal },
                { VitureHandJointID.IndexTip, XRHandJointID.IndexTip },

                { VitureHandJointID.MiddleProximal, XRHandJointID.MiddleProximal },
                { VitureHandJointID.MiddleIntermediate, XRHandJointID.MiddleIntermediate },
                { VitureHandJointID.MiddleDistal, XRHandJointID.MiddleDistal },
                { VitureHandJointID.MiddleTip, XRHandJointID.MiddleTip },

                { VitureHandJointID.RingProximal, XRHandJointID.RingProximal },
                { VitureHandJointID.RingIntermediate, XRHandJointID.RingIntermediate },
                { VitureHandJointID.RingDistal, XRHandJointID.RingDistal },
                { VitureHandJointID.RingTip, XRHandJointID.RingTip },

                { VitureHandJointID.LittleProximal, XRHandJointID.LittleProximal },
                { VitureHandJointID.LittleIntermediate, XRHandJointID.LittleIntermediate },
                { VitureHandJointID.LittleDistal, XRHandJointID.LittleDistal },
                { VitureHandJointID.LittleTip, XRHandJointID.LittleTip }
            };
        }
    }
}
#endif