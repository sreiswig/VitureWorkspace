using UnityEngine;

namespace Viture.XR
{
    /// <summary>
    /// Represents a detected marker with its pose and tracking data.
    /// </summary>
    public struct VitureTrackedMarker
    {
        /// <summary>
        /// Object ID matching the value defined in the <see cref="MarkerLibrary"/>.
        /// </summary>
        public int objectId;

        /// <summary>
        /// Physical size of the marker in meters.
        /// </summary>
        public float markerLength;

        /// <summary>
        /// Position and rotation of the marker.
        /// </summary>
        public Pose pose;

        /// <summary>
        /// Timestamp when this marker was detected, in seconds since game start (aligned with <see cref="Time.time"/>).
        /// </summary>
        public float timestamp;
    }
}
