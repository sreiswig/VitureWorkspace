using System;
using System.Threading;
using UnityEngine;
using UnityEngine.XR.Management;

namespace Viture.XR
{
    /// <summary>
    /// Central hub providing static access to VITURE XR functionality.
    /// All APIs are organized into logical categories for easy discovery and use.
    /// </summary>
    public static class VitureXR
    {
        internal static SynchronizationContext s_MainThreadContext;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            s_MainThreadContext = SynchronizationContext.Current;

            Application.quitting += OnApplicationQuitting;
            Application.focusChanged += OnApplicationFocusChanged;

#if UNITY_ANDROID && !UNITY_EDITOR
            VitureNativeApi.Capture.RegisterRecordingCallbacks(
                Capture.RecordingStartSuccessCallback,
                Capture.RecordingStartFailureCallback,
                Capture.RecordingSaveSuccessCallback,
                Capture.RecordingSaveFailureCallback);
#endif
        }
        
        /// <summary>
        /// Gets the active VITURE XR loader instance.
        /// </summary>
        /// <returns>The active VitureLoader instance, or null if no VITURE loader is active.</returns>
        public static VitureLoader GetLoader()
        {
            return XRGeneralSettings.Instance?.Manager?.activeLoader as VitureLoader;
        }

        private static void OnApplicationQuitting()
        {
            if (Capture.isRecording)
                Capture.StopRecording();
        }

        private static void OnApplicationFocusChanged(bool hasFocus)
        {
            if (!hasFocus)
            {
                if (Capture.isRecording)
                    Capture.StopRecording();
            }
        }
        
        /// <summary>
        /// Provides information and control for connected VITURE glasses.
        /// </summary>
        public static class Glasses
        {
            /// <summary>
            /// Gets the model of the currently connected VITURE glasses.
            /// </summary>
            /// <returns>Connected model, or Unknown if no glasses are connected.</returns>
            public static VitureGlassesModel GetGlassesModel()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                return (VitureGlassesModel)VitureNativeApi.Glasses.GetGlassesModel();
#else
                Debug.LogWarning("VitureXR.Glasses.GetGlassesModel() is only available on VITURE Neckband");
                return VitureGlassesModel.Unknown;
#endif
            }

            /// <summary>
            /// Sets the electrochromic darkness level of the glasses lenses.
            /// Current glasses models treat this as on/off: 0.0 = off, any other value = on.
            /// Future models will support multiple darkness levels.
            /// </summary>
            /// <param name="level">Darkness from 0.0 (transparent) to 1.0 (dark). Values outside this range are clamped.</param>
            public static void SetElectrochromicLevel(float level)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                VitureNativeApi.Glasses.SetElectrochromicLevel(Mathf.Clamp01(level));
#else
                Debug.LogWarning("VitureXR.Glasses.SetElectrochromicLevel(float) is only available on VITURE Neckband");
#endif
            }
        }
        
        /// <summary>
        /// Advanced rendering controls
        /// </summary>
        public static class Rendering
        {
            /// <summary>
            /// Enables or disables half frame rate rendering for performance optimization.
            /// When enabled, reduces rendering frame rate by half (e.g., 90fps to 45fps).
            /// Animation will appear less smooth.
            /// </summary>
            /// <param name="enabled">True to enable half frame rate, false to disable.</param>
            public static void SetHalfFrameRate(bool enabled)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                VitureNativeApi.Rendering.SetHalfFrameRate(enabled);
#else
                Debug.LogWarning("VitureXR.Rendering.SetHalfFrameRate(bool) is only available on VITURE Neckband");
#endif
            }
            
            /// <summary>
            /// Enables or disables time warp only rendering mode.
            /// When enabled, stops Unity rendering and uses only time warp
            /// to display the last rendered frame with head tracking compensation.
            /// This is only for testing purposes.
            /// </summary>
            /// <param name="enabled">True to enable time warp only mode, false to disable.</param>
            public static void SetTimeWarpOnlyMode(bool enabled)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                VitureNativeApi.Rendering.SetTimeWarpOnlyMode(enabled);
#else
                Debug.LogWarning("VitureXR.Rendering.SetTimeWarpOnlyMode(bool) is only available on VITURE Neckband");
#endif
            }
        }
        
        /// <summary>
        /// Controls head tracking functionality and provides device capability information.
        /// </summary>
        public static class HeadTracking
        {
            /// <summary>
            /// Gets the head tracking capability of the currently connected glasses.
            /// </summary>
            /// <returns>Head tracking capability (3DoF or 6DoF) based on the connected glasses model.</returns>
            public static VitureHeadTrackingCapability GetHeadTrackingCapability()
            {
                switch (Glasses.GetGlassesModel())
                {
                    case VitureGlassesModel.One:
                        return VitureHeadTrackingCapability.ThreeDoF;
                    case VitureGlassesModel.Pro:
                        return VitureHeadTrackingCapability.ThreeDoF;
                    case VitureGlassesModel.Luma:
                        return VitureHeadTrackingCapability.ThreeDoF;
                    case VitureGlassesModel.LumaUltra:
                        return VitureHeadTrackingCapability.SixDoF;
                    case VitureGlassesModel.Beast:
                        return VitureHeadTrackingCapability.ThreeDoF;
                    default:
                        return VitureHeadTrackingCapability.ThreeDoF;
                }
            }

            /// <summary>
            /// Resets the SLAM origin to the current head position and orientation.
            /// This recalibrates the tracking reference and may take a few seconds.
            /// </summary>
            public static void ResetOrigin()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                VitureNativeApi.HeadTracking.ResetOrigin();
#else
                Debug.LogWarning("VitureXR.HeadTracking.ResetOrigin() is only available on VITURE Neckband");
#endif
            }
        }
        
        /// <summary>
        /// Controls the hand tracking algorithm.
        /// Unity XR Hands package must be installed to use this feature.
        /// </summary>
        public static class HandTracking
        {
            /// <summary>
            /// Gets whether hand tracking is currently active.
            /// </summary>
            public static bool IsActive => s_IsActive;
            
            private static bool s_IsActive;
            
            /// <summary>
            /// Sets the hand tracking filter mode.
            /// Responsive has lower latency but more jitter. Stable is smoother but slightly delayed.
            /// </summary>
            public static VitureHandFilterMode filterMode
            {
#if INCLUDE_UNITY_XR_HANDS
                set => VitureHandFilter.SetMode(value);
#else
                set => Debug.LogWarning("XR Hands package (com.unity.xr.hands) is required");
#endif
            }

            /// <summary>
            /// Starts the VITURE XR hand algorithm.
            /// </summary>
            public static void Start()
            {
#if UNITY_EDITOR
                Debug.LogWarning("VitureXR.HandTracking.Start() is only available on VITURE Neckband");
                s_IsActive = true;
#elif INCLUDE_UNITY_XR_HANDS
                if (s_IsActive)
                    return;

                s_IsActive = true;
                VitureNativeApi.HandTracking.Start();
#else
                Debug.LogError("XR Hands package (com.unity.xr.hands) is required to use XR Hand Subsystem");
#endif
            }

            /// <summary>
            /// Stops the VITURE XR hand algorithm.
            /// </summary>
            public static void Stop()
            {
#if UNITY_EDITOR
                Debug.LogWarning("VitureXR.HandTracking.Stop() is only available on VITURE Neckband");
                s_IsActive = false;
#elif INCLUDE_UNITY_XR_HANDS
                if (!s_IsActive)
                    return;
                
                VitureNativeApi.HandTracking.Stop();
                s_IsActive = false;
#else
                Debug.LogError("XR Hands package (com.unity.xr.hands) is required to use XR Hand Subsystem");
#endif
            }
        }

        /// <summary>
        /// Controls XR content capture functionality.
        /// Currently supports first-person mixed reality recording with both virtual and real-world layers.
        /// </summary>
        public static class Capture
        {
            /// <summary>
            /// Gets whether a recording is currently in progress.
            /// </summary>
            public static bool isRecording => s_IsRecording;

            private static bool s_IsRecording;
            
            /// <summary>
            /// Invoked when recording starts successfully.
            /// </summary>
            public static event Action recordingStartSuccess;

            /// <summary>
            /// Invoked when recording fails to start.
            /// Parameters: errorCode, errorMessage.
            /// </summary>
            public static event Action<int, string> recordingStartFailure;

            /// <summary>
            /// Invoked when recording is saved successfully.
            /// Parameter: filePath.
            /// </summary>
            public static event Action<string> recordingSaveSuccess;

            /// <summary>
            /// Invoked when recording fails to save.
            /// Parameters: errorCode, errorMessage.
            /// </summary>
            public static event Action<int, string> recordingSaveFailure;
            
            /// <summary>
            /// Starts recording XR content with the specified capture options.
            /// At least one visual layer (virtual or real-world) must be enabled.
            /// Real-world layer capture requires VITURE Luma, Luma Ultra, or Beast glasses.
            /// Note: Audio capture (captureAppAudio and captureMicrophoneAudio) is not currently supported and will be
            /// available in future releases.
            /// </summary>
            /// <param name="captureVirtualLayer">If true, captures Unity-rendered content.</param>
            /// <param name="captureRealWorldLayer">If true, captures physical RGB camera feed.</param>
            /// <param name="captureAppAudio">If true, captures application audio output. (Not currently supported)</param>
            /// <param name="captureMicrophoneAudio">If true, captures microphone audio input. (Not currently supported)</param>
            public static void StartRecording(bool captureVirtualLayer = true,
                                              bool captureRealWorldLayer = true,
                                              bool captureAppAudio = false,
                                              bool captureMicrophoneAudio = false)
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (s_IsRecording)
                {
                    Debug.LogWarning("[VitureXR] Recording already started, ignoring start request");
                    return;
                }

                if (captureRealWorldLayer)
                {
                    VitureGlassesModel glassesModel = Glasses.GetGlassesModel();
                    if (glassesModel != VitureGlassesModel.Luma &&
                        glassesModel != VitureGlassesModel.LumaUltra &&
                        glassesModel != VitureGlassesModel.Beast)
                    {
                        Debug.LogWarning($"[VitureXR] Cannot capture real-world layer: Current glasses model ({glassesModel}) does not have an RGB camera. " +
                                         $"Real-world capture requires VITURE Luma, Luma Ultra, or Beast glasses.");
                        captureRealWorldLayer = false;
                    }
                }
                
                if (!captureVirtualLayer && !captureRealWorldLayer)
                {
                    Debug.LogError("[VitureXR] Cannot start recording: At least one visual layer must be enabled (captureVirtualLayer or captureRealWorldLayer)");
                    recordingStartFailure?.Invoke(-1, "At least one visual layer must be enabled");
                    return;
                }
                
                s_IsRecording = true;
                VitureNativeApi.Capture.StartRecording(captureVirtualLayer,
                                                       captureRealWorldLayer,
                                                       captureAppAudio,
                                                       captureMicrophoneAudio);
#else
                s_IsRecording = true;
                RecordingStartSuccessCallback();
                Debug.LogWarning("VitureXR.Capture.StartRecording() is only available on VITURE Neckband");
#endif
            }

            /// <summary>
            /// Stops the current recording and saves the video file.
            /// </summary>
            public static void StopRecording()
            {
#if UNITY_ANDROID && !UNITY_EDITOR
                if (!s_IsRecording)
                {
                    Debug.LogWarning("Recording not started yet, ignoring stop request");
                    return;
                }

                s_IsRecording = false;
                VitureNativeApi.Capture.StopRecording();
#else
                s_IsRecording = false;
                RecordingSaveSuccessCallback("/default/");
                Debug.LogWarning("VitureXR.Capture.StopRecording() is only available on VITURE Neckband");
#endif
            }

            [AOT.MonoPInvokeCallback(typeof(Action))]
            internal static void RecordingStartSuccessCallback()
            {
                s_MainThreadContext.Post(_ =>
                {
                    recordingStartSuccess?.Invoke();
                }, null);
            }

            [AOT.MonoPInvokeCallback(typeof(Action<int, string>))]
            internal static void RecordingStartFailureCallback(int errorCode, string errorMessage)
            {
                s_MainThreadContext.Post(_ =>
                {
                    s_IsRecording = false;
                    recordingStartFailure?.Invoke(errorCode, errorMessage);
                }, null);
            }

            [AOT.MonoPInvokeCallback(typeof(Action<string>))]
            internal static void RecordingSaveSuccessCallback(string filePath)
            {
                s_MainThreadContext.Post(_ =>
                {
                    recordingSaveSuccess?.Invoke(filePath);
                }, null);
            }

            [AOT.MonoPInvokeCallback(typeof(Action<int, string>))]
            internal static void RecordingSaveFailureCallback(int errorCode, string errorMessage)
            {
                s_MainThreadContext.Post(_ =>
                {
                    recordingSaveFailure?.Invoke(errorCode,errorMessage);
                }, null);
            }
        }
    }
}
