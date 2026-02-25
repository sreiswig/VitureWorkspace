using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        internal static class System
        {
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_System_GetMonotonicTimeNs")]
            internal static extern long GetMonotonicTimeNs();
        }
    }
}
