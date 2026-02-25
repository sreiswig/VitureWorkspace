using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        internal static class HeadTracking
        {
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_HeadTracking_ResetOrigin")]
            internal static extern void ResetOrigin();
        }
    }
}
