using System;
using System.Collections.Generic;
using System.Linq;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Core
{
    /// <summary>
    /// Splits a flat component list into logical <see cref="SolutionPreview"/> buckets
    /// grouped by the selected <see cref="SplitStrategy"/>.
    /// </summary>
    public class SolutionSplitEngine
    {
        public List<SolutionPreview> Split(List<ComponentInfo> components, SplitStrategy strategy, string baseSolutionName, string version = "1.0.0.0")
        {
            var results = new List<SolutionPreview>();
            if (components == null || components.Count == 0) return results;

            if (string.IsNullOrWhiteSpace(baseSolutionName))
            {
                baseSolutionName = "Deployment";
            }

            switch (strategy)
            {
                case SplitStrategy.CustomizationVsProcess:
                    var customComponents = components.Where(c => c.Category != ComponentCategory.Process).ToList();
                    var processComponents = components.Where(c => c.Category == ComponentCategory.Process).ToList();

                    if (customComponents.Count > 0)
                    {
                        results.Add(new SolutionPreview
                        {
                            SolutionName = $"{baseSolutionName} Customizations",
                            Version      = version,
                            Components   = customComponents
                        });
                    }
                    if (processComponents.Count > 0)
                    {
                        results.Add(new SolutionPreview
                        {
                            SolutionName = $"{baseSolutionName} Processes",
                            Version      = version,
                            Components   = processComponents
                        });
                    }
                    break;


                case SplitStrategy.ByCategory:
                    var groups = components.GroupBy(c => c.Category);
                    foreach (var g in groups)
                    {
                        results.Add(new SolutionPreview
                        {
                            SolutionName = $"{baseSolutionName} {g.Key}",
                            Version      = version,
                            Components   = g.ToList()
                        });
                    }
                    break;

                case SplitStrategy.SingleSolution:
                default:
                    results.Add(new SolutionPreview
                    {
                        SolutionName = $"{baseSolutionName} Deployment",
                        Version      = version,
                        Components   = components
                    });
                    break;
            }

            return results;
        }
    }
}
