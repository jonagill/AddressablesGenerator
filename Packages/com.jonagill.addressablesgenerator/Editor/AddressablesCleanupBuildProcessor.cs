using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.AddressablesGenerator;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;

namespace AddressablesGenerator
{
    /// <summary>
    /// Build processor that undoes the results of any automated build steps before running a new build
    /// Theoretically each of our other processors should clean up after themselves at the end of each build,
    /// but OnPostprocessBuild() doesn't get invoked if a build errors or gets canceled, which can leave us in a bad state
    /// </summary>
    public class AddressablesCleanupBuildProcessor : BuildPlayerProcessor, IPostprocessBuildWithReport
    {
        public override int callbackOrder => (int) AddressablesGeneratorCallbackOrder.PreBuildCleanup;

        
        public override void PrepareForBuild(BuildPlayerContext buildPlayerContext)
        {
            
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (!AddressablesInternals.ShouldBuildAddressablesForPlayerBuild(settings))
            {
                return;
            }

            AssetDatabase.StartAssetEditing();

            // Clear our dependency groups
            if (AddressablesGeneratorSettings.GenerateDependencyGroupsDuringBuilds)
            {
                GenerateDependencyBundles.DeleteAllDependencyGroups(settings);
            }
            
            // Clear our existing single-bundle groups
            if (AddressablesGeneratorSettings.SplitGroupsIntoSingleBundleGroupsDuringBuilds)
            {
                AddressablesGroupSplitterBuildProcessor.ClearSingleBundleGroups();
            }
            
            // Prompt the user to save any open scenes
            // otherwise they may be prompted after the processor runs, pausing the build
            // which can be very annoying if you have already walked away from your machine
            EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
            
            AssetDatabase.StopAssetEditing();
        }
                
        public void OnPostprocessBuild(BuildReport report) { }
    }
}
