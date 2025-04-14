using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.AddressablesGenerator;
using UnityEditor.Build;

namespace AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our registered Addressable Group generators
    /// </summary>
    public class AddressablesGeneratorBuildProcessor : BuildPlayerProcessor
    {
        public override int callbackOrder => (int) AddressablesGeneratorCallbackOrder.GenerateGroups;
        
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.RunGeneratorsDuringBuilds)
            {
                AddressableGroupGenerator.RunAllGenerators();
            }
        }
    }
}
