using System;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Viture.XR
{
    [XRConfigurationData("VITURE", VitureConstants.k_SettingsKey)]
    [Serializable]
    public class VitureSettings : ScriptableObject
    {
        [SerializeField]
        private VitureAppGlassesSupport m_AppGlassesSupport = VitureAppGlassesSupport.SixDoFOnly;
        
        [SerializeField]
        private bool m_ActivateHandTrackingOnStartup = true;
        
        [SerializeField]
        private VitureHandFilterMode m_HandFilterMode = VitureHandFilterMode.Responsive;

        [SerializeField]
        private bool m_CameraPermission = false;
        
        public VitureAppGlassesSupport AppGlassesSupport
        {
            get => m_AppGlassesSupport;
            set => m_AppGlassesSupport = value;
        }
        
        public bool ActivateHandTrackingOnStartup
        {
            get => m_ActivateHandTrackingOnStartup;
            set => m_ActivateHandTrackingOnStartup = value;
        }
        
        public VitureHandFilterMode HandFilterMode => m_HandFilterMode;

        public bool CameraPermission
        {
            get => m_CameraPermission;
            set => m_CameraPermission = value;
        }

        internal static VitureSettings GetOrCreate()
        {
            VitureSettings settings = null;
#if UNITY_EDITOR
            UnityEditor.EditorBuildSettings.TryGetConfigObject(VitureConstants.k_SettingsKey, out settings);
#else
            settings = s_RuntimeInstance;
#endif
            if (settings == null)
                settings = CreateInstance<VitureSettings>();
            return settings;
        }
        
#if !UNITY_EDITOR
        private static VitureSettings s_RuntimeInstance = null;

        private void Awake() => s_RuntimeInstance = this;
#endif
    }
}