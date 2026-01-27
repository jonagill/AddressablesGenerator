namespace AddressablesGenerator
{
    public enum AddressablesGeneratorCallbackOrder
    {
        PreBuildCleanup = -99, // AddressablesCleanupBuildProcessor.cs
        GenerateGroups = -30, // AddressablesGeneratorBuildProcessor.cs
        SplitGroups = -20, // AddressablesGroupSplitterBuildProcessor.cs
        GenerateDependencyGroups = -10, // AddressablesDependencyBuildProcessor.cs 
        BuildAddressables = 1, // AddressablesPlayerBuildProcessor.cs (built-in)
    }
}
