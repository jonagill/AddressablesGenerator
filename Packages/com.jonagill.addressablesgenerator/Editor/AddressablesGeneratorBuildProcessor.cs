using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Build processor for exposing Addressables build callbacks and triggering our bundle generation code
    /// </summary>
    public class AddressablesGeneratorBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        // Run before AddressablesPlayerBuildProcessor (callbackOrder 1)
        // which actually builds our bundles.
        public int callbackOrder => 0;

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
                 AddressablesGeneratorSettings.GenerateDependencyGroups)
            {
                // Delete any old dependency groups that may still exist (e.g. from a failed build)
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);

                // Find all our assets that are dependencies of multiple Addressable groups
                // and put them into their own groups to prevent duplicating those assets
                var fixDependenciesRule = new GenerateDependencyBundles();
                fixDependenciesRule.FixIssues(settings);
            }

        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Delete all the custom bundles that we created
            // Note that this annoyingly only gets called for a successful build -- errored and canceled builds
            // will still have their bundles changed
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesPlayerBuildProcessor.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroups)
            {
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);
            }
        }
    }
}
