using UnityEditor;
using UnityEngine;

namespace Viture.XR.Editor
{
    [CustomEditor(typeof(VitureSettings))]
    public class VitureSettingsEditor : UnityEditor.Editor
    {
        private bool m_PermissionsFoldout = true;

        private const string k_SupportedGlassesTooltip =
            "Defines which VITURE glasses your app supports. " +
            "The system checks this to verify compatibility with the connected glasses.";

        private const string k_ActivateHandTrackingTooltip =
            "Automatically activate hand tracking when the application starts. " +
            "Disable this if you want to control hand tracking manually via code.";

        private const string k_HandFilterModeTooltip =
            "Balances hand tracking responsiveness vs stability. Responsive has lower latency but more jitter. " +
            "Stable is smoother but slightly delayed. Can be changed at runtime via VitureXR.HandTracking.FilterMode.";

        private const string k_PermissionsTooltip = "Select the Android permissions your app needs.";

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUIUtility.labelWidth = 240;

            var appGlassesSupportProp = serializedObject.FindProperty("m_AppGlassesSupport");
            EditorGUILayout.PropertyField(appGlassesSupportProp, new GUIContent("Supported Glasses", k_SupportedGlassesTooltip));
            EditorGUILayout.Space(5);

            var initHandTrackingProp = serializedObject.FindProperty("m_ActivateHandTrackingOnStartup");
            EditorGUILayout.PropertyField(initHandTrackingProp,
                new GUIContent("Activate Hand Tracking on Startup", k_ActivateHandTrackingTooltip));
            EditorGUILayout.Space(5);
            
            var handFilterModeProp = serializedObject.FindProperty("m_HandFilterMode");
            EditorGUI.BeginChangeCheck();
            var displayedOptionsStrings = new[] { "Responsive", "Stable" };
            var displayedOptions = System.Array.ConvertAll(displayedOptionsStrings, s => new GUIContent(s));
            var optionValues = new[]
            {
                (int)VitureHandFilterMode.Responsive,
                (int)VitureHandFilterMode.Stable
            };
            var labelContent = new GUIContent("Hand Filter Mode", k_HandFilterModeTooltip);
            var newValue = EditorGUILayout.IntPopup(
                labelContent,
                handFilterModeProp.intValue,
                displayedOptions,
                optionValues);
            if (EditorGUI.EndChangeCheck())
            {
                handFilterModeProp.intValue = newValue;
                serializedObject.ApplyModifiedProperties();
            }
            EditorGUILayout.Space(5);

            var permissionFoldoutContent = new GUIContent("Android Permissions", k_PermissionsTooltip);
            m_PermissionsFoldout = EditorGUILayout.Foldout(m_PermissionsFoldout, permissionFoldoutContent,
                EditorStyles.foldoutHeader);
            if (m_PermissionsFoldout)
            {
                EditorGUI.indentLevel++;

                DrawPermission("m_CameraPermission", "Camera", "android.permission.CAMERA");

                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void DrawPermission(string propertyName, string displayName, string androidPermission)
        {
            var prop = serializedObject.FindProperty(propertyName);
            var content = new GUIContent(displayName, androidPermission);
            EditorGUILayout.PropertyField(prop, content);
        }
    }
}