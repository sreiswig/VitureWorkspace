using System.Runtime.InteropServices;

namespace Viture.XR
{
    internal static partial class VitureNativeApi
    {
        private const string k_LibName = "VitureUnityXR";
        
        internal static class Glasses
        {
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_Glasses_GetGlassesModel")]
            internal static extern int GetGlassesModel();
            
            [DllImport(k_LibName, EntryPoint = "VitureUnityXR_Glasses_SetElectrochromicLevel")]
            internal static extern void SetElectrochromicLevel(float level);
        }
    }
}
