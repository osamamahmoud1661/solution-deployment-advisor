using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>Handles Entity / Table components (type code 1).</summary>
    public class TableAnalyzer : IComponentAnalyzer
    {
        public int ComponentType => 1; // Entity

        public ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target)
        {
            source.Category = ComponentCategory.Data;



            return source;
        }
    }
}
