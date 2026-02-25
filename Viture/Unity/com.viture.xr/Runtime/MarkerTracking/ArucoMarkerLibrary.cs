using System;
using UnityEngine;

namespace Viture.XR
{
    /// <summary>
    /// Configuration for a single ArUco marker to track.
    /// Defines the marker's identity and physical properties.
    /// </summary>
    [Serializable]
    public struct ArucoMarkerConfig
    {
        [Tooltip("Developer-defined identifier for this marker. Returned in VitureTrackedMarker.objectId to identify detected markers.")]
        public int objectId;
        
        [Tooltip("ArUco dictionary used to generate this marker. Must match the dictionary used to generate the physical marker.")]
        public ArucoMarkerDictionary dictionary;
        
        [Tooltip("Marker ID within the dictionary. Must match the physical marker's ID.")]
        public int markerId;
        
        [Tooltip("Physical length of the marker side in meters.")]
        public float markerLength;
    }

    /// <summary>
    /// ScriptableObject library containing ArUco marker configurations.
    /// </summary>
    /// <remarks>
    /// Generate physical markers at: https://chev.me/arucogen/
    /// </remarks>
    [CreateAssetMenu(menuName = "VITURE/ArUco Marker Library")]
    public class ArucoMarkerLibrary : MarkerLibrary
    {
        [SerializeField] 
        private ArucoMarkerConfig[] m_Markers;

        public override int count => m_Markers?.Length ?? 0;

        public override void GetMarkerArrays(
            out int[] objectIds,
            out int[] dictionaries,
            out int[] markerIds,
            out float[] markerLengths)
        {
            int markerCount = count;
            objectIds = new int[markerCount];
            dictionaries = new int[markerCount];
            markerIds = new int[markerCount];
            markerLengths = new float[markerCount];

            for (int i = 0; i < markerCount; i++)
            {
                objectIds[i] = m_Markers[i].objectId;
                dictionaries[i] = (int)m_Markers[i].dictionary;
                markerIds[i] = m_Markers[i].markerId;
                markerLengths[i] = m_Markers[i].markerLength;
            }
        }
    }
    
    /// <summary>
    /// ArUco marker dictionary types.
    /// Each dictionary defines a set of unique marker patterns with different grid sizes (4×4 to 7×7).
    /// </summary>
    /// <remarks>
    /// Smaller grids (4×4) offer faster detection; larger grids (7×7) provide better robustness.
    /// Recommended: <see cref="DICT_4X4_50"/> for most applications (optimal balance of speed and reliability).
    /// For more information, see: https://docs.opencv.org/4.x/d9/d6a/group__aruco.html
    /// </remarks>
    public enum ArucoMarkerDictionary
    {
        /// <summary>
        /// 4x4 grid, 50 markers. Fastest detection, recommended default.
        /// </summary>
        DICT_4X4_50 = 0,

        /// <summary>
        /// 4x4 grid, 100 markers. Fast with more variety.
        /// </summary>
        DICT_4X4_100 = 1,

        /// <summary>
        /// 4x4 grid, 250 markers. Fast with extensive variety.
        /// </summary>
        DICT_4X4_250 = 2,

        /// <summary>
        /// 4x4 grid, 1000 markers. Fast with maximum variety.
        /// </summary>
        DICT_4X4_1000 = 3,

        /// <summary>
        /// 5x5 grid, 50 markers. Balanced speed and robustness.
        /// </summary>
        DICT_5X5_50 = 4,

        /// <summary>
        /// 5x5 grid, 100 markers. Balanced speed and robustness.
        /// </summary>
        DICT_5X5_100 = 5,

        /// <summary>
        /// 5x5 grid, 250 markers. Balanced speed and robustness.
        /// </summary>
        DICT_5X5_250 = 6,

        /// <summary>
        /// 5x5 grid, 1000 markers. High robustness and variety.
        /// </summary>
        DICT_5X5_1000 = 7,

        /// <summary>
        /// 6x6 grid, 50 markers. Good robustness, moderate speed.
        /// </summary>
        DICT_6X6_50 = 8,

        /// <summary>
        /// 6x6 grid, 100 markers. Good robustness.
        /// </summary>
        DICT_6X6_100 = 9,

        /// <summary>
        /// 6x6 grid, 250 markers. High robustness.
        /// </summary>
        DICT_6X6_250 = 10,

        /// <summary>
        /// 6x6 grid, 1000 markers. High robustness and variety.
        /// </summary>
        DICT_6X6_1000 = 11,

        /// <summary>
        /// 7x7 grid, 50 markers. Maximum robustness, slower detection.
        /// </summary>
        DICT_7X7_50 = 12,

        /// <summary>
        /// 7x7 grid, 100 markers. Maximum robustness and variety.
        /// </summary>
        DICT_7X7_100 = 13,

        /// <summary>
        /// 7x7 grid, 250 markers. Maximum robustness and variety.
        /// </summary>
        DICT_7X7_250 = 14,

        /// <summary>
        /// 7x7 grid, 1000 markers. Ultimate robustness and variety.
        /// </summary>
        DICT_7X7_1000 = 15
    }
}
