using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>Handles PluginAssembly components (type code 90).</summary>
    public class PluginAnalyzer : IComponentAnalyzer
    {
        public int ComponentType => 90; // PluginAssembly

        public ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target)
        {
            source.Category = ComponentCategory.Process;



            return source;
        }
    }
}
