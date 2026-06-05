namespace SolutionDeploymentAdvisor.Models
{
    public class VersionDecision
    {
        public string CurrentVersion    { get; set; } = "1.0.0.0";
        public string NewVersion        { get; set; } = "1.0.0.0";
        public string SuggestedVersion  { get; set; } = "1.0.0.0";
        public string Reason            { get; set; } = string.Empty;
    }
}
