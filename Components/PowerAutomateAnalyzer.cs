using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>Handles Cloud Flow / Workflow components (type code 29).</summary>
    public class PowerAutomateAnalyzer : IComponentAnalyzer
    {
        public int ComponentType => 29; // Workflow (Cloud Flow)

        public ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target)
        {
            source.Category = ComponentCategory.Process;

            if (source.Lifecycle == ComponentLifecycle.ExistingUpdated)
            {
                source.RiskReason = "Cloud Flow update – verify connections and triggers in target environment.";
            }

            return source;
        }
    }
}
