using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>
    /// Implement this interface for each Dataverse component type you want to analyze.
    /// Register implementations in AnalysisEngine.
    /// </summary>
    public interface IComponentAnalyzer
    {
        /// <summary>Dataverse component type code this analyzer handles.</summary>
        int ComponentType { get; }

        /// <summary>
        /// Analyze <paramref name="source"/> against <paramref name="target"/> (may be null for new components)
        /// and return an enriched <see cref="ComponentInfo"/>.
        /// </summary>
        ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target);
    }
}
