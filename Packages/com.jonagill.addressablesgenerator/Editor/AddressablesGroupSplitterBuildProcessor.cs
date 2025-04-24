using System;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.AddressablesGenerator;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace AddressablesGenerator
{
    /// <summary>
    /// Build processor for splitting  groups into individual bundles.
    /// This helps calculate more accurate dependency information 
    /// </summary>
    public class AddressablesGroupSplitterBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        public override int callbackOrder => (int) AddressablesGeneratorCallbackOrder.SplitGroups;

        private const string SplitGroupSeparator = "_Split_";
        
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.SplitGroupsIntoSingleBundleGroupsDuringBundles)
            {
                SplitGroupsIntoSingleBundleGroups();
            }
        }
        
                
        public void OnPostprocessBuild(BuildReport report)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.SplitGroupsIntoSingleBundleGroupsDuringBundles)
            {
                ClearSingleBundleGroups();
            }
        }

        public static bool SplitGroupsExistForGroup(AddressableAssetGroup group)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            return settings.groups.Any(g => g.name.StartsWith($"{group.name}{SplitGroupSeparator}"));
        }

        public static bool AssetIsInSplitGroupForGroup(UnityEngine.Object asset, AddressableAssetGroup group)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            
            var entry = settings.FindAssetEntry(asset);
            if (entry == null)
            {
                return false;
            }

            var entryGroup = entry.parentGroup;
            return TryGetOriginalGroupName(entryGroup.name, out var originalName) && originalName == group.name;
        }

        [MenuItem("Tools/Addressables Generator/Split Groups into Single Bundle Groups", priority = 10000)]
        private static void SplitGroupsIntoSingleBundleGroups()
        {
            AssetDatabase.StartAssetEditing();
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            
            try
            {
                var cachedGroups = settings.groups.ToArray();
                for (var i = 0; i < cachedGroups.Length; i++)
                {
                    var group = cachedGroups[i];
                    var bundledSchema = group.GetSchema<BundledAssetGroupSchema>();
                    if (bundledSchema == null)
                    {
                        continue;
                    }

                    if (EditorUtility.DisplayCancelableProgressBar($"Splitting Addressables groups into single bundle groups...", 
                            group.name, 
                            i / (float)cachedGroups.Length))
                    {
                        break;
                    }
                    
                    switch (bundledSchema.BundleMode)
                    {
                        case BundledAssetGroupSchema.BundlePackingMode.PackTogether:
                            // This group will already make one bundle -- nothing to do
                            break;
                        case BundledAssetGroupSchema.BundlePackingMode.PackSeparately:
                            SplitGroupByAsset(settings, group);
                            break;
                        case BundledAssetGroupSchema.BundlePackingMode.PackTogetherByLabel:
                            SplitGroupByLabel(settings, group);
                            break;
                        default:
                            throw new ArgumentException(nameof(bundledSchema.BundleMode));
                    }
                }
                
            }
            finally
            {
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                AssetDatabase.StopAssetEditing();
            }
        }

        [MenuItem("Tools/Addressables Generator/Clear Single Bundle Groups", priority = 10001)]
        private static void ClearSingleBundleGroups()
        {
            AssetDatabase.StartAssetEditing();
            var settings = AddressableAssetSettingsDefaultObject.Settings;

            try
            {
                var cachedGroups = settings.groups.ToArray();
                for (var i = 0; i < cachedGroups.Length; i++)
                {
                    var group = cachedGroups[i];
                    if (TryGetOriginalGroupName(group.name, out var originalGroupName))
                    {
                        if (EditorUtility.DisplayCancelableProgressBar($"Clearing single-bundle groups...", group.name, i / (float)cachedGroups.Length))
                        {
                            break;
                        }
                        
                        var originalGroup = settings.FindGroup(originalGroupName);
                        if (originalGroup != null)
                        {
                            var cachedEntries = group.entries.ToArray();
                            foreach (var entry in cachedEntries)
                            {
                                settings.MoveEntry(entry, originalGroup, readOnly: entry.ReadOnly, postEvent: false);
                            }

                            if (group.entries.Count == 0)
                            {
                                settings.RemoveGroup(group, postEvent: false);
                            }
                            else
                            {
                                Debug.LogError($"Failed to completely clear generated single bundle group {group.name}.");
                            }
                        }
                        else
                        {
                            Debug.LogError($"Failed to find original group {originalGroupName} when clearing generated single bundle group {group.name}.");
                        }
                    }
                }
                
            }
            finally
            {
                settings.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
                AssetDatabase.StopAssetEditing();
            }
        }

        private static void SplitGroupByAsset(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var cachedEntries = group.entries.ToArray();
            foreach (var entry in cachedEntries)
            {
                if (string.IsNullOrEmpty(entry.guid)) continue;
                
                var splitGroupName = GetGeneratedGroupName(group.name, entry.guid);
                var splitGroup = settings.FindOrCreateGroup(splitGroupName, readOnly: true, postEvent: false);
                settings.MoveEntry(entry, splitGroup, readOnly: entry.ReadOnly, postEvent: false);
            }
        }
        
        private static void SplitGroupByLabel(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var cachedEntries = group.entries.ToArray();
            foreach (var entry in cachedEntries)
            {
                var labels = string.Join(',', entry.labels);
                var labelHash = labels.GetHashCode().ToString(); // Hash our label collection for brevity and obfuscation
                var splitGroupName = GetGeneratedGroupName(group.name, labelHash);
                var splitGroup = settings.FindOrCreateGroup(splitGroupName, readOnly: true, postEvent: false);
                settings.MoveEntry(entry, splitGroup, readOnly: entry.ReadOnly, postEvent: false);
            }
        }

        private static string GetGeneratedGroupName(string groupName, string id)
        {
            return $"{groupName}{SplitGroupSeparator}{id}";
        }

        private static bool TryGetOriginalGroupName(string splitGroupName, out string originalName)
        {
            var lastIndexOfSplit = splitGroupName.LastIndexOf(SplitGroupSeparator, StringComparison.Ordinal);
            if (lastIndexOfSplit < 0)
            {
                originalName = default;
                return false;
            }
            
            originalName = splitGroupName.Substring(0, lastIndexOfSplit);
            return true;
        }
    }
}
