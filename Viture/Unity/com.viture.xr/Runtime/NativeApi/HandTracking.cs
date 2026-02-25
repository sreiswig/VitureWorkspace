using System;
using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        internal static class HandTracking
        {
            internal const int k_HandDataLength = 374;
            internal const int k_HandJointCount = 26;
            internal const int k_PalmFacingOffset = 185;
            internal const int k_GestureOffset = 186;

            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_HandTracking_Start")]
            internal static extern void Start();

            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_HandTracking_Stop")]
            internal static extern void Stop();

            [DllImport(k_LibName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "VitureUnityXR_HandTracking_ReadData")]
            internal static extern bool ReadData(
                [Out] [MarshalAs(UnmanagedType.LPArray, SizeConst = k_HandDataLength)] float[] dataPtr);
        }
    }

    internal enum VitureHandJointID
    {
        ThumbTip = 0,
        IndexTip = 1,
        MiddleTip = 2,
        RingTip = 3,
        LittleTip = 4,
        Wrist = 5,
        ThumbProximal = 6,
        ThumbDistal = 7,
        IndexProximal = 8,
        IndexIntermediate = 9,
        IndexDistal = 10,
        MiddleProximal = 11,
        MiddleIntermediate = 12,
        MiddleDistal = 13,
        RingProximal = 14,
        RingIntermediate = 15,
        RingDistal = 16,
        LittleProximal = 17,
        LittleIntermediate = 18,
        LittleDistal = 19,
        Palm = 20,
        None = 21
    }
}
