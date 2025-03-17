using UnityEditor.Build;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our registered Addressable Group generators
    /// </summary>
    public class AddressablesGeneratorBuildProcessor : BuildPlayerProcessor
    {
        public int callbackOrder => (int) AddressablesGeneratorCallbackOrder.GenerateGroups;
        
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
