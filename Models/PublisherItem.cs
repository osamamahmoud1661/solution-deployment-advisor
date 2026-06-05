using System;

namespace SolutionDeploymentAdvisor.Models
{
    /// <summary>Flat wrapper for publisher entity — enables ComboBox data binding.</summary>
    public class PublisherItem
    {
        public Guid   PublisherId  { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string UniqueName   { get; set; } = string.Empty;

        public override string ToString() => FriendlyName;
    }
}
