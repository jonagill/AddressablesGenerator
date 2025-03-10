using UnityEngine;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    public static class AddressablesGeneratorSettings
    {
        private class EditorContent
        {
            public const float ToggleLabelWidth = 300f;
            public static readonly GUIContent GenerateDependencyGroupsLabel = new GUIContent(
                "Generate dependency bundles during builds", 
                "If true, we will automatically generate optimized dependency groups during the build process to reduce duplicate asset usage. " +
                "Note that this will can the asset bundle layout to change from build to build, and may not be appropriate for projects that require a stable bundle layout. " +
                "(E.g games that ship additional bundle updates separate from the main client build.");
        }
        
        private const string GenerateDependencyGroupsKey = "AddressablesGeneratorSettings_GenerateDependencyGroupsKey";
        
        /// <summary>
        /// Whether to automatically generate additional Addressable Groups to manage asset bundle dependencies during the build process
        /// </summary>
        public static bool GenerateDependencyGroups
        {
            get => EditorPrefs.GetBool(GenerateDependencyGroupsKey, false);
            set => EditorPrefs.SetBool(GenerateDependencyGroupsKey, value);
        }
        
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            var settingsProvider = new SettingsProvider(
                "Project/Addressables/Addressables Generator", 
                SettingsScope.Project, 
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<EditorContent>()) {
                guiHandler = _ =>
                {
                    PreferencesGUI();
                }
            };
            return settingsProvider;
        }

        private static void PreferencesGUI()
        {
            var prevWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorContent.ToggleLabelWidth;
            GenerateDependencyGroups = EditorGUILayout.Toggle(EditorContent.GenerateDependencyGroupsLabel, GenerateDependencyGroups, GUILayout.ExpandWidth(true));
            EditorGUIUtility.labelWidth = prevWidth;
        }
    }
}
