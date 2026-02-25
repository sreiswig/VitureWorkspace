#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace Viture.XR.Editor
{
    internal class VitureSetupWizard : EditorWindow
    {
        private bool m_XRPluginConfigured;
        
        private bool m_PlatformConfigured;
        
        private bool m_GraphicsApiConfigured;
        
        private bool m_MinimumApiLevelConfigured;
        
        private bool m_InputActionsConfigured;

        private void OnGUI()
        {
            DrawHeader();
            DrawRequirements();
            DrawActions();
        }

        private void DrawHeader()
        {
            GUILayout.Space(10);
            EditorGUIUtility.labelWidth = 240;

            GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16
            };
            GUILayout.Label("Required Project Settings", titleStyle);
            GUILayout.Space(10);
        }

        private void DrawRequirements()
        {
            EditorGUI.BeginDisabledGroup(true);
            
            m_XRPluginConfigured = DrawStatusToggle("XR Plug-in: VITURE",
                VitureEditorUtils.IsViturePluginEnabled());
            
            m_PlatformConfigured = DrawStatusToggle("Platform: Android", 
                VitureEditorUtils.IsAndroidPlatform());
            
            m_GraphicsApiConfigured = DrawStatusToggle("Graphics API: OpenGLES3", 
                VitureEditorUtils.IsOpenGLES3Only());
            
            m_MinimumApiLevelConfigured = DrawStatusToggle($"Minimum API Level: {VitureEditorUtils.k_MinAndroidSdkVersionString}",
                VitureEditorUtils.IsAndroidMinApiLevelConfigured());
            
            m_InputActionsConfigured = DrawStatusToggle("Input Actions: VITURE",
                VitureEditorUtils.IsVitureInputActionsConfigured());
            
            EditorGUI.EndDisabledGroup();
        }

        private bool DrawStatusToggle(string label, bool status)
        {
            return EditorGUILayout.Toggle(label, status);
        }

        private void DrawActions()
        {
            bool allRequirementsMet = m_PlatformConfigured &&
                                      m_XRPluginConfigured &&
                                      m_GraphicsApiConfigured &&
                                      m_MinimumApiLevelConfigured &&
                                      m_InputActionsConfigured;
            
            GUILayout.Space(10);

            if (allRequirementsMet)
            {
                EditorGUILayout.HelpBox("All requirements are configured correctly!", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Some requirements are not met. Click 'Setup Project' to configure automatically.",
                    MessageType.Warning);
                GUILayout.Space(10);
                
                if (GUILayout.Button("Setup Project", GUILayout.Height(40)))
                {
                    SetupProject();
                }
            }
        }

        private static void SetupProject()
        {
            VitureSetupWizard window = GetWindow<VitureSetupWizard>();

            try
            {
                VitureEditorUtils.SwitchToAndroidPlatform();
                VitureEditorUtils.ConfigureViturePlugin();
                VitureEditorUtils.SetOpenGLES3Only();
                VitureEditorUtils.ConfigureAndroidMinApiLevel();
                VitureEditorUtils.ConfigureVitureInputActions();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Project Setup Complete",
                    "Your project has been configured for VITURE XR development!", "OK");
            }
            catch (Exception e)
            {
                EditorUtility.DisplayDialog("Setup Error",
                    $"An error occurred during project setup: {e.Message}", "OK");
                Debug.LogError($"VITURE XR Setup Error: {e.Message}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                window.Repaint();
            }
        }
    }
}
#endif
