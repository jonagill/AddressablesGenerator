using UnityEngine;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Store the per-project settings for Addressables Generator
    /// </summary>
    [FilePath("ProjectSettings/com.jonagill.addressablesgenerator/AddressablesGeneratorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public class AddressablesGeneratorProjectSettings : ScriptableSingleton<AddressablesGeneratorProjectSettings>
    {
        [SerializeField] public bool runGeneratorsDuringBuilds = true;
        [SerializeField] public bool generateDependencyGroupsDuringBuilds = false;
        [SerializeField] public bool calculateDependenciesForNonIncludedGroups = true;
        
        void OnDisable()
        {
            Save();
        }
        
        public void Save()
        {
            Save(true);
        }

        internal SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }
    
    public static class AddressablesGeneratorSettings
    {
        private class EditorContent
        {
            public const float ToggleLabelWidth = 300f;

            public static readonly GUIContent RunAllGeneratorsLabel = new GUIContent(
                "Run all registered generators during builds", 
                "If true, we will automatically run Addressable Group generators registered to AddressableAssetGroupGenerator automatically during builds. " +
                "This should generally be safe, but you may wish to disable it if you want to rely on your generated groups being manually generated and checked into version control.");
            
            public static readonly GUIContent GenerateDependencyGroupsLabel = new GUIContent(
                "Generate dependency bundles during builds", 
                "If true, we will automatically generate optimized dependency groups during the build process to reduce duplicate asset usage. " +
                "Note that this will can the asset bundle layout to change from build to build, and may not be appropriate for projects that require a stable bundle layout. " +
                "(E.g games that ship additional bundle updates separate from the main client build.");
            
            public static readonly GUIContent CalculateDependenciesForNonIncludedGroupsLabel = new GUIContent(
                "Calculate dependencies for non-included groups", 
                "If true, we will consider Addressable Groups with 'Include in Build' set to false when calculating dependencies. " +
                "This can help prevent issues with duplicate bundles being created when certain groups are included or excluded from builds dynamically. " +
                "This may cause more dependency bundles to be created than is strictly necessary.");
        }

        /// <summary>
        /// Whether to automatically run any registered Addressable Group generators during the build process
        /// </summary>
        public static bool RunGeneratorsDuringBuilds => AddressablesGeneratorProjectSettings.instance.runGeneratorsDuringBuilds;

        /// <summary>
        /// Whether to automatically generate additional Addressable Groups to manage asset bundle dependencies during the build process
        /// </summary>
        public static bool GenerateDependencyGroupsDuringBuilds => AddressablesGeneratorProjectSettings.instance.generateDependencyGroupsDuringBuilds;
        
        /// <summary>
        /// Whether to consider Addressables Groups with Include in Build marked as false when calculating asset bundle dependencies
        /// </summary>
        public static bool CalculateDependenciesForNonIncludedGroups => AddressablesGeneratorProjectSettings.instance.calculateDependenciesForNonIncludedGroups;

        private static SerializedObject settingsObject;
        private static SerializedProperty runGeneratorsDuringBuildsProp;
        private static SerializedProperty generateDependencyGroupsDuringBuildsProp;
        private static SerializedProperty calculateDependenciesForNonIncludedGroupsProp;
        
        [SettingsProvider]
        private static SettingsProvider CreateSettingsProvider()
        {
            var settingsProvider = new SettingsProvider(
                "Project/Addressables/Addressables Generator", 
                SettingsScope.Project,
                SettingsProvider.GetSearchKeywordsFromGUIContentProperties<EditorContent>())
            {
                activateHandler = (SearchContext, rootElement) =>
                {
                    OnActivate();
                },
                guiHandler = _ =>
                {
                    PreferencesGUI();
                }
            };
            return settingsProvider;
        }

        private static void OnActivate()
        {
            AddressablesGeneratorProjectSettings.instance.Save();
            settingsObject = AddressablesGeneratorProjectSettings.instance.GetSerializedObject();
            runGeneratorsDuringBuildsProp = settingsObject.FindProperty(nameof(AddressablesGeneratorProjectSettings.runGeneratorsDuringBuilds));
            generateDependencyGroupsDuringBuildsProp = settingsObject.FindProperty(nameof(AddressablesGeneratorProjectSettings.generateDependencyGroupsDuringBuilds));
            calculateDependenciesForNonIncludedGroupsProp = settingsObject.FindProperty(nameof(AddressablesGeneratorProjectSettings.calculateDependenciesForNonIncludedGroups));
        }

        private static void PreferencesGUI()
        {
            settingsObject.Update();

            using (var changeScope = new EditorGUI.ChangeCheckScope())
            {
                var prevWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = EditorContent.ToggleLabelWidth;
                EditorGUILayout.PropertyField(runGeneratorsDuringBuildsProp, EditorContent.RunAllGeneratorsLabel);
                EditorGUILayout.PropertyField(generateDependencyGroupsDuringBuildsProp, EditorContent.GenerateDependencyGroupsLabel);
                EditorGUILayout.PropertyField(calculateDependenciesForNonIncludedGroupsProp, EditorContent.CalculateDependenciesForNonIncludedGroupsLabel);
                EditorGUIUtility.labelWidth = prevWidth;
                
                if (changeScope.changed)
                {
                    settingsObject.ApplyModifiedProperties();
                    AddressablesGeneratorProjectSettings.instance.Save();
                }
            }
        }
    }
}
