namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    public enum AddressablesGeneratorCallbackOrder
    {
        GenerateGroups = -1, // AddressablesGeneratorBuildProcessor.cs
        GenerateDependencyGroups = 0, // AddressablesDependencyBuildProcessor.cs 
        BuildAddressables = 1, // AddressablesPlayerBuildProcessor.cs (built-in)
    }
}
