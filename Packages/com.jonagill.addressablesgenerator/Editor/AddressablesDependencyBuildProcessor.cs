using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.AddressablesGenerator;
using UnityEditor.Build;

#if !UNITY_6000_3_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace AddressablesGenerator
{
    /// <summary>
    /// Build processor for triggering our Addressable dependency bundle generation
    /// </summary>
    public class AddressablesDependencyBuildProcessor : BuildPlayerProcessor,
#if UNITY_6000_3_OR_NEWER
        IPostprocessBuildWithContext
#else
        IPostprocessBuildWithReport
#endif
    {
        public override int callbackOrder => (int) AddressablesGeneratorCallbackOrder.GenerateDependencyGroups;

        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.GenerateDependencyGroups();
            }
        }

#if UNITY_6000_3_OR_NEWER
        public void OnPostprocessBuild(BuildCallbackContext context)
        {
            PostBuildCleanup();
        }
#else
        public void OnPostprocessBuild(BuildReport report)
        {
            // Note that this annoyingly only gets called for a successful build -- errored and canceled builds
            // will still have their bundles changed
            PostBuildCleanup();
        }
#endif

        private void PostBuildCleanup()
        {
            // Delete all the custom bundles that we created
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings) && 
                AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);
            }
        }
    }
}
