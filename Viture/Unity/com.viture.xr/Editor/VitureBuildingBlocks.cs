using System.Collections.Generic;
using System.Linq;
using Unity.XR.CoreUtils.Editor.BuildingBlocks;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
#if INCLUDE_UNITY_XRI
using UnityEngine.XR.Interaction.Toolkit.UI;
#endif

namespace Viture.XR.Editor
{
    [BuildingBlockItem(Priority = k_SectionPriority)]
    class VitureSection : IBuildingBlockSection
    {
        public const string k_SectionId = "VITURE";
        public string SectionId => k_SectionId;

        const string k_SectionIconPath = "Building/Block/Section/Icon/Path";
        public string SectionIconPath => k_SectionIconPath;
        const int k_SectionPriority = 1;

        readonly IBuildingBlock[] m_BBlocksElementIds = new IBuildingBlock[]
        {
            new VitureXROriginBuildingBlock(),
            new VitureQuickActionsBuildingBlock(),
            new VitureCanvasBuildingBlock(),
            new VitureMarkerTrackingBuildingBlock(),
        };

        public IEnumerable<IBuildingBlock> GetBuildingBlocks()
        {
            var elements = m_BBlocksElementIds.ToList();
            return elements;
        }
    }

    class VitureXROriginBuildingBlock : IBuildingBlock
    {
        const string k_Id = "XR Origin (Viture)";
        const string k_BuildingBlockPath = VitureEditorUtils.k_BuildingBlockPathO + k_Id;
        const string k_IconPath = "buildingblockIcon";
        const string k_Tooltip = "Creates the core XR Origin with camera rig and tracking for VITURE glasses.";
        const int k_SectionPriority = 1;

        public string Id => k_Id;
        public string IconPath => k_IconPath;
        public bool IsEnabled => true;
        public string Tooltip => k_Tooltip;

        static void DoInterestingStuff()
        {
#if !INCLUDE_UNITY_XR_HANDS
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XrHandPackageName);
#endif
#if !INCLUDE_UNITY_XRI
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XriPackageName);
#endif
            VitureEditorUtils.s_SDKVersion = VitureConstants.k_DefaultSdkVersion;
            GameObject cameraOrigin = VitureEditorUtils.CheckAndCreatePrefabForBuildingBlocks(
                VitureEditorUtils.k_SDKPackageName,
                VitureEditorUtils.s_SDKVersion,
                VitureEditorUtils.k_SDKPackageDisplayName,
                VitureEditorUtils.k_SDKSampleStarterAssets,
                k_Id,
                true);

            if (!VitureEditorUtils.IsVitureInputActionsConfigured())
            {
                VitureEditorUtils.ConfigureVitureInputActions();
                SettingsService.OpenProjectSettings("Project/Input System Package");
            }
        }

        public void ExecuteBuildingBlock() => DoInterestingStuff();

        public static void ExecuteBuildingBlockStatic()
        {
            DoInterestingStuff();
        }

        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => DoInterestingStuff();

        [MenuItem(VitureEditorUtils.k_BuildingBlockPathP + k_Id, false, k_SectionPriority)]
        public static void ExecuteMenuItemHierarchy(MenuCommand command) => DoInterestingStuff();
    }

    class VitureQuickActionsBuildingBlock : IBuildingBlock
    {
        const string k_Id = "Viture Quick Actions";
        const string k_BuildingBlockPath = VitureEditorUtils.k_BuildingBlockPathO + k_Id;
        const string k_IconPath = "buildingblockIcon";
        const string k_Tooltip = "Adds a look-up activated UI panel for quick system controls like recording and home.";
        const int k_SectionPriority = 3;

        public string Id => k_Id;
        public string IconPath => k_IconPath;
        public bool IsEnabled => true;
        public string Tooltip => k_Tooltip;

        static void DoInterestingStuff()
        {
#if !INCLUDE_UNITY_XR_HANDS
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XrHandPackageName);
#endif
#if !INCLUDE_UNITY_XRI
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XriPackageName);
#endif
            VitureEditorUtils.DisableNonVitureMainCameras();
            VitureEditorUtils.s_SDKVersion = VitureConstants.k_DefaultSdkVersion;
            GameObject cameraOrigin = VitureEditorUtils.CheckAndCreatePrefabForBuildingBlocks(
                VitureEditorUtils.k_SDKPackageName,
                VitureEditorUtils.s_SDKVersion,
                VitureEditorUtils.k_SDKPackageDisplayName,
                VitureEditorUtils.k_SDKSampleStarterAssets,
                VitureEditorUtils.k_VitureXROriginPrefabName,
                true);

            GameObject quickAction = VitureEditorUtils.CheckAndCreatePrefabForBuildingBlocks(
                VitureEditorUtils.k_SDKPackageName,
                VitureEditorUtils.s_SDKVersion,
                VitureEditorUtils.k_SDKPackageDisplayName,
                VitureEditorUtils.k_SDKSampleStarterAssets,
                VitureEditorUtils.k_VitureQuickActionsPrefabName);

            Camera mainCamera;
            if (cameraOrigin != null && quickAction != null)
            {
                mainCamera = cameraOrigin.GetComponentsInChildren<Camera>()
                    .FirstOrDefault(component => component.gameObject.CompareTag("MainCamera"));

                var canvas = quickAction.GetComponentsInChildren<Canvas>().ToList();
                foreach (Canvas c in canvas)
                {
#if INCLUDE_UNITY_XRI
                    TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster = c.GetComponent<TrackedDeviceGraphicRaycaster>();
                    if (trackedDeviceGraphicRaycaster == null)
                    {
                        trackedDeviceGraphicRaycaster = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(c.gameObject);
                    }
                    else
                    {
                        Undo.RecordObject(trackedDeviceGraphicRaycaster, "Enable XR Raycaster");
                        trackedDeviceGraphicRaycaster.enabled = true;
                    }
#endif
                    Undo.RecordObject(c, "Set Canvas World Camera");
                    c.worldCamera = mainCamera;
                }
            }
        }

        public void ExecuteBuildingBlock() => DoInterestingStuff();

        public static void ExecuteBuildingBlockStatic()
        {
            DoInterestingStuff();
        }

        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => DoInterestingStuff();

        [MenuItem(VitureEditorUtils.k_BuildingBlockPathP + k_Id, false, k_SectionPriority)]
        public static void ExecuteMenuItemHierarchy(MenuCommand command) => DoInterestingStuff();
    }

    class VitureCanvasBuildingBlock : IBuildingBlock
    {
        const string k_Id = "Canvas Interaction";
        const string k_BuildingBlockPath = VitureEditorUtils.k_BuildingBlockPathO + k_Id;
        const string k_IconPath = "buildingblockIcon";
        const string k_Tooltip = "Sets up a World Space Canvas with hand ray interaction support for UI elements.";

        const int k_SectionPriority = 4;

        private const float k_CanvasWorldSpaceScale = 0.001f;
        private static readonly Vector2 s_CanvasDimensionsInMeters = new Vector2(1.0f, 1.0f);

        public string Id => k_Id;
        public string IconPath => k_IconPath;
        public bool IsEnabled => true;
        public string Tooltip => k_Tooltip;

        static string s_CanvasName = $"{VitureEditorUtils.k_BuildingBlock} {k_Id}";

        static void DoInterestingStuff()
        {
#if !INCLUDE_UNITY_XR_HANDS
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XrHandPackageName);
#endif
#if !INCLUDE_UNITY_XRI
            VitureEditorUtils.InstallOrUpdatePackage(VitureEditorUtils.k_XriPackageName);
#endif
            VitureEditorUtils.DisableNonVitureMainCameras();
            VitureEditorUtils.s_SDKVersion = VitureConstants.k_DefaultSdkVersion;
            GameObject cameraOrigin = VitureEditorUtils.CheckAndCreatePrefabForBuildingBlocks(
                VitureEditorUtils.k_SDKPackageName,
                VitureEditorUtils.s_SDKVersion,
                VitureEditorUtils.k_SDKPackageDisplayName,
                VitureEditorUtils.k_SDKSampleStarterAssets,
                VitureEditorUtils.k_VitureXROriginPrefabName,
                true);

            List<Canvas> canvases = VitureEditorUtils.FindComponentsInScene<Canvas>().ToList();
            if (canvases.Count == 0)
            {
                EditorApplication.ExecuteMenuItem("GameObject/UI/Canvas");
                canvases = VitureEditorUtils.FindComponentsInScene<Canvas>();
                if (canvases.Count > 0)
                {
                    Undo.RegisterCreatedObjectUndo(canvases[0], "Create Canvas");
                }
            }

            if (canvases.Count == 0)
            {
                Debug.LogError("Failed to create Canvas");
                return;
            }

            Camera mainCamera = null;
            if (cameraOrigin != null)
            {
                mainCamera = cameraOrigin.GetComponentsInChildren<Camera>()
                    .FirstOrDefault(cam => cam.gameObject.CompareTag("MainCamera"));
            }

            foreach (Canvas canvas in canvases)
            {
                Undo.RecordObject(canvas, "Set Canvas World Camera");
#if INCLUDE_UNITY_XRI
                TrackedDeviceGraphicRaycaster trackedDeviceGraphicRaycaster = canvas.GetComponent<TrackedDeviceGraphicRaycaster>();
                if (trackedDeviceGraphicRaycaster == null)
                {
                    trackedDeviceGraphicRaycaster = Undo.AddComponent<TrackedDeviceGraphicRaycaster>(canvas.gameObject);
                }
                else
                {
                    Undo.RecordObject(trackedDeviceGraphicRaycaster, "Enable XR Raycaster");
                    trackedDeviceGraphicRaycaster.enabled = true;
                }
#endif
                canvas.worldCamera = mainCamera;

                if (canvas.renderMode != RenderMode.WorldSpace)
                {
                    Vector2 canvasSize = s_CanvasDimensionsInMeters / k_CanvasWorldSpaceScale;

                    RectTransform rectTransform = canvas.GetComponent<RectTransform>();
                    Undo.RecordObject(rectTransform, "Set Canvas Size");
                    rectTransform.sizeDelta = canvasSize;

                    canvas.renderMode = RenderMode.WorldSpace;
                    canvas.transform.localScale = Vector3.one * k_CanvasWorldSpaceScale;

                    if (mainCamera != null)
                    {
                        Undo.RecordObject(canvas.transform, "Position Canvas");
                        canvas.transform.position = mainCamera.transform.position + new Vector3(0, 0, 1);
                        canvas.transform.rotation = mainCamera.transform.rotation;
                    }
                }

                Undo.RecordObject(canvas, "Change Canvas Name");
                canvas.name = s_CanvasName;
            }

            foreach (EventSystem es in VitureEditorUtils.FindComponentsInScene<EventSystem>())
            {
                Undo.RecordObject(es.gameObject, "Disable Event System");
                es.gameObject.SetActive(false);
            }

            VitureEditorUtils.SafeMarkSceneDirty(cameraOrigin);
        }

        public void ExecuteBuildingBlock() => DoInterestingStuff();

        public static void ExecuteBuildingBlockStatic()
        {
            DoInterestingStuff();
        }

        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => DoInterestingStuff();

        [MenuItem(VitureEditorUtils.k_BuildingBlockPathP + k_Id, false, k_SectionPriority)]
        public static void ExecuteMenuItemHierarchy(MenuCommand command) => DoInterestingStuff();
    }

    class VitureMarkerTrackingBuildingBlock : IBuildingBlock
    {
        const string k_Id = "Marker Tracking";
        const string k_BuildingBlockPath = VitureEditorUtils.k_BuildingBlockPathO + k_Id;
        const string k_IconPath = "buildingblockIcon";
        const string k_Tooltip = "Enables image marker detection and tracking for AR experiences.";
        const int k_SectionPriority = 5;

        public string Id => k_Id;
        public string IconPath => k_IconPath;
        public bool IsEnabled => true;
        public string Tooltip => k_Tooltip;

        static void DoInterestingStuff()
        {
            VitureEditorUtils.s_SDKVersion = VitureConstants.k_DefaultSdkVersion;
            GameObject cameraOrigin = VitureEditorUtils.CheckAndCreatePrefabForBuildingBlocks(
                VitureEditorUtils.k_SDKPackageName,
                VitureEditorUtils.s_SDKVersion,
                VitureEditorUtils.k_SDKPackageDisplayName,
                VitureEditorUtils.k_SDKSampleMarkerTrackingDemo,
                VitureEditorUtils.k_VitureMarkerTrackingPrefabName,
                true);
        }

        public void ExecuteBuildingBlock() => DoInterestingStuff();

        public static void ExecuteBuildingBlockStatic()
        {
            DoInterestingStuff();
        }

        [MenuItem(k_BuildingBlockPath, false, k_SectionPriority)]
        public static void ExecuteMenuItem(MenuCommand command) => DoInterestingStuff();

        [MenuItem(VitureEditorUtils.k_BuildingBlockPathP + k_Id, false, k_SectionPriority)]
        public static void ExecuteMenuItemHierarchy(MenuCommand command) => DoInterestingStuff();
    }
}