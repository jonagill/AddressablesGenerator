namespace AddressablesGenerator
{
    public enum AddressablesGeneratorCallbackOrder
    {
        PreBuildCleanup = -99, // AddressablesCleanupBuildProcessor.cs
        GenerateGroups = -2, // AddressablesGeneratorBuildProcessor.cs
        SplitGroups = -1, // AddressablesGroupSplitterBuildProcessor.cs
        GenerateDependencyGroups = 0, // AddressablesDependencyBuildProcessor.cs 
        BuildAddressables = 1, // AddressablesPlayerBuildProcessor.cs (built-in)
    }
}
