using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Core
{
    /// <summary>Determines the lifecycle state of a component given source and target info.</summary>
    public class LifecycleEvaluator
    {
        public ComponentLifecycle Evaluate(ComponentInfo source, ComponentInfo? target)
        {
            if (target == null)
                return ComponentLifecycle.New;

            // Simple equality check on name; extend for deep comparison
            if (source.Name == target.Name)
                return ComponentLifecycle.Unchanged;

            return ComponentLifecycle.ExistingUpdated;
        }
    }
}
