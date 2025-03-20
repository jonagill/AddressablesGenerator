using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace AddressablesGenerator
{
    /// <summary>
    /// Extension methods for working with AddressableAssetSettings
    /// </summary>
    public static class AddressableAssetSettingsExtensions
    {
        public static void RemoveAllEmptyLabels(this AddressableAssetSettings settings)
        {
            var allLabels = settings.GetLabels();
            var allAssets = settings.GetAllAssetEntries();

            foreach (var label in allLabels)
            {
                if (!allAssets.Any(a => a.labels.Contains(label)))
                {
                    settings.RemoveLabel(label);
                }
            }
        }

        public static void RemoveAllEntriesFromGroup(this AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var entries = group.entries.ToArray();

            // Based on the logic in AddressableAssetSettings.RemoveAssetEntry
            foreach (var entry in entries)
            {
                group.RemoveAssetEntry(entry, false);
                if (entry != null)
                {
                    if (entry.parentGroup != null)
                        entry.parentGroup.RemoveAssetEntry(entry, true);
                    settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryRemoved, entry, true, false);
                }
            }
        }

        public static void RemoveEmptyGroups(this AddressableAssetSettings settings, System.Func<AddressableAssetGroup, bool> filterFunction = null)
        {
            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = settings.groups.Count - 1; i >= 0; i--)
                {
                    var group = settings.groups[i];
                    if (!group.IsDefaultGroup() &&
                        group.entries.Count == 0 &&
                        (filterFunction == null || filterFunction(group)))
                    {
                        settings.RemoveGroup(group);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        /// <summary>
        /// Get all the assets included in the Addressables build system
        /// Note that this includes sub-assets that may not be manually configured in the Addressables system,
        /// such as if a whole folder has been included in the Addressables system.
        /// </summary>
        public static IReadOnlyList<AddressableAssetEntry> GetAllAssets(this AddressableAssetSettings settings, bool includeSubObjects = false)
        {
            var list = new List<AddressableAssetEntry>();
            settings.GetAllAssets(list, includeSubObjects, null, null);
            return list;
        }

        /// <summary>
        /// Get all the asset entries that have been manually configured in the Addressables build system
        /// </summary>
        public static IReadOnlyList<AddressableAssetEntry> GetAllAssetEntries(this AddressableAssetSettings settings)
        {
            var list = new List<AddressableAssetEntry>();
            settings.GetAllAssetEntries(list);
            return list;
        }

        /// <summary>
        /// Get all the asset entries that have been manually configured in the Addressables build system
        /// </summary>
        public static void GetAllAssetEntries(this AddressableAssetSettings settings, List<AddressableAssetEntry> list)
        {
            foreach (var group in settings.groups)
            {
                list.AddRange(group.entries);
            }
        }

        /// <summary>
        /// Get all the assets included in the given Addressable group
        /// Note that this includes sub-assets that may not be manually configured in the group,
        /// such as if a whole folder has been included in the group.
        /// </summary>
        public static IReadOnlyList<AddressableAssetEntry> GetAllAssetsInGroup(this AddressableAssetSettings settings, AddressableAssetGroup group, bool includeSubObjects = false)
        {
            var list = new List<AddressableAssetEntry>();
            settings.GetAllAssets(list, includeSubObjects,g => g == group, null);
            return list;
        }

        public static AddressableAssetEntry CreateOrMoveEntry(
            this AddressableAssetSettings settings,
            UnityEngine.Object asset,
            AddressableAssetGroup group,
            bool readOnly = true,
            bool postEvent = true)
        {
            if (asset == null)
            {
                return null;
            }

            return CreateOrMoveEntry(settings, asset.name, asset, group, readOnly, postEvent);
        }

        public static AddressableAssetEntry FindAssetEntry(
            this AddressableAssetSettings settings,
            Object asset)
        {
            if (asset == null)
            {
                Debug.LogError($"Cannot retrieve null assets from the Addressables catalog.");
                return null;
            }

            if (asset is Component)
            {
                // Always add GameObjects for prefabs, not components
                asset = ((Component)asset).gameObject;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);

            return settings.FindAssetEntry(guid);
        }

        public static AddressableAssetEntry CreateOrMoveEntry(
            this AddressableAssetSettings settings,
            string name,
            UnityEngine.Object asset,
            AddressableAssetGroup group,
            bool readOnly = true,
            bool postEvent = true)
        {
            if (asset == null)
            {
                Debug.LogError($"Cannot add null assets to the Addressables catalog.");
                return null;
            }

            if (asset is Component)
            {
                // Always add GameObjects for prefabs, not components
                asset = ((Component) asset).gameObject;
            }

            if (!EditorUtility.IsPersistent(asset))
            {
                Debug.LogError($"Cannot add non-persistent object ({asset}) to the Addressables catalog.", asset);
                return null;
            }

            if (!AssetDatabase.IsMainAsset(asset))
            {
                Debug.LogError($"Cannot add sub-asset ({asset}) to the Addressables catalog.", asset);
                return null;
            }

            var path = AssetDatabase.GetAssetPath(asset);
            var guid = AssetDatabase.AssetPathToGUID(path);

            var assetEntry = settings.FindAssetEntry(guid);
            if (assetEntry == null || assetEntry.parentGroup != group)
            {
                assetEntry = settings.CreateOrMoveEntry(guid, group, readOnly: readOnly, postEvent: postEvent);
                assetEntry.address = name;
            }

            return assetEntry;
        }
        
        public static AddressableAssetGroup FindOrCreateGroup(
            this AddressableAssetSettings settings,
            string name,
            bool readOnly = true)
        {
            AddressableAssetGroup group = settings.FindGroup(name);
            if (group != null)
            {
                return group;
            }

            var template =
                (AddressableAssetGroupTemplate) settings.GroupTemplateObjects.First(t => t.name == "Packed Assets");
            return settings.CreateGroup(name, false, readOnly, false, null, template.GetTypes());
        }
    }
}
