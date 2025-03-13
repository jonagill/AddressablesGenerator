using UnityEditor.Build;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our registered Addressable Group generators
    /// </summary>
    public class AddressablesGeneratorBuildProcessor : BuildPlayerProcessor
    {
        // Run before AddressablesDependencyBuildProcessor (callbackOrder 0) and
        // AddressablesPlayerBuildProcessor (callbackOrder 1)
        public const int CallbackOrder = -1;
        
        // Run before AddressablesPlayerBuildProcessor (callbackOrder 1)
        // which actually builds our bundles.
        public int callbackOrder => CallbackOrder;
        
        // Exposes internal-only API for other classes to tie into
        public static bool ShouldBuildAddressablesForPlayerBuild()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            return AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings);
        }

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.RunGeneratorsDuringBuilds)
            {
                AddressableGroupGenerator.RunAllGenerators();
            }
        }
    }
}
