using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our Addressable dependency bundle generation
    /// </summary>
    public class AddressablesDependencyBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        // Run before AddressablesPlayerBuildProcessor (callbackOrder 1)
        // which actually builds our bundles.
        public int callbackOrder => 0;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings) && 
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
            if (AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);
            }
        }
    }
}
