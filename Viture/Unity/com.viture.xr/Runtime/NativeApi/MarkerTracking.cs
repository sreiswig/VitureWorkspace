using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        internal static class MarkerTracking
        {
            [DllImport(k_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "VitureUnityXR_MarkerTracking_CreateSession")]
            internal static extern bool CreateSession(
                [In] int[] objectIds,
                [In] int[] dictionaries,
                [In] int[] markerIds,
                [In] float[] markerLengths,
                int length);

            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_MarkerTracking_DestroySession")]
            internal static extern bool DestroySession();
            
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_MarkerTracking_Start")]
            internal static extern void Start();
            
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_MarkerTracking_Stop")]
            internal static extern void Stop();

            [DllImport(k_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "VitureUnityXR_MarkerTracking_ReadData")]
            internal static extern int ReadData([Out] MarkerTrackingData[] dataArray);
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    internal struct MarkerTrackingData
    {
        public ulong timestamp;
        public int objectId;
        public float markerLength;
        public float qw, qx, qy, qz;
        public float tx, ty, tz;
    }
}
