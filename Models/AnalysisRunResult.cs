using System.Collections.Generic;

namespace SolutionDeploymentAdvisor.Models
{
    public class AnalysisRunResult
    {
        public List<ComponentInfo> Components { get; set; } = new();
        public VersionDecision VersionDecision { get; set; } = new();
    }
}
