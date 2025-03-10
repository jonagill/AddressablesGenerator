using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Additional helper methods for working with the Addressables API in the editor 
    /// </summary>
    public static class AddressablesEditorHelpers
    {
        public static AssetReference CreateAssetReference(Object asset)
        {
            var assetGuid = GetAssetGuid( asset );
            if ( string.IsNullOrEmpty( assetGuid ) )
            {
                return null;
            }

            return new AssetReference(assetGuid);
        }

        public static AssetReferenceGameObject CreatePrefabAssetReference(GameObject asset)
        {
            var assetGuid = GetAssetGuid( asset );
            if ( string.IsNullOrEmpty( assetGuid ) )
            {
                return null;
            }

            return new AssetReferenceGameObject( assetGuid );
        }

        private static string GetAssetGuid(Object asset)
        {
            var assetPath = AssetDatabase.GetAssetPath(asset);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);

            if (string.IsNullOrEmpty(assetGuid))
            {
                Debug.LogError( $"Cannot construct AssetReference for asset {asset} as it has no valid asset GUID." );
                return null;
            }

            var assetEntry = AddressableAssetSettingsDefaultObject.Settings.FindAssetEntry(assetGuid);
            if (assetEntry == null)
            {
                Debug.LogError( $"Cannot construct AssetReference for asset {asset} as it has not been added to the Addressables system." );
                return null;
            }

            return assetGuid;
        }
    }
}
