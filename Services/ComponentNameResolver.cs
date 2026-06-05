using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>
    /// Resolves human-readable names for component object IDs based on their type code.
    /// </summary>
    public class ComponentNameResolver
    {
        private readonly IOrganizationService _service;

        public static readonly Dictionary<int, (string Entity, string NameAttr)> TypeMap = new()
        {
            { 1,   ("entity",                        "name") },
            { 2,   ("attribute",                     "logicalname") },
            { 3,   ("relationship",                  "schemaname") },
            { 10,  ("entityrelationship",            "schemaname") },
            { 60,  ("systemform",                    "name") },
            { 61,  ("savedquery",                    "name") },
            { 62,  ("savedqueryvisualization",       "name") },
            { 29,  ("workflow",                      "name") },
            { 90,  ("pluginassembly",                "name") },
            { 92,  ("sdkmessageprocessingstep",      "name") },
            { 9,   ("optionset",                     "name") },
            { 26,  ("webresource",                   "name") },
            { 20,  ("role",                          "name") },
            { 44,  ("report",                        "name") },
            { 300, ("canvasapp",                     "name") },
            { 2000,("appmodule",                     "name") },
        };

        public ComponentNameResolver(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Resolves human-readable names for all components in bulk to avoid N+1 query performance hits.
        /// </summary>
        public void ResolveNamesInBatch(List<ComponentInfo> components)
        {
            if (components == null || components.Count == 0) return;

            var groups = components.GroupBy(c => c.ComponentType);
            foreach (var group in groups)
            {
                var typeCode = group.Key;
                if (!TypeMap.TryGetValue(typeCode, out var map))
                {
                    foreach (var c in group)
                    {
                        c.Name = $"{c.ComponentId} (type {typeCode})";
                    }
                    continue;
                }

                var idAttributeName = map.Entity + "id";
                var distinctIds = group.Select(c => c.ComponentId).Distinct().ToList();
                var nameLookup = new Dictionary<Guid, string>();

                const int chunkSize = 200;
                for (int i = 0; i < distinctIds.Count; i += chunkSize)
                {
                    var chunk = distinctIds.Skip(i).Take(chunkSize).ToList();

                    try
                    {
                        var query = new QueryExpression(map.Entity)
                        {
                            ColumnSet = new ColumnSet(map.NameAttr)
                        };
                        query.Criteria.AddCondition(idAttributeName, ConditionOperator.In, chunk.Cast<object>().ToArray());
                        var results = _service.RetrieveMultiple(query);
                        foreach (var entity in results.Entities)
                        {
                            var name = entity.GetAttributeValue<string>(map.NameAttr);
                            if (!string.IsNullOrWhiteSpace(name))
                            {
                                nameLookup[entity.Id] = name;
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to individual retrieves if query fails
                        foreach (var id in chunk)
                        {
                            try
                            {
                                var entity = _service.Retrieve(map.Entity, id, new ColumnSet(map.NameAttr));
                                var name = entity.GetAttributeValue<string>(map.NameAttr);
                                if (!string.IsNullOrWhiteSpace(name))
                                {
                                    nameLookup[id] = name;
                                }
                            }
                            catch
                            {
                                // Ignore
                            }
                        }
                    }
                }

                foreach (var c in group)
                {
                    if (nameLookup.TryGetValue(c.ComponentId, out var resolvedName))
                    {
                        c.Name = resolvedName;
                    }
                    else
                    {
                        c.Name = c.ComponentId.ToString();
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to resolve a display name for the component.
        /// Returns the GUID string if the type is unknown or the record is not found.
        /// </summary>
        public string Resolve(Guid objectId, int typeCode)
        {
            if (!TypeMap.TryGetValue(typeCode, out var map))
                return $"{objectId} (type {typeCode})";

            try
            {
                var entity = _service.Retrieve(map.Entity, objectId, new ColumnSet(map.NameAttr));
                var name = entity.GetAttributeValue<string>(map.NameAttr);
                return string.IsNullOrWhiteSpace(name) ? objectId.ToString() : name;
            }
            catch
            {
                return objectId.ToString();
            }
        }

        /// <summary>Returns a friendly type label for the type code.</summary>
        public static string TypeLabel(int typeCode) => typeCode switch
        {
            1   => "Table (Entity)",
            2   => "Column (Attribute)",
            3   => "Relationship",
            9   => "Option Set",
            10  => "Entity Relationship",
            20  => "Security Role",
            26  => "Web Resource",
            29  => "Cloud Flow",
            44  => "Report",
            60  => "Form",
            61  => "Saved View",
            62  => "Chart",
            90  => "Plugin Assembly",
            92  => "Plugin Step",
            300 => "Canvas App",
            380 => "Env Variable Definition",
            381 => "Env Variable Value",
            2000=> "Model-driven App",
            _   => $"Type {typeCode}"
        };
    }
}
