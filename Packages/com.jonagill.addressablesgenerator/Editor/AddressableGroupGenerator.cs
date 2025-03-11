using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Object = System.Object;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Editor class that handles auto-generating addressable asset groups whenever a configuration file changes
    /// </summary>
    public class AddressableGroupGenerator : AssetPostprocessor, IPreprocessBuildWithReport
    {
        // Run before AddressablesGeneratorBuildProcessor (callbackOrder 0) and
        // AddressablesPlayerBuildProcessor (callbackOrder 1)
        public const int GenerateGroupsCallbackOrder = -1;
        
        private const string GENERATED_GROUP_SUFFIX = " (Generated)";

        [System.Serializable]
        public struct AssetEntryRequest
        {
            public UnityEngine.Object asset;
            public string label;
        }

#region Static API

        public static void AddEntries(
            string groupName,
            IReadOnlyList<UnityEngine.Object> assets,
            BundledAssetGroupSchema.BundlePackingMode packingMode,
            bool clearGroup = false,
            bool makeReadOnly = true
       )
        {
            var requests = assets.Select(a => new AssetEntryRequest() {asset = a, label = a.name}).ToArray();
            AddEntries(
                groupName,
                requests,
                packingMode,
                clearGroup,
                makeReadOnly);
        }

        public static void AddEntries(
            string groupName,
            IReadOnlyList<AssetEntryRequest> entryRequests,
            BundledAssetGroupSchema.BundlePackingMode packingMode,
            bool clearGroup = false,
            bool makeReadOnly = true)
        {
            // Close the addressables window, which can cause freezes if we add entries while it is open
            var addressablesWindow = EditorWindow.GetWindow(typeof (GUI.AddressableAssetsWindow));
            if (addressablesWindow != null)
            {
                addressablesWindow.Close();
            }

            // Sort all our entry requests for consistent ordering during serialization
            entryRequests = entryRequests
                .OrderBy(er => er.asset != null ? er.asset.name : string.Empty)
                .ToArray();

            var settings = AddressableAssetSettingsDefaultObject.Settings;

            List<AssetEntryRequest> filesWithInvalidCharactersInName = new List<AssetEntryRequest>();
            List<AssetEntryRequest> filesInResources = new List<AssetEntryRequest>();
            HashSet<string> resourceFolders = new HashSet<string>();

            AddressableAssetGroup group = ConfigureAddressableGroup(settings, groupName, packingMode, clearGroup, makeReadOnly);

            ValidateAssetRequests(
                groupName,
                entryRequests,
                filesWithInvalidCharactersInName,
                filesInResources,
                out bool isCanceled);

            if (isCanceled)
            {
                return;
            }

            RenameInvalidAssets(filesWithInvalidCharactersInName);
            MoveAssetsInResources(filesInResources, resourceFolders);
            DeleteEmptyResourceFolders(resourceFolders);

            AddAssetsToGroup(settings, entryRequests, group, makeReadOnly, out isCanceled);
            if (isCanceled)
            {
                return;
            }

            if (BuildPipeline.isBuildingPlayer)
            {
                // If we're about to build the player, process changes immediately
                PostUpdateCleanup();
            }
            else
            {
                // Otherwise, wait until the next update to batch together multiple changes into one cleanup pass
                EditorApplication.delayCall -= PostUpdateCleanup;
                EditorApplication.delayCall += PostUpdateCleanup;
            }
        }

        private static AddressableAssetGroup ConfigureAddressableGroup(
            AddressableAssetSettings settings,
            string groupName,
            BundledAssetGroupSchema.BundlePackingMode packingMode,
            bool clearGroup,
            bool makeReadOnly)
        {
            // Tag all our generated groups with the same suffix so we can find them programmatically
            var group = settings.FindOrCreateGroup(groupName + GENERATED_GROUP_SUFFIX, readOnly: makeReadOnly);

            if (makeReadOnly && !group.ReadOnly)
            {
                // Use ReadOnly as a flag for whether this group is automatically or manually configured
                // Never stomp manually configured groups
                throw new InvalidOperationException($"Attempting to automatically configure non-readonly Addressables group {groupName}. This is not supported.");
            }

            if (clearGroup)
            {
                settings.RemoveAllEntriesFromGroup(group);
            }

            var bundledSchema = group.GetSchema(typeof(BundledAssetGroupSchema)) as BundledAssetGroupSchema;
            if (bundledSchema != null)
            {
                bundledSchema.BundleMode = packingMode;

                if (bundledSchema.LoadPath.GetName(settings) == AddressableAssetSettings.kLocalLoadPath)
                {
                    // It does not make sense to use the asset bundle cache for local bundles
                    // Disable it here, as it can cause issues when attempting to load bundles
                    bundledSchema.UseAssetBundleCache = false;
                    bundledSchema.UseAssetBundleCrc = false;
                }
            }

            return group;
        }

        private static void ValidateAssetRequests(
            string groupName,
            IReadOnlyList<AssetEntryRequest> entryRequests,
            List<AssetEntryRequest> filesWithInvalidCharactersInName,
            List<AssetEntryRequest> filesInResources,
            out bool isCanceled)
        {
            isCanceled = false;
            for (int i = 0; i < entryRequests.Count; i++)
            {
                var entryRequest = entryRequests[i];
                if (entryRequest.asset == null)
                {
                    EditorUtility.DisplayDialog(
                        "Null addressable asset detected!",
                        $"Cannot add null assets to Addressable group {groupName}. (Label: { entryRequest.label }). Please validate your data and try again.",
                        "Okay");

                    isCanceled = true;
                    break;
                }

                if (EditorUtility.DisplayCancelableProgressBar("Validating Addressable asset requests...",
                        entryRequests[i].asset.ToString(), i / (float)entryRequests.Count))
                {
                    EditorUtility.ClearProgressBar();
                    isCanceled = true;
                    break;
                }

                var path = AssetDatabase.GetAssetPath(entryRequest.asset);
                var fileName = Path.GetFileNameWithoutExtension(path);
                if (fileName.Contains('[') || fileName.Contains(']'))
                {
                    if (filesWithInvalidCharactersInName.Count == 0)
                    {
                        // Warn the first time we find an invalid filename
                        if (!EditorUtility.DisplayDialog(
                                "Invalid asset name detected!",
                                $"Invalid characters found in Addressable asset request: {entryRequest.asset.name}\n\n" +
                                "Addressable assets can only have URI-safe characters in their filenames.\n" +
                                "Automatically rename assets with invalid names?",
                                "Okay",
                                "Cancel"))
                        {
                            isCanceled = true;
                            break;
                        }
                    }

                    filesWithInvalidCharactersInName.Add(entryRequest);
                }

                if (path.Split('/').Contains("Resources"))
                {
                    if (filesInResources.Count == 0)
                    {
                        if (!EditorUtility.DisplayDialog(
                                "Addressable assets cannot be in Resources!",
                                "Addressable assets will not load correctly from Resources folders.\n" +
                                "Automatically move assets out of Resources?",
                                "Yes, move",
                                "Cancel"))
                        {
                            isCanceled = true;
                            break;
                        }
                    }

                    filesInResources.Add(entryRequest);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static void RenameInvalidAssets(
            List<AssetEntryRequest> filesWithInvalidCharactersInName)
        {
            if (filesWithInvalidCharactersInName.Count == 0)
            {
                return;
            }

            AssetDatabase.StartAssetEditing();

            foreach (var entryRequest in filesWithInvalidCharactersInName)
            {
                var asset = entryRequest.asset;
                var path = AssetDatabase.GetAssetPath(asset);

                var fileName = Path.GetFileNameWithoutExtension(path);
                var safeFileName = fileName.Replace('[', '(').Replace(']', ')');
                if (fileName != safeFileName)
                {
                    var newPath = path.Replace(fileName, safeFileName);
                    AssetDatabase.MoveAsset(path, newPath);
                }
            }

            // Stop editing assets so that we get up to data path information for the modified assets
            AssetDatabase.StopAssetEditing();
        }

        private static void MoveAssetsInResources(
            List<AssetEntryRequest> filesInResources,
            HashSet<string> resourceFolders)
        {
            if (filesInResources.Count == 0)
            {
                return;
            }

            AssetDatabase.StartAssetEditing();

            foreach (var entryRequest in filesInResources)
            {
                var asset = entryRequest.asset;
                var path = AssetDatabase.GetAssetPath(asset);

                var newPath = path.Replace("Resources/", "");
                AssetDatabase.MoveAsset(path, newPath);

                // Track what folders we moved stuff out of so we can delete them later
                var resourcePath = path.Replace("/" + Path.GetFileName(path), "");
                resourceFolders.Add(resourcePath);
            }

            // Stop editing assets so that we get up to data path information for the modified assets
            AssetDatabase.StopAssetEditing();
        }

        private static void DeleteEmptyResourceFolders(HashSet<string> resourceFolders)
        {
            if (resourceFolders.Count == 0)
            {
                return;
            }

            AssetDatabase.StartAssetEditing();

            foreach (var folderPath in resourceFolders)
            {
                if (AssetDatabase.IsValidFolder(folderPath))
                {
                    DirectoryInfo di = new DirectoryInfo(folderPath);
                    if (di.GetFiles().Length == 0 && di.GetDirectories().Length == 0)
                    {
                        AssetDatabase.DeleteAsset(folderPath);
                    }
                }
            }

            AssetDatabase.StopAssetEditing();
        }

        private static void AddAssetsToGroup(
            AddressableAssetSettings settings,
            IReadOnlyList<AssetEntryRequest> entryRequests,
            AddressableAssetGroup group,
            bool makeReadOnly,
            out bool isCanceled)
        {
            isCanceled = false;
            AssetDatabase.StartAssetEditing();

            for (int i = 0; i < entryRequests.Count; i++)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Processing Addressable asset requests...",
                        entryRequests[i].asset.ToString(), i / (float)entryRequests.Count))
                {
                    EditorUtility.ClearProgressBar();
                    isCanceled = true;
                    break;
                }

                var entryRequest = entryRequests[i];

                var entry = settings.CreateOrMoveEntry(entryRequest.asset, group, readOnly: makeReadOnly, postEvent: false);
                if (entry != null)
                {
                    entry.SetLabel(
                        entryRequest.label,
                        enable: !string.IsNullOrEmpty(entryRequest.label),
                        force: true,
                        postEvent: false);
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.StopAssetEditing();
        }

        private static void PostUpdateCleanup()
        {
            // Remove empty labels and groups
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            settings.RemoveAllEmptyLabels();
            settings.RemoveEmptyGroups(GENERATED_GROUP_SUFFIX);

            AssetDatabase.SaveAssets();
        }

#endregion

#region AssetModificationProcessor

        private struct GroupGenerator
        {
            public Func<UnityEngine.Object, string> groupNameGenerator;
            public Func<UnityEngine.Object, IReadOnlyList<AssetEntryRequest>> requestGenerator;
            public BundledAssetGroupSchema.BundlePackingMode packingMode;
            public bool clearGroup;
            public bool makeReadOnly;

            public Action<UnityEngine.Object> onAddressablesGeneratedForAsset;
        }

        private static readonly Dictionary<Type, GroupGenerator> PerTypeGenerators = new();
        private static readonly List<string> PathsToProcess = new();
        private static readonly List<string> PathsJustProcessed = new();


        public static void RegisterGeneratorForType<T>(
            string groupName,
            Func<T, IReadOnlyList<AssetEntryRequest>> requestGenerator,
            BundledAssetGroupSchema.BundlePackingMode packingMode,
            bool clearGroup = false,
            bool makeReadOnly = true,
            Action<UnityEngine.Object> onAddressablesGeneratedForAsset = null
       ) where T : UnityEngine.Object
        {
            RegisterGeneratorForType(
                t => groupName,
                requestGenerator,
                packingMode,
                clearGroup,
                makeReadOnly,
                onAddressablesGeneratedForAsset);
        }

        public static void RegisterGeneratorForType<T>(
            Func<T, string> groupNameGenerator,
            Func<T, IReadOnlyList<AssetEntryRequest>> requestGenerator,
            BundledAssetGroupSchema.BundlePackingMode packingMode,
            bool clearGroup = false,
            bool makeReadOnly = true,
            Action<UnityEngine.Object> onAddressablesGeneratedForAsset = null
       ) where T : UnityEngine.Object
        {
            var type = typeof(T);
            if (PerTypeGenerators.ContainsKey(type))
            {
                Debug.LogError($"Addressable group generator already registered for type {type.Name}");
                return;
            }

            string UntypedGroupNameGenerator(object o) => groupNameGenerator((T) o);
            IReadOnlyList<AssetEntryRequest> UntypedRequestGenerator(object o) => requestGenerator((T) o);

            PerTypeGenerators[type] = new GroupGenerator()
            {
                groupNameGenerator = UntypedGroupNameGenerator,
                requestGenerator = (Func<Object, IReadOnlyList<AssetEntryRequest>>) UntypedRequestGenerator,
                packingMode = packingMode,
                clearGroup = clearGroup,
                makeReadOnly = makeReadOnly,
                onAddressablesGeneratedForAsset = onAddressablesGeneratedForAsset
            };
        }

        public static void ProcessGeneratorsForPath(string assetPath)
        {
            var paths = new string[1];
            paths[0] = assetPath;
            ProcessGeneratorsForPaths(paths);
        }

        public static void ProcessGeneratorsForPaths(IReadOnlyList<string> assetPaths)
        {
            for (var i = 0; i < assetPaths.Count; i++)
            {
                var assetPath = assetPaths[i];
                if (EditorUtility.DisplayCancelableProgressBar(
                        "Running Addressables group generators...",
                        assetPath,
                        i / (float)assetPaths.Count))
                {
                    break;
                }

                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (PerTypeGenerators.TryGetValue(type, out var generator))
                {
                    var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    var groupName = generator.groupNameGenerator.Invoke(asset);
                    var entryRequests = generator.requestGenerator.Invoke(asset);

                    AddEntries(
                        groupName,
                        entryRequests,
                        generator.packingMode,
                        generator.clearGroup,
                        generator.makeReadOnly);

                    generator.onAddressablesGeneratedForAsset?.Invoke(asset);
                }
            }

            EditorUtility.ClearProgressBar();
        }

        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths,
            bool didDomainReload)
        {
            if (BuildPipeline.isBuildingPlayer)
            {
                // Don't trigger automatic Addressables rebuilds during a build
                return;
            }

            bool pathsAdded = false;
            foreach (var assetPath in importedAssets)
            {
                var type = AssetDatabase.GetMainAssetTypeAtPath(assetPath);
                if (PerTypeGenerators.ContainsKey(type) &&
                     !PathsToProcess.Contains(assetPath) &&
                     !PathsJustProcessed.Contains(assetPath))
                {
                    PathsToProcess.Add(assetPath);
                    pathsAdded = true;
                }
            }

            if (pathsAdded)
            {
                EditorApplication.delayCall -= ProcessQueuedPaths;
                EditorApplication.delayCall += ProcessQueuedPaths;
            }
        }

        private static void ProcessQueuedPaths()
        {
            ProcessGeneratorsForPaths(PathsToProcess);

            PathsJustProcessed.AddRange(PathsToProcess);
            PathsToProcess.Clear();

            EditorApplication.delayCall -= ClearPathsJustProcessed;
            EditorApplication.delayCall += ClearPathsJustProcessed;
        }

        private static void ClearPathsJustProcessed()
        {
            PathsJustProcessed.Clear();
        }

        [MenuItem("Tools/Addressables Generator/Run All Generators")]
        public static void RunAllGenerators()
        {
            foreach (var kvp in PerTypeGenerators)
            {
                var type = kvp.Key;
                var assetPaths =
                    AssetDatabase.FindAssets($"t:{type.Name}")
                        .Select(AssetDatabase.GUIDToAssetPath)
                        .ToArray();

                ProcessGeneratorsForPaths(assetPaths);
            }
        }

        #endregion

        #region IPreprocessBuildWithReport
        
        int IOrderedCallback.callbackOrder => GenerateGroupsCallbackOrder;

        void IPreprocessBuildWithReport.OnPreprocessBuild(BuildReport report)
        {
            if (AddressablesGeneratorBuildProcessor.ShouldBuildAddressablesForPlayerBuild() &&
                AddressablesGeneratorSettings.RunGeneratorsDuringBuilds)
            {
                RunAllGenerators();
            }
        }
        
        #endregion
    }
}
