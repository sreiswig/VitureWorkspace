using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Viture.XR
{
    /// <summary>
    /// A manager for <see cref="VitureTrackedMarker"/>s.
    /// Detects and tracks fiducial markers in the physical environment.
    /// </summary>
    /// <remarks>
    /// Subscribe to <see cref="trackedMarkersChanged"/> to receive tracking updates each frame.
    /// Requires a <see cref="MarkerLibrary"/> to define which markers to track.
    /// </remarks>
    public class VitureTrackedMarkerManager : MonoBehaviour
    {
        [SerializeField]
        private MarkerLibrary m_MarkerLibrary;
        
        private bool m_SessionCreated;

        private bool m_TimestampCalibrated;
        private float m_TimestampOffset;

        private bool m_HasDetectedMarkers;
        private int m_ConsecutiveEmptyFrames;
        
        private const int k_MaxMarkers = 8;
        
        private const int k_EmptyFramesThreshold = 10;

        private static readonly Quaternion k_RotationOffset = Quaternion.Euler(-90f, 0f, 0f);

        /// <summary>
        /// Invoked each frame with all currently tracked markers.
        /// </summary>
        public UnityEvent<List<VitureTrackedMarker>> trackedMarkersChanged;
        
        private void OnEnable()
        {
            CreateSession();
        }

        private void OnDisable()
        {
            DestroySession();
        }

        private void Update()
        {
            if (m_SessionCreated)
                UpdateTrackedMarkers();
        }
        
        private void CreateSession()
        {
            if (m_SessionCreated)
                return;

            if (m_MarkerLibrary == null || m_MarkerLibrary.count == 0)
            {
                Debug.LogWarning("[VitureTrackedMarkerManager] MarkerLibrary is not assigned or empty");
                return;
            }

            m_MarkerLibrary.GetMarkerArrays(
                out int[] objectIds,
                out int[] dictionaries,
                out int[] markerIds,
                out float[] markerLengths);

            // Temporarily override all lengths to zero
            for (int i = 0; i < markerLengths.Length; i++)
                markerLengths[i] = 0f;

#if UNITY_ANDROID && !UNITY_EDITOR
            m_SessionCreated = VitureNativeApi.MarkerTracking.CreateSession(objectIds, dictionaries, markerIds, markerLengths, objectIds.Length);

            if (!m_SessionCreated)
            {
                Debug.LogError("[VitureTrackedMarkerManager] Failed to create marker tracking session");
                return;
            }
            
            VitureNativeApi.MarkerTracking.Start();
#else
            m_SessionCreated = true;
#endif
        }

        private void DestroySession()
        {
            if (!m_SessionCreated)
                return;

#if UNITY_ANDROID && !UNITY_EDITOR
            VitureNativeApi.MarkerTracking.Stop();
            VitureNativeApi.MarkerTracking.DestroySession();
#endif
            m_SessionCreated = false;
            m_HasDetectedMarkers = false;
        }
        
        private void UpdateTrackedMarkers()
        {
            MarkerTrackingData[] rawData = new MarkerTrackingData[k_MaxMarkers];
            
#if UNITY_ANDROID && !UNITY_EDITOR
            int count = VitureNativeApi.MarkerTracking.ReadData(rawData);
#else
            int count = 0;
#endif

            if (count > 0)
            {
                m_HasDetectedMarkers = true;
                m_ConsecutiveEmptyFrames = 0;
                
                var trackedMarkers = new List<VitureTrackedMarker>();

                for (int i = 0; i < count; i++)
                {
                    if (TryParseMarkerData(rawData[i], out VitureTrackedMarker marker))
                        trackedMarkers.Add(marker);
                }
            
                trackedMarkersChanged?.Invoke(trackedMarkers);
            }
            else
            {
                m_ConsecutiveEmptyFrames++;
                
                if (m_HasDetectedMarkers && m_ConsecutiveEmptyFrames == k_EmptyFramesThreshold)
                    trackedMarkersChanged?.Invoke(new List<VitureTrackedMarker>());
            }
        }

        private bool TryParseMarkerData(MarkerTrackingData data, out VitureTrackedMarker marker)
        {
            marker = default;

            if (data.timestamp <= 0 || data.objectId < 0 || data.markerLength <= 0)
                return false;
            
            float qMag = data.qw * data.qw + data.qx * data.qx + data.qy * data.qy + data.qz * data.qz;
            if (qMag < 0.99f || qMag > 1.01f)
                return false;
            
            Vector3 position = new Vector3(data.tx, data.ty, -data.tz);
            Quaternion rotation = new Quaternion(-data.qx, -data.qy, data.qz, data.qw) * k_RotationOffset;
            
            if (!m_TimestampCalibrated)
            {
                m_TimestampOffset = Time.time - (float)(VitureNativeApi.System.GetMonotonicTimeNs() / 1e9);
                m_TimestampCalibrated = true;
            }
            
            marker = new VitureTrackedMarker
            {
                objectId = data.objectId,
                markerLength = data.markerLength,
                pose = new Pose(position, rotation),
                timestamp = (float)(data.timestamp / 1e9) + m_TimestampOffset
            };

            return true;
        }
    }
}
