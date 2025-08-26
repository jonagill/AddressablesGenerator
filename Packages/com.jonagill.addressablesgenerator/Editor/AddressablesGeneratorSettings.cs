using UnityEditor;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SettingsManagement;
using UnityEngine;

namespace AddressablesGenerator
{
    public static class AddressablesGeneratorSettings
    {
        /// <summary>
        /// Custom UserSetting type that routes to our settings instance
        /// </summary>
        private class Setting<T> : UserSetting<T>
        {
            public Setting(string key, T value, SettingsScope scope = SettingsScope.Project)
                : base(Instance, key, value, scope)
            { }

            public Setting(Settings settings, string key, T value, SettingsScope scope = SettingsScope.Project)
                : base(settings, key, value, scope) { }
        }
        
        /// <summary>
        /// Registers our settings for display in the Project Settings GUI
        /// </summary>
        static class AddressablesSettingsProvider
        {
            private const string SettingsPath = "Project/Addressables";
            
            [SettingsProvider]
            static SettingsProvider CreateSettingsProvider()
            {
                var provider = new UserSettingsProvider(SettingsPath,
                    Instance,
                    new [] { typeof(AddressablesGeneratorSettings).Assembly },
                    SettingsScope.Project);

                return provider;
            }
        }
        
        private const string PackageName = "com.jonagill.addressablesgenerator";
        private const string CategoryName = "Addressables Generator";
        
        private static Settings _instance;

        private static Settings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Settings(PackageName);

                return _instance;
            }
        }

        
        [UserSetting(
            category: CategoryName, 
            title: "Run all registered generators during builds",
            tooltip: "If true, we will automatically run Addressable Group generators registered to AddressableAssetGroupGenerator automatically during builds. " +
                     "This should generally be safe, but you may wish to disable it if you want to rely on your generated groups being manually generated and checked into version control.")]
        private static Setting<bool> _runGeneratorsDuringBuild = new ($"{PackageName}.RunGeneratorsDuringBuilds", true);

        /// <summary>
        /// Whether to automatically run any registered Addressable Group generators during the build process
        /// </summary>
        public static bool RunGeneratorsDuringBuilds
        {
            get => _runGeneratorsDuringBuild.value;
            set => _runGeneratorsDuringBuild.SetValue(value, true);
        }
        
        [UserSetting(
            category: CategoryName,
            title: "Generate dependency bundles during builds", 
            tooltip: "If true, we will automatically generate optimized dependency groups during the build process to reduce duplicate asset usage. " +
                     "Note that this will can the asset bundle layout to change from build to build, and may not be appropriate for projects that require a stable bundle layout. " +
                     "(E.g games that ship additional bundle updates separate from the main client build.")]
        private static Setting<bool> _generateDependencyGroupsDuringBuilds = new ($"{PackageName}.GenerateDependencyGroupsDuringBuilds", false);

        /// <summary>
        /// Whether to automatically generate additional Addressable Groups to manage asset bundle dependencies during the build process
        /// </summary>
        public static bool GenerateDependencyGroupsDuringBuilds
        {
            get => _generateDependencyGroupsDuringBuilds.value;
            set => _generateDependencyGroupsDuringBuilds.SetValue(value, true);
        }
        
        [UserSetting(
            category: CategoryName,
            title: "Calculate dependencies for non-included groups",
            tooltip: "If true, we will consider Addressable Groups with 'Include in Build' set to false when calculating dependencies. " +
                     "This can help prevent issues with duplicate bundles being created when certain groups are included or excluded from builds dynamically. " +
                     "This may cause more dependency bundles to be created than is strictly necessary.")]
        private static Setting<bool> _calculateDependenciesForNonIncludedGroups = new ($"{PackageName}.CalculateDependenciesForNonIncludedGroups", true);

        /// <summary>
        /// Whether to consider Addressables Groups with Include in Build marked as false when calculating asset bundle dependencies
        /// </summary>
        public static bool CalculateDependenciesForNonIncludedGroups
        {
            get => _calculateDependenciesForNonIncludedGroups.value;
            set => _calculateDependenciesForNonIncludedGroups.SetValue(value, true);
        }

        [UserSetting(
            category: CategoryName,
            title: "Split groups into single bundles during builds",
            tooltip: "If true, we will split each group into multiple groups based on their Bundle Packing Mode setting. " +
                     "This will make it so there is one group per asset bundle, which allows for more granular dependency bundle creation.")]
        private static Setting<bool> _splitGroupsIntoSingleBundleGroupsDuringBuilds = new ($"{PackageName}.SplitGroupsIntoSingleBundleGroupsDuringBundles", false);
        
        /// <summary>
        /// Whether to automatically split Addressable Groups into generated groups that each output a single asset bundle during builds
        /// </summary>
        public static bool SplitGroupsIntoSingleBundleGroupsDuringBuilds
        {
            get => _splitGroupsIntoSingleBundleGroupsDuringBuilds.value;
            set => _splitGroupsIntoSingleBundleGroupsDuringBuilds.SetValue(value, true);
        }
        
        [UserSetting(
            category: CategoryName,
            title: "Generated Bundle Naming Mode",
            tooltip: "How to name bundles created by the bundle generation, bundle splitting, and dependency generation steps.")]
        private static Setting<BundledAssetGroupSchema.BundleNamingStyle> _generatedBundleNamingMode = new ($"{PackageName}.GeneratedBundleNamingMode", BundledAssetGroupSchema.BundleNamingStyle.NoHash);
        
        /// <summary>
        /// How to name bundles created by the bundle generation, bundle splitting, and dependency generation steps
        /// </summary>
        public static BundledAssetGroupSchema.BundleNamingStyle GeneratedBundleNamingMode
        {
            get => _generatedBundleNamingMode.value;
            set => _generatedBundleNamingMode.SetValue(value, true);
        }
    }
}
