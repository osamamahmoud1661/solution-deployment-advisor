using System;

namespace SolutionDeploymentAdvisor.Models
{
    /// <summary>
    /// Flat wrapper around a solution entity so ComboBox data binding
    /// can find FriendlyName and SolutionId as real .NET properties.
    /// </summary>
    public class SolutionItem
    {
        public Guid   SolutionId   { get; set; }
        public string FriendlyName { get; set; } = string.Empty;
        public string UniqueName   { get; set; } = string.Empty;
        public string Version      { get; set; } = string.Empty;
        public bool   IsManaged    { get; set; }

        public override string ToString() => FriendlyName;
    }
}
