using UnityEditor.AddressableAssets.Settings;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Exposes access to some internal Addressables APIs
    /// </summary>
    public static class AddressablesInternals
    {
        public static bool ShouldBuildAddressablesForPlayerBuild(AddressableAssetSettings settings)
        {
            return AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings);
        }

        public static bool IsAddressablesWindowOpen()
        {
            return EditorWindow.HasOpenInstances<GUI.AddressableAssetsWindow>();
        }

        public static EditorWindow GetAddressablesWindow()
        {
            return EditorWindow.GetWindow(typeof(GUI.AddressableAssetsWindow));
        }
    }
}