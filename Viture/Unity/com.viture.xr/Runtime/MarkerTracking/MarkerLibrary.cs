using UnityEngine;

namespace Viture.XR
{
    /// <summary>
    /// Base class for marker libraries used by <see cref="VitureTrackedMarkerManager"/>.
    /// Concrete implementations define different marker types (e.g., <see cref="ArucoMarkerLibrary"/>).
    /// </summary>
    public abstract class MarkerLibrary : ScriptableObject
    {
        public abstract int count { get; }
        
        public abstract void GetMarkerArrays(
            out int[] objectIds,
            out int[] dictionaries,
            out int[] markerIds,
            out float[] markerLengths);
    }
}
