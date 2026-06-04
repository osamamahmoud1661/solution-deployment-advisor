using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Components
{
    /// <summary>Handles SDKMessageProcessingStep components (type code 92).</summary>
    public class PluginStepAnalyzer : IComponentAnalyzer
    {
        public int ComponentType => 92; // SDKMessageProcessingStep

        public ComponentInfo Analyze(ComponentInfo source, ComponentInfo? target)
        {
            source.Category = ComponentCategory.Process;

            if (source.Lifecycle == ComponentLifecycle.ExistingUpdated)
            {
                source.RiskReason = "Plugin step modification may affect active integrations.";
            }

            return source;
        }
    }
}
