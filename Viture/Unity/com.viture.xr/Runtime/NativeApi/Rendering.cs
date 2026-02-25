using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        internal static class Rendering
        {
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_Rendering_SetHalfFrameRate")]
            internal static extern void SetHalfFrameRate(bool enabled);

            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_Rendering_SetTimeWarpOnlyMode")]
            internal static extern void SetTimeWarpOnlyMode(bool enabled);
        }
    }
}
