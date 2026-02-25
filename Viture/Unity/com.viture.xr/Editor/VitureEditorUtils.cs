using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.PackageManager.UI;
using UnityEditor.SceneManagement;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;

namespace Viture.XR.Editor
{
    internal static class VitureEditorUtils
    {
        internal const BuildTarget k_BuildTarget = BuildTarget.Android;
        internal const AndroidSdkVersions k_MinAndroidSdkVersion = AndroidSdkVersions.AndroidApiLevel33;
        internal const string k_MinAndroidSdkVersionString = "Android 13.0";

        #region Constants - Package Names
        internal const string k_XriPackageName = "com.unity.xr.interaction.toolkit";
        internal const string k_XrHandPackageName = "com.unity.xr.hands";
        #endregion

        #region com.viture.xr
        internal static string s_SDKVersion = "0.6.0";
        internal const string k_SDKPackageName = "com.viture.xr";
        internal const string k_SDKPackageDisplayName = "VITURE XR Plugin";
        internal const string k_SDKSampleStarterAssets = "Starter Assets";
        internal const string k_SDKSampleHandStateDemo = "Hand State Demo";
        internal const string k_SDKSampleMarkerTrackingDemo = "Marker Tracking Demo";
        internal const string k_VitureXROriginPrefabName = "XR Origin (Viture)";
        internal const string k_VitureQuickActionsPrefabName = "Viture Quick Actions";
        internal const string k_VitureMarkerTrackingPrefabName = "Marker Tracking";
        #endregion

        #region Constants - Building Block
        internal const string k_BuildingBlock = "[Building Block]";
        internal const string k_BuildingBlockPathO = "GameObject/VITURE Building Blocks/";
        internal const string k_BuildingBlockPathP = "VITURE/Building Blocks/";
        #endregion

        #region Platform Configuration
        internal static bool IsAndroidPlatform()
        {
            return EditorUserBuildSettings.activeBuildTarget == k_BuildTarget;
        }

        internal static void SwitchToAndroidPlatform()
        {
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, k_BuildTarget);
            EditorUserBuildSettings.selectedBuildTargetGroup = BuildTargetGroup.Android;
        }

        internal static bool IsOpenGLES3Only()
        {
            if (PlayerSettings.GetUseDefaultGraphicsAPIs(k_BuildTarget))
                return false;

            var graphicsApis = PlayerSettings.GetGraphicsAPIs(k_BuildTarget);
            return graphicsApis.Length == 1 && graphicsApis[0] == GraphicsDeviceType.OpenGLES3;
        }

        internal static void SetOpenGLES3Only()
        {
            PlayerSettings.SetUseDefaultGraphicsAPIs(k_BuildTarget, false);
            PlayerSettings.SetGraphicsAPIs(k_BuildTarget, new[] { GraphicsDeviceType.OpenGLES3 });
        }

        internal static bool IsAndroidMinApiLevelConfigured()
        {
            return PlayerSettings.Android.minSdkVersion >= k_MinAndroidSdkVersion;
        }

        internal static void ConfigureAndroidMinApiLevel()
        {
            PlayerSettings.Android.minSdkVersion = k_MinAndroidSdkVersion;
        }
        #endregion

        #region XR Plugin Management
        internal static bool IsViturePluginEnabled()
        {
            var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings == null)
                return false;

            var managerSettings = generalSettings.AssignedSettings;
            return managerSettings != null && managerSettings.activeLoaders.Any(loader => loader is VitureLoader);
        }

        internal static void ConfigureViturePlugin()
        {
            SettingsService.OpenProjectSettings("Project/XR Plug-in Management");
            var buildTargetSettings = AssetDatabase.FindAssets("t:XRGeneralSettingsPerBuildTarget")
                .Select(guid => AssetDatabase.LoadAssetAtPath<XRGeneralSettingsPerBuildTarget>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault();

            if (buildTargetSettings == null)
            {
                buildTargetSettings = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                AssetDatabase.CreateAsset(buildTargetSettings, "Assets/XRGeneralSettingsPerBuildTarget.asset");
            }

            var generalSettings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
            if (generalSettings == null)
            {
                generalSettings = ScriptableObject.CreateInstance<XRGeneralSettings>();
                AssetDatabase.AddObjectToAsset(generalSettings, buildTargetSettings);
                buildTargetSettings.SetSettingsForBuildTarget(BuildTargetGroup.Android, generalSettings);

                var managerSettings = ScriptableObject.CreateInstance<XRManagerSettings>();
                AssetDatabase.AddObjectToAsset(managerSettings, buildTargetSettings);
                generalSettings.Manager = managerSettings;

                EditorUtility.SetDirty(buildTargetSettings);
                AssetDatabase.SaveAssets();
            }

            if (generalSettings.Manager)
            {
                while (generalSettings.Manager.activeLoaders.Count > 0)
                {
                    var loaderName = generalSettings.Manager.activeLoaders[0].GetType().FullName;
                    XRPackageMetadataStore.RemoveLoader(generalSettings.Manager, loaderName, BuildTargetGroup.Android);
                }

                bool success = XRPackageMetadataStore.AssignLoader(generalSettings.Manager, "VitureLoader", BuildTargetGroup.Android);
                if (!success)
                {
                    Debug.LogWarning("Failed to assign VITURE XR loader. Please enable it manually in XR Plug-in Management.");
                }
            }
        }
        #endregion

        #region Input System Configuration
        internal static bool IsVitureInputActionsConfigured()
        {
            var actionGuids = AssetDatabase.FindAssets("t:InputActionAsset Viture Input Actions");
            if (actionGuids.Length == 0)
            {
                return false;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(actionGuids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            return asset != null && InputSystem.actions == asset;
        }

        internal static void ConfigureVitureInputActions()
        {
            var actionGuids = AssetDatabase.FindAssets("t:InputActionAsset Viture Input Actions");
            if (actionGuids.Length == 0)
            {
                Debug.LogWarning("Viture Input Actions asset not found in project");
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(actionGuids[0]);
            var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(assetPath);

            if (asset == null)
            {
                Debug.LogError($"Failed to load InputActionAsset from: {assetPath}");
                return;
            }

            if (InputSystem.actions == asset)
            {
                return;
            }

            InputSystem.actions = asset;
            EditorUtility.SetDirty(InputSystem.settings);
            AssetDatabase.SaveAssets();
        }
        #endregion

        #region Package Management
        private const float k_PackageSearchTimeoutSeconds = 15f;
        private const int k_PackageSearchPollIntervalMs = 200;
        private const float k_PackageInstallTimeoutSeconds = 180f;

        static AddRequest s_PackageAddRequest;

        internal static void InstallOrUpdatePackage(string packageName)
        {
            if (IsPackageInstalled(packageName))
            {
                return;
            }

            var searchRequest = Client.Search(packageName);
            var searchStartTime = DateTime.Now;
            var searchTimeout = TimeSpan.FromSeconds(k_PackageSearchTimeoutSeconds);

            while (searchRequest.Status == StatusCode.InProgress &&
                   DateTime.Now - searchStartTime < searchTimeout)
            {
                Thread.Sleep(k_PackageSearchPollIntervalMs);
            }

            if (searchRequest.Status != StatusCode.Success)
            {
                Debug.LogError($"Failed to search for package '{packageName}': {searchRequest.Error?.message}");
                return;
            }

            if (searchRequest.Result.Length == 0)
            {
                Debug.LogError($"Package '{packageName}' not found in Unity Package Registry.");
                return;
            }

            var packageInfo = searchRequest.Result[0];
            var recommendedVersion = packageInfo.versions.recommended;
            var packageId = $"{packageName}@{recommendedVersion}";

            s_PackageAddRequest = Client.Add(packageId);

            if (s_PackageAddRequest.Error != null)
            {
                Debug.LogError($"Failed to start installation of '{packageId}': {s_PackageAddRequest.Error.message}");
                return;
            }

            var installStartTime = DateTime.Now;
            var installTimeout = TimeSpan.FromSeconds(k_PackageInstallTimeoutSeconds);

            while (!s_PackageAddRequest.IsCompleted &&
                   DateTime.Now - installStartTime < installTimeout)
            {
                Thread.Sleep(k_PackageSearchPollIntervalMs);
            }

            if (s_PackageAddRequest.IsCompleted)
            {
                if (s_PackageAddRequest.Status != StatusCode.Success)
                {
                    Debug.LogError($"Failed to install package '{packageId}': {s_PackageAddRequest.Error?.message}");
                }
            }
            else
            {
                Debug.LogError($"Package installation timed out for '{packageId}' after {k_PackageInstallTimeoutSeconds} seconds.");
            }
        }

        private static bool IsPackageInstalled(string packageName)
        {
            try
            {
                var listRequest = Client.List(true, false);
                var timeout = TimeSpan.FromSeconds(5);
                var startTime = DateTime.Now;
                while (!listRequest.IsCompleted && DateTime.Now - startTime < timeout)
                {
                    Thread.Sleep(50);
                }

                if (listRequest.Status == StatusCode.Success)
                {
                    return listRequest.Result.Any(pkg => pkg.name == packageName);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error checking package installation status: {e.Message}");
            }

            return false;
        }

        internal static bool EnsureSampleImported(string packageName, string packageVersion, string samplesDisplayName, string sampleDisplayName)
        {
            string samplePath = $"Assets/Samples/{samplesDisplayName}";
            if (Directory.Exists(samplePath))
            {
                bool isImported = Directory.GetDirectories(samplePath, $"{sampleDisplayName}*", SearchOption.TopDirectoryOnly).Any() ||
                                  Directory.GetFiles(samplePath, $"{sampleDisplayName}*", SearchOption.AllDirectories).Any();

                if (isImported)
                {
                    return true;
                }
            }

            try
            {
                var samples = Sample.FindByPackage(packageName, packageVersion);
                if (samples == null || !samples.Any())
                {
                    Debug.LogError($"No samples found for package '{packageName}@{packageVersion}'. Package may not be installed.");
                    return false;
                }

                var sample = samples.FirstOrDefault(s => s.displayName == sampleDisplayName);
                if (sample.Equals(default(Sample)))
                {
                    Debug.LogError($"Sample '{sampleDisplayName}' not found in package '{packageName}@{packageVersion}'. " +
                                  $"Available samples: {string.Join(", ", samples.Select(s => s.displayName))}");
                    return false;
                }

                sample.Import(Sample.ImportOptions.OverridePreviousImports);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to import sample '{sampleDisplayName}' from '{packageName}@{packageVersion}': {e.Message}");
                return false;
            }
        }
        #endregion

        #region Scene Utilities
        internal static List<T> FindComponentsInScene<T>() where T : Component
        {
            var activeScene = SceneManager.GetActiveScene();
            var foundComponents = new List<T>();

            var rootObjects = activeScene.GetRootGameObjects();
            foreach (var rootObject in rootObjects)
            {
                var components = rootObject.GetComponentsInChildren<T>(true);
                foundComponents.AddRange(components);
            }

            return foundComponents;
        }

        internal static GameObject FindGameObjectInScene(string objectName)
        {
            if (string.IsNullOrEmpty(objectName))
            {
                return null;
            }

            List<Transform> transforms = FindComponentsInScene<Transform>()
                .Where(component => component.name == objectName)
                .ToList();

            if (transforms.Count > 0)
            {
                return transforms[0].gameObject;
            }

            return null;
        }

        internal static GameObject GetXROriginMainCamera()
        {
            string cameraName = "XR Origin (Viture)";
            string buildingBlocksName = k_BuildingBlock + cameraName;

            GameObject gameObject = FindGameObjectInScene(buildingBlocksName);

            if (gameObject == null)
            {
                gameObject = FindGameObjectInScene(cameraName);
                if (gameObject == null)
                {
                    return null;
                }
            }
            return gameObject;
        }

        internal static void SafeMarkSceneDirty(GameObject gameObject)
        {
            if (gameObject == null) return;

            Scene scene = gameObject.scene;

            if (scene.IsValid() && !string.IsNullOrEmpty(scene.path) && !scene.path.Contains("Preview"))
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorApplication.delayCall += () =>
                {
                    if (scene.IsValid())
                    {
                        EditorSceneManager.SaveScene(scene);
                    }
                };
            }
        }
        #endregion

        #region Building Blocks
        internal static GameObject CheckAndCreatePrefabForBuildingBlocks(string sdkPackageName, string sdkVersion, string sdkPackageDisplayName, string sdkSamplesName, string prefabName, bool resetPose = false)
        {
            bool sampleImported = EnsureSampleImported(sdkPackageName, sdkVersion, sdkPackageDisplayName, sdkSamplesName);
            if (!sampleImported)
            {
                Debug.LogWarning($"Failed to import required sample, {prefabName} setup may be incomplete.");
            }

            string buildingBlocksName = $"{k_BuildingBlock} {prefabName}";
            GameObject gameObject = FindGameObjectInScene(buildingBlocksName);

            if (gameObject != null)
            {
                return gameObject;
            }

            gameObject = FindGameObjectInScene(prefabName);
            if (gameObject != null)
            {
                gameObject.name = buildingBlocksName;
                return gameObject;
            }

            string prefabPath = $"Assets/Samples/{sdkPackageDisplayName}/{sdkVersion}/{sdkSamplesName}/Prefabs/{prefabName}.prefab";

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogError($"Failed to load prefab asset from: {prefabPath}");
                return null;
            }

            gameObject = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (gameObject == null)
            {
                Debug.LogError($"Failed to instantiate prefab: {prefabPath}");
                return null;
            }

            Selection.activeGameObject = gameObject;
            Undo.RegisterCreatedObjectUndo(gameObject, "Create ViturePrefabPath.");

            if (resetPose)
            {
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;
            }
            gameObject.SetActive(true);
            gameObject.name = buildingBlocksName;

            SafeMarkSceneDirty(gameObject);
            return gameObject;
        }

        internal static void DisableNonVitureMainCameras()
        {
            var cameras = FindComponentsInScene<Camera>()
                .Where(cam => cam.gameObject.activeInHierarchy && cam.gameObject.CompareTag("MainCamera"))
                .ToList();

            foreach (Camera cam in cameras)
            {
                var origin = cam.GetComponentInParent<XROrigin>();
                if (origin == null)
                {
                    Undo.RecordObject(cam.gameObject, "Disable External Main Camera");
                    cam.gameObject.SetActive(false);
                }
            }
        }
        #endregion
    }
}