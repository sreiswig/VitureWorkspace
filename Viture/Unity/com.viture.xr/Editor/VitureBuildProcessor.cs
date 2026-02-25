using System;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Viture.XR.Editor
{
    public class VitureBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            CleanOldSettings();

            if (!EditorBuildSettings.TryGetConfigObject(VitureConstants.k_SettingsKey, out VitureSettings settings) ||
                settings == null)
                return;

            var preloadedAssets = PlayerSettings.GetPreloadedAssets();

            if (preloadedAssets == null)
            {
                PlayerSettings.SetPreloadedAssets(new UnityEngine.Object[] { settings });
                return;
            }

            if (!preloadedAssets.Contains(settings))
            {
                var assets = preloadedAssets.ToList();
                assets.Add(settings);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            CleanOldSettings();
        }

        private void CleanOldSettings()
        {
            var preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null || preloadedAssets.Length == 0)
                return;

            var assetsToKeep = preloadedAssets
                .Where(asset => asset != null && !(asset is VitureSettings))
                .ToArray();

            if (assetsToKeep.Length != preloadedAssets.Length)
                PlayerSettings.SetPreloadedAssets(assetsToKeep);
        }

        public void OnPostGenerateGradleAndroidProject(string gradleProjectPath)
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                return;

            if (!EditorBuildSettings.TryGetConfigObject(VitureConstants.k_SettingsKey, out VitureSettings settings) ||
                settings == null)
                return;

            string manifestPath = Path.Combine(gradleProjectPath, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestPath))
            {
                Debug.LogError($"VitureBuildProcessor: AndroidManifest.xml not found. Path: {manifestPath}");
                return;
            }

            var manifestTool = new ManifestXmlTool(manifestPath);

            try
            {
                manifestTool.AddMetadata("com.viture.xr.sdk_version", GetPackageVersion());
                manifestTool.AddMetadata("com.viture.xr.min_os_version", VitureConstants.k_MinimumOSVersion);
                if (settings.CameraPermission)
                {
                    manifestTool.AddPermission("android.permission.CAMERA");
                }
                switch (settings.AppGlassesSupport)
                {
                    case VitureAppGlassesSupport.SixDoFOnly:
                        manifestTool.AddMetadata("com.viture.xr.dof", "6dof");
                        break;
                    case VitureAppGlassesSupport.Both:
                        manifestTool.AddMetadata("com.viture.xr.dof", "both");
                        break;
                }

                manifestTool.Save();
            }
            catch (Exception e)
            {
                Debug.LogError($"VitureBuildProcessor: Failed to inject permission/meta-data. Error: {e.Message}");
            }
            finally
            {
                manifestTool.Dispose();
            }
        }

        private static string GetPackageVersion()
        {
            try
            {
                var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.viture.xr");
                if (packageInfo != null)
                {
                    return packageInfo.version;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to read package version: {e.Message}");
            }

            return VitureConstants.k_DefaultSdkVersion;
        }
    }

    public class ManifestXmlTool : IDisposable
    {
        private readonly XmlDocument doc;
        private readonly string manifestPath;
        private readonly XmlNamespaceManager nsMgr;
        private const string AndroidNamespace = "http://schemas.android.com/apk/res/android";

        public ManifestXmlTool(string manifestPath)
        {
            this.manifestPath = manifestPath;
            doc = new XmlDocument();
            doc.Load(manifestPath);
            nsMgr = new XmlNamespaceManager(doc.NameTable);
            nsMgr.AddNamespace("android", AndroidNamespace);
        }

        public void AddPermission(string permissionName)
        {
            string trimmedPermission = permissionName?.Trim() ?? "";
            if (string.IsNullOrEmpty(trimmedPermission))
            {
                return;
            }

            XmlNode manifestNode = doc.SelectSingleNode("/manifest");
            if (manifestNode == null)
            {
                Debug.LogError("Manifest node <manifest> not found in AndroidManifest.xml");
                return;
            }

            XmlNode existingPerm = doc.SelectSingleNode($"/manifest/uses-permission[@android:name='{trimmedPermission}']", nsMgr);
            if (existingPerm == null)
            {
                XmlElement newPermNode = doc.CreateElement("uses-permission");
                newPermNode.SetAttribute("name", AndroidNamespace, trimmedPermission);
                manifestNode.AppendChild(newPermNode);
            }
        }

        public void AddMetadata(string metaName, string metaValue)
        {
            if (string.IsNullOrEmpty(metaName))
            {
                return;
            }

            XmlNode appNode = doc.SelectSingleNode("/manifest/application");
            if (appNode == null)
            {
                Debug.LogError("Application node <application> not found in AndroidManifest.xml");
                return;
            }

            string trimmedName = metaName.Trim();
            string trimmedValue = metaValue?.Trim() ?? "";

            XmlNode existingMeta = doc.SelectSingleNode($"/manifest/application/meta-data[@android:name='{trimmedName}']", nsMgr);

            if (existingMeta != null)
            {
                ((XmlElement)existingMeta).SetAttribute("value", AndroidNamespace, trimmedValue);
            }
            else
            {
                XmlElement newMetaNode = doc.CreateElement("meta-data");
                newMetaNode.SetAttribute("name", AndroidNamespace, trimmedName);
                newMetaNode.SetAttribute("value", AndroidNamespace, trimmedValue);
                appNode.AppendChild(newMetaNode);
            }
        }

        public void Save()
        {
            doc.Save(manifestPath);
        }

        public void Dispose()
        {
            doc?.RemoveAll();
        }
    }
}
