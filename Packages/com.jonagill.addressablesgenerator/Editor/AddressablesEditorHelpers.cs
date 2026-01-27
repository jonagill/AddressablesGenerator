using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Object = UnityEngine.Object;

namespace AddressablesGenerator
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

        public static void CopySchemasToGroup<T>(AddressableAssetGroup from, AddressableAssetGroup to) where T : class
        {
            foreach (var schema in from.Schemas)
            {
                if (schema is T)
                {
                    // If our target doesn't have a schema of this type, copy it over
                    var schemaType = schema.GetType();
                    if (to.GetSchema(schemaType) == null)
                    {
                        to.AddSchema(schema);
                    }
                }
            }
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
