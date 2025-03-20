using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.AddressablesGenerator;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our Addressable dependency bundle generation
    /// </summary>
    public class AddressablesDependencyBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        public int callbackOrder => (int) AddressablesGeneratorCallbackOrder.GenerateDependencyGroups;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.GenerateDependencyGroups();
            }
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Delete all the custom bundles that we created
            // Note that this annoyingly only gets called for a successful build -- errored and canceled builds
            // will still have their bundles changed
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);
            }
        }
    }
}
