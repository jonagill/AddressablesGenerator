using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Customized version of the existing CheckBundleDupeDependencies rule that
    /// splits dependency assets into multiple groups based on the combinations of groups
    /// that depend on them. This improves Unity's ability to unload memory used by
    /// dependency assets since it's more likely that a given group will have all
    /// references to it removed when e.g. changing scenes.
    /// </summary>
    [InitializeOnLoad]
    public class GenerateDependencyBundles : CheckBundleDupeDependencies
    {
        private const string DEPENDENCY_BUNDLE_PREFIX = "Dependency Bundle";

        static GenerateDependencyBundles()
        {
            AnalyzeSystem.RegisterNewRule<GenerateDependencyBundles>();
        }

        public override string ruleName
        {
            get { return "Generate Dependency Bundles"; }
        }

        public override void FixIssues(AddressableAssetSettings settings)
        {
            // Clear any existing dependency bundles
            DeleteAllDependencyGroups( settings );
            
            HashSet<AddressableAssetGroup> groupsToDisable = new HashSet<AddressableAssetGroup>();

            if (AddressablesGeneratorSettings.CalculateDependenciesForNonIncludedGroups)
            {
                // Forcibly enable all of our groups so that they are
                // taken into account as we calculate dependencies
                foreach (var group in settings.groups)
                {
                    var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (bundledSchema != null && !bundledSchema.IncludeInBuild)
                    {
                        bundledSchema.IncludeInBuild = true;
                        groupsToDisable.Add(group);
                    }
                }
            }

            if (m_ImplicitAssets == null || m_ResultsData == null )
                CheckForDuplicateDependencies(settings);

            if (m_ImplicitAssets != null && m_ImplicitAssets.Count > 0)
            {
                AssetDatabase.StartAssetEditing();
                
                try
                {
                    int assetsProcessed = 0;
                    foreach (var assetGuid in m_ImplicitAssets)
                    {
                        var assetGuidString = assetGuid.ToString();
                        var assetPath = AssetDatabase.GUIDToAssetPath(assetGuidString);

                        if (EditorUtility.DisplayCancelableProgressBar(
                                "Moving duplicate dependencies...",
                                assetPath, assetsProcessed / (float)m_ImplicitAssets.Count))
                        {
                            break;
                        }

                        // Generate a group name to store this asset based on the groups that depend on it
                        // This way we can split up dependent assets and make it more likely that we will actually
                        // remove all references to that group's assets and unload the underlying bundle when
                        // e.g. changing scenes
                        var groupsThatDependOnAsset = GetGroupsThatDependOnAsset(assetGuid);
                        var groupHash = string.Join(",", groupsThatDependOnAsset).GetHashCode();
                        var groupName = $"{DEPENDENCY_BUNDLE_PREFIX} ({groupHash})";


                        // Get the group that we want to move this asset to
                        var group = settings.FindOrCreateGroup(groupName, readOnly: true);

                        // Mark this group as static content (just replicating the base functionality)
                        var updateGroupSchema = group.GetSchema<ContentUpdateGroupSchema>();
                        if (updateGroupSchema != null && !updateGroupSchema.StaticContent)
                        {
                            updateGroupSchema.StaticContent = true;
                        }

                        settings.CreateOrMoveEntry(assetGuid.ToString(), group, false, false);

                        assetsProcessed++;
                    }
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                    EditorUtility.ClearProgressBar();
                }
            }

            // Disable any groups that we forcibly enabled earlier
            foreach (var group in groupsToDisable)
            {
                var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                bundledSchema.IncludeInBuild = false;
            }

            settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
        }

        public static void DeleteAllDependencyGroups(AddressableAssetSettings settings)
        {
            for ( var i = settings.groups.Count - 1; i >= 0; i-- )
            {
                var group = settings.groups[i];
                if ( group.Name.StartsWith( DEPENDENCY_BUNDLE_PREFIX ) )
                {
                    settings.RemoveGroup( group );
                }
            }
        }

        private IEnumerable<string> GetGroupsThatDependOnAsset(GUID assetGuid)
        {
            var groups = new List<string>();
            foreach ( var result in m_ResultsData )
            {
                if ( result.DuplicatedGroupGuid == assetGuid )
                {
                    var groupName = result.Group.Name;
                    if ( !groups.Contains( groupName ) )
                    {
                        groups.Add( groupName );
                    }
                }
            }

            // Alphabetize for consistent results
            groups.Sort();

            return groups;
        }

        [MenuItem("Tools/Addressables Generator/Generate Dependency Groups", priority = 20000)]
        public static void GenerateDependencyGroups()
        {
            AssetDatabase.StartAssetEditing();
            
            // Delete any old dependency groups that may still exist (e.g. from a failed build)
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            DeleteAllDependencyGroups(settings);

            // Find all our assets that are dependencies of multiple Addressable groups
            // and put them into their own groups to prevent duplicating those assets
            var fixDependenciesRule = new GenerateDependencyBundles();
            fixDependenciesRule.FixIssues(settings);
            
            AssetDatabase.StopAssetEditing();
        }

        [MenuItem("Tools/Addressables Generator/Delete Dependency Groups", priority = 20001)]
        private static void DeleteAllDependencyGroups()
        {
            AssetDatabase.StartAssetEditing();

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            DeleteAllDependencyGroups( settings );

            AssetDatabase.StopAssetEditing();
        }
    }
}
