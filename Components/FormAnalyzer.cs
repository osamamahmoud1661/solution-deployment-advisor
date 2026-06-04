using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>Handles SystemForm components (type code 60).</summary>
    public class FormAnalyzer : IComponentAnalyzer
    {
        public int ComponentType => 60; // SystemForm

        public ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target)
        {
            source.Category = ComponentCategory.UI;



            return source;
        }
    }
}
