using System.Collections.Generic;

namespace SolutionDeploymentAdvisor.Models
{
    public class SolutionPreview
    {
        public string SolutionName  { get; set; } = string.Empty;
        public string Version       { get; set; } = "1.0.0.0";
        public string Publisher     { get; set; } = string.Empty;
        public string PublisherId   { get; set; } = string.Empty;
        public List<ComponentInfo> Components { get; set; } = new();

        /// <summary>
        /// When set, this solution will be created via CloneAsPatch from this parent solution unique name.
        /// Null means create as a plain new solution.
        /// </summary>
        public string? PatchParent  { get; set; } = null;

        public string Action { get; set; } = "Create New";
        public System.Guid? ExistingSolutionId { get; set; }
    }
}
