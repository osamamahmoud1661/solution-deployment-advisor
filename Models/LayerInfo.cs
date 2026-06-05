using System;

namespace SolutionDeploymentAdvisor.Models
{
    public class LayerInfo
    {
        public Guid   ComponentId   { get; set; }
        public string SolutionName  { get; set; } = string.Empty;
        public int    LayerOrder     { get; set; }
        public string PublisherName  { get; set; } = string.Empty;
    }
}
