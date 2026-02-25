using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace Viture.XR.Samples.StarterAssets
{
    public class VitureHandRayController : MonoBehaviour
    {
        [SerializeField] private Handedness m_Handedness;

        [SerializeField] private NearFarInteractor m_NearFarInteractor;

        private bool m_UseFarCasting = false;
        private XRHandSubsystem m_HandSubsystem;

        private void Awake()
        {
            m_UseFarCasting = m_NearFarInteractor.enableFarCasting;
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            m_NearFarInteractor.enableFarCasting = false;
#else
        m_HandSubsystem = XRGeneralSettings.Instance?.Manager?.activeLoader?
            .GetLoadedSubsystem<XRHandSubsystem>();
            if (m_HandSubsystem != null)
            {
                m_HandSubsystem.trackingAcquired += OnTrackingAcquired;
                m_HandSubsystem.trackingLost += OnTrackingLost;

                if (m_NearFarInteractor != null)
                {
                    XRHand hand = m_Handedness == Handedness.Left ? m_HandSubsystem.leftHand : m_HandSubsystem.rightHand;
                    m_NearFarInteractor.enableFarCasting = hand.isTracked && m_UseFarCasting;
                }
            }
#endif
        }

        private void OnDisable()
        {
#if !UNITY_EDITOR
            if (m_HandSubsystem != null)
            {
                m_HandSubsystem.trackingAcquired -= OnTrackingAcquired;
                m_HandSubsystem.trackingLost -= OnTrackingLost;
            }
#endif
        }

        private void OnTrackingAcquired(XRHand hand)
        {
            if (hand.handedness == m_Handedness)
            {
                if (m_NearFarInteractor != null)
                    m_NearFarInteractor.enableFarCasting = m_UseFarCasting;
            }
        }

        private void OnTrackingLost(XRHand hand)
        {
            if (hand.handedness == m_Handedness)
            {
                if (m_NearFarInteractor != null)
                    m_NearFarInteractor.enableFarCasting = false;
            }
        }
    }
}