using System.Collections.Generic;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Core
{
    /// <summary>Recommends a new version number based on the components being deployed.</summary>
    public class VersionStrategyEngine
    {
        public VersionDecision Decide(string currentVersion, List<ComponentInfo> components)
        {
            var parts = currentVersion.Split('.');
            if (parts.Length != 4 ||
                !int.TryParse(parts[0], out int major) ||
                !int.TryParse(parts[1], out int minor) ||
                !int.TryParse(parts[2], out int build) ||
                !int.TryParse(parts[3], out int revision))
            {
                var fallback = "1.0.0.1";
                return new VersionDecision
                {
                    CurrentVersion   = currentVersion,
                    NewVersion       = fallback,
                    SuggestedVersion = fallback,
                    Reason           = "Could not parse current version; defaulting to 1.0.0.1."
                };
            }

            string newVersion = $"{major}.{minor}.{build}.{revision + 1}";
            string reason = $"Suggested next patch version from the last version ({currentVersion}).";

            return new VersionDecision
            {
                CurrentVersion   = currentVersion,
                NewVersion       = newVersion,
                SuggestedVersion = newVersion,
                Reason           = reason
            };
        }

        /// <summary>Overload that derives current version from component list (defaults to 1.0.0.0).</summary>
        public VersionDecision Decide(List<ComponentInfo> components)
            => Decide("1.0.0.0", components);
    }
}
