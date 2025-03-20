using System.Collections.Generic;
using UnityEditor.AddressableAssets.Build.AnalyzeRules;

namespace UnityEditor.AddressableAssets.AddressablesGenerator
{
    /// <summary>
    /// Version of CheckBundleDupeDependencies that exposes some internal-only data 
    /// </summary>
    public class CheckBundleDupeDependenciesBase : CheckBundleDupeDependencies
    {
        protected IReadOnlyList<CheckDupeResult> ResultsData => m_ResultsData;
        protected IReadOnlyCollection<GUID> ImplicitAssets => m_ImplicitAssets;
    }
}