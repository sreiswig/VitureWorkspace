using System.Collections.Generic;
using UnityEngine.XR;
using UnityEngine.XR.Management;

#if INCLUDE_UNITY_XR_HANDS
using UnityEngine.XR.Hands;
#endif

namespace Viture.XR
{
    public class VitureLoader : XRLoaderHelper
    {
        const string k_DisplaySubsystemId = "Viture-Display";
        const string k_InputSubsystemId = "Viture-Input";

        static readonly List<XRDisplaySubsystemDescriptor> k_DisplaySubsystemDescriptors = new();
        static readonly List<XRInputSubsystemDescriptor> k_InputSubsystemDescriptors = new();
#if INCLUDE_UNITY_XR_HANDS 
        static readonly List<XRHandSubsystemDescriptor> k_HandSubsystemDescriptors = new();

        public XRHandSubsystem handSubsystem => GetLoadedSubsystem<XRHandSubsystem>();
#else
        private const string k_HandsPackageRequiredError = "XR Hands package (com.unity.xr.hands) is required to use XR Hand Subsystem";
#endif
        
        public override bool Initialize()
        {
            CreateSubsystem<XRDisplaySubsystemDescriptor, XRDisplaySubsystem>(k_DisplaySubsystemDescriptors, k_DisplaySubsystemId);
            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(k_InputSubsystemDescriptors, k_InputSubsystemId);
#if INCLUDE_UNITY_XR_HANDS
            CreateSubsystem<XRHandSubsystemDescriptor, XRHandSubsystem>(k_HandSubsystemDescriptors, VitureHandSubsystem.k_HandSubsystemId);
#endif
            return true;
        }

        public override bool Start()
        {
            StartSubsystem<XRDisplaySubsystem>();
            StartSubsystem<XRInputSubsystem>();
#if INCLUDE_UNITY_XR_HANDS
            StartSubsystem<XRHandSubsystem>();
#endif
            return true;
        }

        public override bool Stop()
        {
            StopSubsystem<XRDisplaySubsystem>();
            StopSubsystem<XRInputSubsystem>();
#if INCLUDE_UNITY_XR_HANDS
            StopSubsystem<XRHandSubsystem>();
#endif
            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRDisplaySubsystem>();
            DestroySubsystem<XRInputSubsystem>();
#if INCLUDE_UNITY_XR_HANDS
            DestroySubsystem<XRHandSubsystem>();
#endif
            return true;
        }
    }
}