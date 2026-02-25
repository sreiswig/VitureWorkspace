using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Management;
using UnityEngine.XR.Hands;

namespace Viture.XR.Samples.StarterAssets
{
    public class TwoHandTransform : MonoBehaviour
    {
        [SerializeField] private Transform m_Target;

        [Header("Input Actions")]
        [SerializeField] private InputActionReference m_LeftHandSelectAction;
        [SerializeField] private InputActionReference m_RightHandSelectAction;
        
        [Header("Translation")]
        [SerializeField] private bool m_EnableTranslation = true;
        [SerializeField, Tooltip("Multiplier for translation sensitivity")]
        private float m_TranslationMultiplier = 6f;
        
        [Header("Rotation")]
        [SerializeField] private bool m_EnableRotation = true;
        
        [Header("Scale")]
        [SerializeField] private bool m_EnableScale = true;
        [SerializeField, Tooltip("Minimum allowed scale")]
        private float m_MinScale = 0.2f;
        [SerializeField, Tooltip("Maximum allowed scale")]
        private float m_MaxScale = 5f;

        private XRHandSubsystem m_HandSubsystem;
        
        private bool m_IsTwoHandPinching;

        private bool m_IsTranslating;
        private Vector3 m_PinchStartMidpoint;
        private Vector3 m_TargetStartPosition;

        private bool m_IsRotating;
        private Vector3 m_PinchStartDirection;
        private Quaternion m_TargetStartRotation;

        private bool m_IsScaling;
        private float m_PinchStartDistance;
        private float m_TargetStartScale;
        
        private void Update()
        {
            if (m_Target == null)
                return;
            
            if (m_HandSubsystem == null)
            {
                m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?
                    .GetLoadedSubsystem<XRHandSubsystem>();
            }

            if (m_HandSubsystem == null)
                return;

            m_IsTwoHandPinching = GetIsTwoHandPinching();
            
            if (m_EnableTranslation)
                UpdateTranslation();
            
            if (m_EnableRotation)
                UpdateRotation();
            
            if (m_EnableScale)
                UpdateScale();
        }

        private void UpdateTranslation()
        {
            if (m_IsTwoHandPinching)
            {
                if (m_IsTranslating)
                {
                    Vector3 currentMidpoint = GetTwoHandPinchMidpoint();
                    Vector3 deltaMovement = (currentMidpoint - m_PinchStartMidpoint) * m_TranslationMultiplier;
                    m_Target.position = m_TargetStartPosition + deltaMovement;
                }
                else
                {
                    m_IsTranslating = true;
                    m_PinchStartMidpoint = GetTwoHandPinchMidpoint();
                    m_TargetStartPosition = m_Target.position;
                }
            }
            else
            {
                m_IsTranslating = false;
            }
        }

        private void UpdateRotation()
        {
            if (m_IsTwoHandPinching)
            {
                if (m_IsRotating)
                {
                    Vector3 currentDirection = GetTwoHandPinchDirection();
                    Vector3 startDirXZ = new Vector3(m_PinchStartDirection.x, 0, m_PinchStartDirection.z).normalized;
                    Vector3 currentDirXZ = new Vector3(currentDirection.x, 0, currentDirection.z).normalized;
                    
                    if (startDirXZ != Vector3.zero && currentDirXZ != Vector3.zero)
                    {
                        float angle = Vector3.SignedAngle(startDirXZ, currentDirXZ, Vector3.up);
                        Quaternion yRotation = Quaternion.AngleAxis(angle, Vector3.up);
                        m_Target.rotation = yRotation * m_TargetStartRotation;
                    }
                }
                else
                {
                    m_IsRotating = true;
                    m_PinchStartDirection = GetTwoHandPinchDirection();
                    m_TargetStartRotation = m_Target.rotation;
                }
            }
            else
            {
                m_IsRotating = false;
            }
        }

        private void UpdateScale()
        {
            if (m_IsTwoHandPinching)
            {
                if (m_IsScaling)
                {
                    float newScale = m_TargetStartScale * (GetTwoHandPinchDistance() / m_PinchStartDistance);
                    newScale = Mathf.Clamp(newScale, m_MinScale, m_MaxScale);
                    m_Target.transform.localScale = new Vector3(newScale, newScale, newScale);
                }
                else
                {
                    m_IsScaling = true;
                    m_PinchStartDistance = GetTwoHandPinchDistance();
                    m_TargetStartScale = m_Target.localScale.x;
                }
            }
            else
            {
                m_IsScaling = false;
            }
        }

        private bool GetIsTwoHandPinching()
        {
            return m_LeftHandSelectAction.action.ReadValue<float>() > 0.5f &&
                   m_RightHandSelectAction.action.ReadValue<float>() > 0.5f;
        }

        private Vector3 GetTwoHandPinchMidpoint()
        {
            var leftThumbTip = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip);
            var rightThumbTip = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);

            if (leftThumbTip.TryGetPose(out Pose leftThumbTipPose) &&
                rightThumbTip.TryGetPose(out Pose rightThumbTipPose))
                return (leftThumbTipPose.position + rightThumbTipPose.position) * 0.5f;

            return m_PinchStartMidpoint;
        }

        private Vector3 GetTwoHandPinchDirection()
        {
            var leftThumbTip = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip);
            var rightThumbTip = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);
            
            if (leftThumbTip.TryGetPose(out Pose leftThumbTipPose) &&
                rightThumbTip.TryGetPose(out Pose rightThumbTipPose))
                return (rightThumbTipPose.position - leftThumbTipPose.position).normalized;

            return m_PinchStartDirection;
        }
        
        private float GetTwoHandPinchDistance()
        {
            var leftThumbTip = m_HandSubsystem.leftHand.GetJoint(XRHandJointID.ThumbTip);
            var rightThumbTip = m_HandSubsystem.rightHand.GetJoint(XRHandJointID.ThumbTip);

            if (leftThumbTip.TryGetPose(out Pose leftThumbTipPose) &&
                rightThumbTip.TryGetPose(out Pose rightThumbTipPose))
                return Vector3.Distance(leftThumbTipPose.position, rightThumbTipPose.position);
            
            return m_PinchStartDistance > 0 ? m_PinchStartDistance : 0.5f;
        }
    }
}
