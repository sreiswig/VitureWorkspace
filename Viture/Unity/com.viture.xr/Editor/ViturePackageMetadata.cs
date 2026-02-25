using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management.Metadata;

namespace Viture.XR.Editor
{
    class ViturePackage : IXRPackage
    {
        class VitureLoaderMetadata : IXRLoaderMetadata
        {
            public string loaderName { get; set; }
            public string loaderType { get; set; }
            public List<BuildTargetGroup> supportedBuildTargets { get; set; }
        }
        
        class ViturePackageMetadata : IXRPackageMetadata
        {
            public string packageName { get; set; }
            public string packageId { get; set; }
            public string settingsType { get; set; }
            public List<IXRLoaderMetadata> loaderMetadata { get; set; }
        }
        
        static IXRPackageMetadata s_Metadata = new ViturePackageMetadata()
        {
            packageName = "VITURE XR Plugin",
            packageId = "com.viture.xr",
            settingsType = typeof(VitureSettings).FullName,
            loaderMetadata = new List<IXRLoaderMetadata>()
            {
                new VitureLoaderMetadata()
                {
                    loaderName = "VITURE",
                    loaderType = typeof(VitureLoader).FullName,
                    supportedBuildTargets = new List<BuildTargetGroup>()
                    {
                        BuildTargetGroup.Android
                    }
                }
            }
        };
        
        public IXRPackageMetadata metadata => s_Metadata;
        
        public bool PopulateNewSettingsInstance(ScriptableObject obj)
        {
            return true;
        }
    }
}
