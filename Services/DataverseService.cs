using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>Low-level Dataverse queries.</summary>
    public class DataverseService
    {
        private readonly IOrganizationService _service;

        public DataverseService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Returns all solution components for the given solution unique name.</summary>
        public List<ComponentInfo> GetSolutionComponents(string solutionUniqueName)
        {
            var results = new List<ComponentInfo>();

            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("objectid", "componenttype")
            };

            var solutionLink = query.AddLink("solution", "solutionid", "solutionid");
            solutionLink.LinkCriteria.AddCondition("uniquename", ConditionOperator.Equal, solutionUniqueName);

            var collection = _service.RetrieveMultiple(query);

            foreach (var entity in collection.Entities)
            {
                results.Add(new ComponentInfo
                {
                    Id       = entity.GetAttributeValue<Guid>("objectid"),
                    ComponentType = entity.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0,
                    SourceSolution = solutionUniqueName
                });
            }

            return results;
        }
    }
}
