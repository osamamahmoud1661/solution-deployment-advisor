using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Core
{
    /// <summary>
    /// Assigns a <see cref="RiskLevel"/> to a component based on its lifecycle and category.
    /// High   = new Process component (plugin, flow, step) – likely to break integrations.
    /// Medium = any existing component being updated.
    /// Low    = unchanged or simple data/UI additions.
    /// </summary>
    public static class RiskEngine
    {
        public static RiskLevel Evaluate(ComponentInfo c)
        {
            // New automation/process components are the highest risk
            if (c.Lifecycle == ComponentLifecycle.New && c.Category == ComponentCategory.Process)
                return RiskLevel.High;

            // Any updated component carries medium risk
            if (c.Lifecycle == ComponentLifecycle.ExistingUpdated)
                return RiskLevel.Medium;

            // Deleted components are always high risk
            if (c.Lifecycle == ComponentLifecycle.Deleted)
                return RiskLevel.High;

            return RiskLevel.Low;
        }
    }
}
