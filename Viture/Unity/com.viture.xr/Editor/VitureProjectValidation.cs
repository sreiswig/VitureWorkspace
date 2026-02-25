using Unity.XR.CoreUtils.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Rendering;
using UnityEditor.XR.Management;
using UnityEngine;

namespace Viture.XR.Editor
{
    internal static class VitureProjectValidation
    {
        private const string k_Category = "VITURE";

        [InitializeOnLoadMethod]
        private static void RegisterValidationRules()
        {
            var androidValidationRules = new[]
            {
#region Required
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "VITURE XR plugin must be enabled and set as the only active XR plugin.",
                    CheckPredicate = () =>
                    {
                        var generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Android);
                        if (!generalSettings)
                            return false;

                        var activeLoaders = generalSettings.Manager.activeLoaders;
                        return activeLoaders.Count == 1 && VitureEditorUtils.IsViturePluginEnabled();
                    },
                    FixItMessage = "Open Project Settings > XR Plug-in Management > enable 'VITURE'.",
                    FixIt = VitureEditorUtils.ConfigureViturePlugin,
                    Error = true
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Build target platform must be set to Android.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = VitureEditorUtils.IsAndroidPlatform,
#if UNITY_6000_0_OR_NEWER
                    FixItMessage = "Open Build Profiles > Platforms, switch to 'Android'.",
#else
                    FixItMessage = "Open Project Settings > Platform, switch to 'Android'.",
#endif
                    FixIt = VitureEditorUtils.SwitchToAndroidPlatform,
                    Error = true
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Graphics API must be set to OpenGLES3 only.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = VitureEditorUtils.IsOpenGLES3Only,
                    FixItMessage = "Open Project Settings > Player > Other Settings > Graphics APIs, and set to OpenGLES3 only.",
                    FixIt = VitureEditorUtils.SetOpenGLES3Only,
                    Error = true
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = $"Minimum Android API Level must be set to {VitureEditorUtils.k_MinAndroidSdkVersionString}.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = VitureEditorUtils.IsAndroidMinApiLevelConfigured,
                    FixItMessage = $"Open Project Settings > Player > Other Settings, and set Minimum API Level to {VitureEditorUtils.k_MinAndroidSdkVersionString}.",
                    FixIt = VitureEditorUtils.ConfigureAndroidMinApiLevel,
                    Error = true
                },
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Viture Input Actions must be set as Project-wide Actions for head and hand input.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = VitureEditorUtils.IsVitureInputActionsConfigured,
                    FixItMessage = "Open Project Settings > Input System Package, and set Project-wide Actions to 'Viture Input Actions'.",
                    FixIt = VitureEditorUtils.ConfigureVitureInputActions,
                    Error = true
                },
#endregion Required
                
#region Recommended 
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "IL2CPP scripting backend with ARM64 architecture is recommended for optimal XR performance.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () =>
                    {
                        var scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
                        var targetArchitectures = PlayerSettings.Android.targetArchitectures;

                        return scriptingBackend == ScriptingImplementation.IL2CPP &&
                               (targetArchitectures & AndroidArchitecture.ARM64) != AndroidArchitecture.None;
                    },
                    FixItMessage = "Open Project Settings > Player > Other Settings, set Scripting Backend to IL2CPP and enable ARM64 under Target Architectures.",
                    FixIt = () =>
                    {
                        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
                        PlayerSettings.Android.targetArchitectures |= AndroidArchitecture.ARM64;
                    },
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "GameActivity is recommended instead of the legacy Activity for better performance.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => PlayerSettings.Android.applicationEntry == AndroidApplicationEntry.GameActivity,
                    FixItMessage = "Open Project Settings > Player > Other Settings, and set Application Entry Point to GameActivity.",
                    FixIt = () => PlayerSettings.Android.applicationEntry = AndroidApplicationEntry.GameActivity,
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Multithreaded Rendering is recommended for better XR performance.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => PlayerSettings.GetMobileMTRendering(NamedBuildTarget.Android),
                    FixItMessage = "Open Project Settings > Player > Other Settings, and enable Multithreaded Rendering.",
                    FixIt = () => PlayerSettings.SetMobileMTRendering(NamedBuildTarget.Android, true),
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Graphics Jobs should be disabled to avoid potential XR performance issues.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => !PlayerSettings.graphicsJobs,
                    FixItMessage = "Open Project Settings > Player > Other Settings, and disable Graphics Jobs.",
                    FixIt = () => PlayerSettings.graphicsJobs = false,
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Forward rendering path is recommended for optimal XR performance.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () =>
                    {
                        var tierSettings = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, Graphics.activeTier);
                        return tierSettings.renderingPath == RenderingPath.Forward;
                    },
                    FixItMessage = "Open Project Settings > Graphics, and set Rendering Path to Forward.",
                    FixIt = () =>
                    {
                        var renderingTier = EditorGraphicsSettings.GetTierSettings(BuildTargetGroup.Android, Graphics.activeTier);
                        renderingTier.renderingPath = RenderingPath.Forward;
                        EditorGraphicsSettings.SetTierSettings(BuildTargetGroup.Android, Graphics.activeTier, renderingTier);
                    },
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Realtime Global Illumination should be disabled to improve XR performance and reduce latency.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => !Lightmapping.realtimeGI,
                    FixItMessage = "Open Window > Rendering > Lighting > Scene, then under Realtime Lighting, disable Realtime Global Illumination.",
                    FixIt = () => Lightmapping.realtimeGI = false,
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Anisotropic Texture filtering is recommended for sharper texture details at oblique angles.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => QualitySettings.anisotropicFiltering == AnisotropicFiltering.Enable,
                    FixItMessage = "Open Project Settings > Quality, and set Anisotropic Texture to Per Texture.",
                    FixIt = () => QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable,
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "GPU Skinning is recommended to offload character animation calculations to the GPU, improving XR performance.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => PlayerSettings.gpuSkinning,
                    FixItMessage = "Open Project Settings > Player > Other Settings, and enable GPU Skinning.",
                    FixIt = () => PlayerSettings.gpuSkinning = true,
                    Error = false
                },
                
                new BuildValidationRule
                {
                    Category = k_Category,
                    Message = "Target API Level should be set to Automatic for better compatibility and future-proofing.",
                    IsRuleEnabled = VitureEditorUtils.IsViturePluginEnabled,
                    CheckPredicate = () => PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevelAuto,
                    FixItMessage = "Open Project Settings > Player > Other Settings, and set Target API Level to Automatic.",
                    FixIt = () => PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto,
                    Error = false
                },
#endregion Recommended
            };

            BuildValidator.AddRules(BuildTargetGroup.Android, androidValidationRules);
        }
    }
}
