using System;

namespace SolutionDeploymentAdvisor.Models
{
    public class ComponentInfo
    {
        public Guid   Id              { get; set; }
        public Guid   ComponentId     { get; set; }
        public string Name            { get; set; } = string.Empty;
        public string ComponentTypeName { get; set; } = string.Empty;
        public int    ComponentType   { get; set; }

        public ComponentLifecycle Lifecycle { get; set; } = ComponentLifecycle.Unknown;
        public ComponentCategory  Category  { get; set; } = ComponentCategory.Unknown;
        public RiskLevel          Risk      { get; set; } = RiskLevel.Low;

        public string? SourceSolution { get; set; }
        public string? TargetSolution { get; set; }
        public string? RiskReason     { get; set; }

        public string? SourceVersionDetails    { get; set; }
        public string? TargetVersionDetails    { get; set; }
        public string? MissingPatches          { get; set; }

        public string? ManualTargetSolution    { get; set; }
        public bool    ApplySplitStrategyToManualSolution { get; set; } = true;

        /// <summary>
        /// The solution name of the highest-order layer this component belongs to in the target environment.
        /// Used to group components that share the same last patch when creating new patches.
        /// </summary>
        public string? LastTargetSolutionName  { get; set; }
    }
}
