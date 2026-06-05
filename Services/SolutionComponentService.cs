using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionDeploymentAdvisor.Models;
using System;
using System.Collections.Generic;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>
    /// Retrieves the raw solution component records for a given solution ID.
    /// </summary>
    public class SolutionComponentService
    {
        private readonly IOrganizationService _service;

        public SolutionComponentService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>Returns all components belonging to <paramref name="solutionId"/>.</summary>
        public List<ComponentInfo> GetComponents(Guid solutionId)
        {
            var result = new List<ComponentInfo>();

            var query = new QueryExpression("solutioncomponent")
            {
                ColumnSet = new ColumnSet("componenttype", "objectid")
            };

            query.Criteria.AddCondition("solutionid", ConditionOperator.Equal, solutionId);

            foreach (var sc in _service.RetrieveMultiple(query).Entities)
            {
                var objectId   = sc.GetAttributeValue<Guid>("objectid");
                var typeCode   = sc.GetAttributeValue<OptionSetValue>("componenttype")?.Value ?? 0;

                result.Add(new ComponentInfo
                {
                    Id            = sc.Id,
                    ComponentId   = objectId,
                    ComponentType = typeCode,
                    Name          = objectId.ToString()   // enriched later by analyzers
                });
            }

            return result;
        }
    }
}
