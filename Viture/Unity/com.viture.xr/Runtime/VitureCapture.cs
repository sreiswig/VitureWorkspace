using System;
using System.Collections.Generic;
using UnityEngine;

namespace Viture.XR
{
    internal static class VitureCapture
    {
        private static readonly Dictionary<CaptureCameraType, CaptureCamera> s_CaptureCameras = new();
        
        private static GameObject s_RootObject;

        private const string k_NoCaptureLayer = "VitureNoCapture";
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            s_RootObject = new GameObject("VitureXR_CaptureCameras");
            s_RootObject.hideFlags = HideFlags.HideAndDontSave;
            UnityEngine.Object.DontDestroyOnLoad(s_RootObject);
         
#if UNITY_ANDROID && !UNITY_EDITOR
            VitureNativeApi.Capture.RegisterCallbacks(
                CreateCaptureCameraCallback,
                DestroyCaptureCameraCallback,
                CreateDeviceCameraTextureCallback,
                DestroyDeviceCameraTextureCallback,
                InitializeRecordingCompositorContextCallback,
                ShutdownRecordingCompositorContextCallback,
                InitializeStreamingCompositorContextCallback,
                ShutdownStreamingCompositorContextCallback,
                UpdateAndRenderCallback);
#endif
        }
        
        [AOT.MonoPInvokeCallback(typeof(Action<int, int, int, float>))]
        internal static void CreateCaptureCameraCallback(int type, int width, int height, float verticalFov)
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                CreateCaptureCamera(type, width, height, verticalFov);
            }, null);
        }
        
        [AOT.MonoPInvokeCallback(typeof(Action<int>))]
        internal static void DestroyCaptureCameraCallback(int type)
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                DestroyCaptureCamera(type);
            }, null);
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void CreateDeviceCameraTextureCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetCreateDeviceCameraTextureFunc(), 0);
            }, null);
        }
        
        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void DestroyDeviceCameraTextureCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetDestroyDeviceCameraTextureFunc(), 0);
            }, null);
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void InitializeRecordingCompositorContextCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetInitializeRecordingCompositorContextFunc(), 0);
            }, null);
        }
        
        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void ShutdownRecordingCompositorContextCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetShutdownRecordingCompositorContextFunc(), 0);
            }, null);
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void InitializeStreamingCompositorContextCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetInitializeStreamingCompositorContextFunc(), 0);
            }, null);
        }

        [AOT.MonoPInvokeCallback(typeof(Action))]
        internal static void ShutdownStreamingCompositorContextCallback()
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                GL.IssuePluginEvent(VitureNativeApi.Capture.GetShutdownStreamingCompositorContextFunc(), 0);
            }, null);
        }
        
        [AOT.MonoPInvokeCallback(typeof(Action<int, float, float, float, float, float, float, float>))]
        internal static void UpdateAndRenderCallback(int type,  
            float posX, float posY, float posZ, 
            float rotX, float rotY, float rotZ, float rotW,
            float verticalFov)
        {
            VitureXR.s_MainThreadContext.Post(_ =>
            {
                CaptureCameraType captureType = (CaptureCameraType)type;
                
                if (captureType == CaptureCameraType.Recording)
                    GL.IssuePluginEvent(VitureNativeApi.Capture.GetUpdateDeviceCameraTextureFunc(), 0);
                
                UpdateCameraAndRender(type, new Vector3(posX, posY, posZ), new Quaternion(rotX, rotY, rotZ, rotW), verticalFov);

                GL.IssuePluginEvent(
                    captureType == CaptureCameraType.Recording
                        ? VitureNativeApi.Capture.GetCompositeRecordingFrameFunc()
                        : VitureNativeApi.Capture.GetBlitToEncoderSurfaceFunc(), 0);
            }, null);
        }
        
        private static void CreateCaptureCamera(int typeIndex, int width, int height, float verticalFov)
        {
            CaptureCameraType type = (CaptureCameraType)typeIndex;
            if (s_CaptureCameras.ContainsKey(type))
            {
                Debug.LogWarning($"[VitureXR] Capture camera {type} already exists.");
                return;
            }
            
            GameObject cameraObj = new GameObject($"CaptureCamera_{type}");
            cameraObj.transform.SetParent(s_RootObject.transform);
            cameraObj.hideFlags = HideFlags.HideAndDontSave;

            Camera cam = cameraObj.AddComponent<Camera>();
            cam.enabled = false;
            cam.fieldOfView = verticalFov;
            cam.aspect = width / (float)height;
            cam.nearClipPlane = 0.08f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.depth = -99;
            int noCaptureLayer = LayerMask.NameToLayer(k_NoCaptureLayer);
            cam.cullingMask = noCaptureLayer == -1 ? -1 : ~(1 << noCaptureLayer);

            RenderTexture rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            rt.name = $"CaptureCamera_{type}_RT";
            rt.antiAliasing = 1;
            rt.Create();
            cam.targetTexture = rt;

            CaptureCamera captureCam = new CaptureCamera
            {
                gameObject = cameraObj,
                camera = cam,
                renderTexture = rt,
                type = type
            };
            s_CaptureCameras.Add(type, captureCam);

            IntPtr renderTexturePtr = rt.GetNativeTexturePtr();
            VitureNativeApi.Capture.OnCaptureCameraCreated(typeIndex, renderTexturePtr);
            
            Debug.Log($"[VitureXR] Capture camera <{type}> created successfully");
        }
        
        private static void DestroyCaptureCamera(int typeIndex)
        {
            CaptureCameraType type = (CaptureCameraType)typeIndex;

            if (s_CaptureCameras.TryGetValue(type, out CaptureCamera captureCam))
            {
                captureCam.Cleanup();
                s_CaptureCameras.Remove(type);
                Debug.Log($"[VitureXR] Capture camera <{type}> destroyed");
            }
            else
            {
                Debug.LogWarning($"[VitureXR] Cannot destroy - capture camera {type} not found");
            }
        }

        private static void UpdateCameraAndRender(int typeIndex, Vector3 position, Quaternion rotation, float verticalFov)
        {
            CaptureCameraType cameraType = (CaptureCameraType)typeIndex;
            if (s_CaptureCameras.TryGetValue(cameraType, out var cam))
            {
                cam.gameObject.transform.SetPositionAndRotation(position, rotation);
                if (cameraType == CaptureCameraType.Streaming)
                    cam.camera.fieldOfView = verticalFov;
                cam.camera.Render();
            }
        }
        
        private enum CaptureCameraType
        {
            Recording = 0,
            Streaming = 1
        }
        
        private class CaptureCamera
        {
            public GameObject gameObject;
            public Camera camera;
            public RenderTexture renderTexture;
            public CaptureCameraType type;

            public void Cleanup()
            {
                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.Destroy(renderTexture);
                    renderTexture = null;
                }

                if (gameObject != null)
                {
                    UnityEngine.Object.Destroy(gameObject);
                    gameObject = null;
                }

                camera = null;
            }
        }
    }
}
