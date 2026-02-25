using UnityEditor;
using UnityEngine;

namespace Viture.XR.Editor
{
    internal static class VitureEditorMenu
    {
        private const string k_MenuRoot = "VITURE/";
        private const string k_DocumentationItem = k_MenuRoot + "Developer Documentation";
        private const string k_SetupWizardItem = k_MenuRoot + "Setup Wizard";
        
        [MenuItem(k_DocumentationItem, false, 100)]
        internal static void OpenDocumentation()
        {
            const string url = "https://www.viture.com/developer/unity-sdk/unity#overview";
            Application.OpenURL(url);
        }

        [MenuItem(k_SetupWizardItem, false, 0)]
        internal static void OpenSetupWizard()
        {
            EditorWindow.GetWindow<VitureSetupWizard>("VITURE Setup Wizard");
        }

        [InitializeOnLoadMethod]
        private static void OnEditorLoad()
        {
            string projectKey = $"com.viture.xr.setup-wizard.{Application.dataPath.GetHashCode()}";
            
            if (EditorPrefs.GetInt(projectKey, 0) == 0)
            {
                EditorApplication.delayCall += () =>
                {
                    OpenSetupWizard();
                    EditorPrefs.SetInt(projectKey, 1);
                };
            }
        }
    }
}
