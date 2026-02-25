using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;

namespace Viture.XR.Samples.StarterAssets
{
    public class VitureHandVisualizer : MonoBehaviour
    {
        [SerializeField] private GameObject m_LeftHandTracking;

        [SerializeField] private GameObject m_RightHandTracking;

        [SerializeField] private bool m_DrawMeshes = true;

        [SerializeField] private bool m_DrawJoints = true;

        [SerializeField] private GameObject m_JointPrefab;

        [SerializeField] private bool m_DrawLines = true;

        [SerializeField] private Material m_LineMaterial;

        private XRHandSubsystem m_HandSubsystem;

        private readonly Dictionary<Handedness, Dictionary<XRHandJointID, GameObject>> m_SpawnedJoints = new();
        private readonly Dictionary<Handedness, Dictionary<XRHandJointID, LineRenderer>> m_JointLines = new();

        private static readonly Vector3[] s_LinePointsReuse = new Vector3[2];

        private const float k_LineWidth = 0.0036f;

        private void Start()
        {
            if (!m_DrawMeshes)
            {
                for (int i = 0; i < 2; i++)
                {
                    var meshController = i == 0
                        ? m_LeftHandTracking.GetComponent<XRHandMeshController>()
                        : m_RightHandTracking.GetComponent<XRHandMeshController>();

                    if (meshController != null)
                    {
                        meshController.enabled = false;
                        meshController.handMeshRenderer.enabled = false;
                    }
                }
            }

#if !UNITY_EDITOR
            if (m_DrawJoints)
            {
                SpawnJointPrefabs(Handedness.Left);
                SpawnJointPrefabs(Handedness.Right);
                
                if (m_HandSubsystem != null)
                {
                    UpdateRenderingVisibility(Handedness.Left, m_HandSubsystem.leftHand.isTracked);
                    UpdateRenderingVisibility(Handedness.Right, m_HandSubsystem.rightHand.isTracked);
                }
            }
#endif
        }

        private void OnDestroy()
        {
            foreach (var joints in m_SpawnedJoints.Values)
            {
                foreach (var joint in joints.Values)
                {
                    if (joint != null)
                        Destroy(joint);
                }
            }

            m_SpawnedJoints.Clear();
            m_JointLines.Clear();
        }

        private void OnEnable()
        {
            m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?
                .GetLoadedSubsystem<XRHandSubsystem>();
            if (m_HandSubsystem != null)
            {
                m_HandSubsystem.trackingAcquired += OnTrackingAcquired;
                m_HandSubsystem.trackingLost += OnTrackingLost;
                m_HandSubsystem.updatedHands += OnUpdatedHands;
            }
        }

        private void OnDisable()
        {
            if (m_HandSubsystem != null)
            {
                m_HandSubsystem.trackingAcquired -= OnTrackingAcquired;
                m_HandSubsystem.trackingLost -= OnTrackingLost;
                m_HandSubsystem.updatedHands -= OnUpdatedHands;
            }
        }

        private void SpawnJointPrefabs(Handedness handedness)
        {
            m_SpawnedJoints.Add(handedness, new Dictionary<XRHandJointID, GameObject>());
            m_JointLines.Add(handedness, new Dictionary<XRHandJointID, LineRenderer>());

            XRHandSkeletonDriver skeletonDriver = handedness == Handedness.Left
                ? m_LeftHandTracking.GetComponent<XRHandSkeletonDriver>()
                : m_RightHandTracking.GetComponent<XRHandSkeletonDriver>();

            if (skeletonDriver != null)
            {
                foreach (var jointTransformReference in skeletonDriver.jointTransformReferences)
                {
                    var jointId = jointTransformReference.xrHandJointID;
                    var spawnedJoint = Instantiate(m_JointPrefab, jointTransformReference.jointTransform);
                    m_SpawnedJoints[handedness][jointId] = spawnedJoint;

                    if (m_DrawLines && jointId != XRHandJointID.Wrist)
                    {
                        var lineRenderer = spawnedJoint.AddComponent<LineRenderer>();
                        ConfigureLineRenderer(lineRenderer);
                        m_JointLines[handedness][jointId] = lineRenderer;
                    }
                }
            }
        }

        private void ConfigureLineRenderer(LineRenderer lineRenderer)
        {
            lineRenderer.startWidth = lineRenderer.endWidth = k_LineWidth;
            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.material = m_LineMaterial;
        }

        private void OnTrackingAcquired(XRHand hand)
        {
            UpdateRenderingVisibility(hand.handedness, true);
        }

        private void OnTrackingLost(XRHand hand)
        {
            UpdateRenderingVisibility(hand.handedness, false);
        }

        private void OnUpdatedHands(XRHandSubsystem subsystem, XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
            XRHandSubsystem.UpdateType updateType)
        {
            if (m_DrawLines)
            {
                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.LeftHandJoints) != 0)
                    UpdateJointLines(subsystem.leftHand);

                if ((updateSuccessFlags & XRHandSubsystem.UpdateSuccessFlags.RightHandJoints) != 0)
                    UpdateJointLines(subsystem.rightHand);
            }
        }

        private void UpdateJointLines(XRHand hand)
        {
            if (!m_JointLines.ContainsKey(hand.handedness))
                return;

            var handedness = hand.handedness;
            var lines = m_JointLines[handedness];

            foreach (var kvp in lines)
            {
                var jointId = kvp.Key;
                var lineRenderer = kvp.Value;
                var parentJointId = GetParentJointId(jointId);

                if (parentJointId == XRHandJointID.Invalid)
                    continue;

                if (hand.GetJoint(jointId).TryGetPose(out var jointPose) &&
                    hand.GetJoint(parentJointId).TryGetPose(out var parentPose))
                {
                    s_LinePointsReuse[0] = parentPose.position;
                    s_LinePointsReuse[1] = jointPose.position;
                    lineRenderer.SetPositions(s_LinePointsReuse);
                }
            }
        }

        private void UpdateRenderingVisibility(Handedness handedness, bool isTracked)
        {
            if (m_SpawnedJoints.TryGetValue(handedness, out var joints))
            {
                foreach (var joint in joints.Values)
                {
                    joint.SetActive(isTracked);
                }
            }
        }

        private XRHandJointID GetParentJointId(XRHandJointID jointId)
        {
            return jointId switch
            {
                // Thumb chain
                XRHandJointID.ThumbProximal => XRHandJointID.Wrist,
                XRHandJointID.ThumbDistal => XRHandJointID.ThumbProximal,
                XRHandJointID.ThumbTip => XRHandJointID.ThumbDistal,

                // Index chain
                XRHandJointID.IndexProximal => XRHandJointID.Wrist,
                XRHandJointID.IndexIntermediate => XRHandJointID.IndexProximal,
                XRHandJointID.IndexDistal => XRHandJointID.IndexIntermediate,
                XRHandJointID.IndexTip => XRHandJointID.IndexDistal,

                // Middle chain
                XRHandJointID.MiddleProximal => XRHandJointID.Wrist,
                XRHandJointID.MiddleIntermediate => XRHandJointID.MiddleProximal,
                XRHandJointID.MiddleDistal => XRHandJointID.MiddleIntermediate,
                XRHandJointID.MiddleTip => XRHandJointID.MiddleDistal,

                // Ring chain
                XRHandJointID.RingProximal => XRHandJointID.Wrist,
                XRHandJointID.RingIntermediate => XRHandJointID.RingProximal,
                XRHandJointID.RingDistal => XRHandJointID.RingIntermediate,
                XRHandJointID.RingTip => XRHandJointID.RingDistal,

                // Little chain
                XRHandJointID.LittleProximal => XRHandJointID.Wrist,
                XRHandJointID.LittleIntermediate => XRHandJointID.LittleProximal,
                XRHandJointID.LittleDistal => XRHandJointID.LittleIntermediate,
                XRHandJointID.LittleTip => XRHandJointID.LittleDistal,

                // Palm connects to wrist
                XRHandJointID.Palm => XRHandJointID.Wrist,

                _ => XRHandJointID.Invalid
            };
        }
    }
}