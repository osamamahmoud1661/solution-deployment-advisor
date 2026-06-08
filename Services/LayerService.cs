using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>
    /// Retrieves solution layer information for components via msdyn_componentlayer.
    /// NOTE: msdyn_componentlayer is a virtual entity that requires BOTH
    ///       msdyn_componentid AND msdyn_solutioncomponentname to return results.
    ///       Querying by componentid alone always returns zero rows.
    /// </summary>
    public class LayerService
    {
        private readonly IOrganizationService _service;

        public LayerService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        // ──────────────────────────────────────────────────────────────────────
        // Maps solutioncomponent.componenttype integer → msdyn_solutioncomponentname
        // These are the string values the virtual entity provider expects.
        // ──────────────────────────────────────────────────────────────────────
        public static string ComponentTypeName(int typeCode) => typeCode switch
        {
            1   => "Entity",
            2   => "Attribute",
            3   => "Relationship",
            4   => "Attribute Picklist Value",
            5   => "Attribute Lookup Value",
            9   => "OptionSet",
            10  => "Entity Relationship",
            14  => "Entity Key",
            16  => "Privilege",
            20  => "Role",
            26  => "WebResource",
            29  => "Workflow",
            31  => "Report",
            44  => "Report",
            60  => "SystemForm",
            61  => "SavedQuery",
            62  => "SavedQueryVisualization",
            66  => "CustomControl",
            70  => "FieldSecurityProfile",
            71  => "FieldPermission",
            90  => "PluginAssembly",
            91  => "PluginType",
            92  => "SdkMessageProcessingStep",
            95  => "ServiceEndpoint",
            380 => "EnvironmentVariableDefinition",
            381 => "EnvironmentVariableValue",
            2000=> "AppModule",
            300 => "CanvasApp",
            10001=> "CanvasApp",
            _   => string.Empty
        };

        // ──────────────────────────────────────────────────────────────────────
        // Retrieves layers for a list of components, grouped by type so we can
        // include the mandatory msdyn_solutioncomponentname filter per group.
        // Falls back to individual queries if a group returns nothing.
        // ──────────────────────────────────────────────────────────────────────
        public Dictionary<Guid, List<LayerInfo>> GetLayersInBatch(List<ComponentInfo> components)
        {
            var result = new Dictionary<Guid, List<LayerInfo>>();
            if (components == null || components.Count == 0) return result;

            // Group by component type so we can batch within the same type
            var byType = components
                .Where(c => !string.IsNullOrEmpty(ComponentTypeName(c.ComponentType)))
                .GroupBy(c => c.ComponentType);

            foreach (var group in byType)
            {
                var typeName = ComponentTypeName(group.Key);
                var ids      = group.Select(c => c.ComponentId).Distinct().ToList();

                const int chunkSize = 50; // Keep chunks small for virtual entity
                for (int i = 0; i < ids.Count; i += chunkSize)
                {
                    var chunk = ids.Skip(i).Take(chunkSize).ToList();
                    bool chunkGotResults = false;

                    try
                    {
                        var query = new QueryExpression("msdyn_componentlayer")
                        {
                            NoLock    = true,
                            ColumnSet = new ColumnSet("msdyn_componentid", "msdyn_solutionname",
                                                      "msdyn_order",       "msdyn_publishername")
                        };

                        // BOTH filters are required by the virtual entity provider
                        query.Criteria.AddCondition("msdyn_solutioncomponentname",
                            ConditionOperator.Equal, typeName);
                        query.Criteria.AddCondition("msdyn_componentid",
                            ConditionOperator.In, chunk.Select(id => (object)id.ToString()).ToArray());

                        var collection = _service.RetrieveMultiple(query);
                        foreach (var e in collection.Entities)
                        {
                            if (!TryParseComponentId(e, out var cid)) continue;

                            var solName = e.GetAttributeValue<string>("msdyn_solutionname") ?? string.Empty;
                            if (solName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                solName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                                continue;

                            if (!result.ContainsKey(cid)) result[cid] = new List<LayerInfo>();
                            result[cid].Add(BuildLayer(cid, e));
                            chunkGotResults = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogError(ex);
                    }

                    // If the chunk batch returned nothing, fall back to per-component queries
                    if (!chunkGotResults)
                    {
                        foreach (var id in chunk)
                        {
                            if (result.ContainsKey(id)) continue; // already have data
                            var layers = GetLayers(id, typeName);
                            if (layers.Count > 0) result[id] = layers;
                        }
                    }
                }
            }

            return result;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Retrieves layers for one component — requires both ID and type name.
        // ──────────────────────────────────────────────────────────────────────
        public List<LayerInfo> GetLayers(Guid componentId, string componentTypeName)
        {
            var results = new List<LayerInfo>();
            if (string.IsNullOrEmpty(componentTypeName)) return results;

            try
            {
                var query = new QueryExpression("msdyn_componentlayer")
                {
                    NoLock    = true,
                    ColumnSet = new ColumnSet("msdyn_componentid", "msdyn_solutionname",
                                              "msdyn_order",       "msdyn_publishername")
                };

                query.Criteria.AddCondition("msdyn_solutioncomponentname",
                    ConditionOperator.Equal, componentTypeName);
                query.Criteria.AddCondition("msdyn_componentid",
                    ConditionOperator.Equal, componentId.ToString());

                var collection = _service.RetrieveMultiple(query);
                foreach (var e in collection.Entities)
                {
                    var solName = e.GetAttributeValue<string>("msdyn_solutionname") ?? string.Empty;
                    if (solName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                        solName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                        continue;

                    results.Add(BuildLayer(componentId, e));
                }
            }
            catch (Exception ex)
            {
                LogError(ex);
            }

            return results;
        }

        // ──────────────────────────────────────────────────────────────────────
        // Retrieves fallback layers using solutioncomponent and solution for metadata
        // components that do not expose layers via msdyn_componentlayer.
        // ──────────────────────────────────────────────────────────────────────
        public Dictionary<Guid, List<LayerInfo>> GetFallbackLayersInBatch(List<Guid> componentIds)
        {
            var result = new Dictionary<Guid, List<LayerInfo>>();
            if (componentIds == null || componentIds.Count == 0) return result;

            const int chunkSize = 200;
            for (int i = 0; i < componentIds.Count; i += chunkSize)
            {
                var chunk = componentIds.Skip(i).Take(chunkSize).ToList();
                try
                {
                    var qe = new QueryExpression("solutioncomponent")
                    {
                        ColumnSet = new ColumnSet("objectid"),
                        NoLock = true
                    };
                    qe.Criteria.AddCondition("objectid", ConditionOperator.In, chunk.Cast<object>().ToArray());

                    var link = qe.AddLink("solution", "solutionid", "solutionid", JoinOperator.Inner);
                    link.EntityAlias = "sol";
                    link.Columns = new ColumnSet("uniquename");
                    
                    var pubLink = link.AddLink("publisher", "publisherid", "publisherid", JoinOperator.Inner);
                    pubLink.EntityAlias = "pub";
                    pubLink.Columns = new ColumnSet("customizationprefix");

                    var collection = _service.RetrieveMultiple(qe);

                    foreach (var e in collection.Entities)
                    {
                        var objectId = e.GetAttributeValue<Guid>("objectid");
                        var solName = e.GetAttributeValue<AliasedValue>("sol.uniquename")?.Value as string ?? string.Empty;
                        var prefix = e.GetAttributeValue<AliasedValue>("pub.customizationprefix")?.Value as string ?? string.Empty;

                        // Skip system layers — "Active" and "Default" are always present on every
                        // component in Dataverse and are not real deployable solution layers.
                        // Showing them as the target version produces the misleading "Active (v1.0)" display.
                        if (solName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                            solName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (!result.ContainsKey(objectId)) result[objectId] = new List<LayerInfo>();

                        result[objectId].Add(new LayerInfo
                        {
                            ComponentId   = objectId,
                            SolutionName  = solName,
                            LayerOrder    = 1,
                            PublisherName = prefix
                        });
                    }
                }
                catch
                {
                    // Fallback to individual
                    foreach (var id in chunk)
                    {
                        try
                        {
                            var qe = new QueryExpression("solutioncomponent")
                            {
                                ColumnSet = new ColumnSet("objectid"),
                                NoLock = true
                            };
                            qe.Criteria.AddCondition("objectid", ConditionOperator.Equal, id);

                            var link = qe.AddLink("solution", "solutionid", "solutionid", JoinOperator.Inner);
                            link.EntityAlias = "sol";
                            link.Columns = new ColumnSet("uniquename");

                            var pubLink = link.AddLink("publisher", "publisherid", "publisherid", JoinOperator.Inner);
                            pubLink.EntityAlias = "pub";
                            pubLink.Columns = new ColumnSet("customizationprefix");

                            var collection = _service.RetrieveMultiple(qe);
                            foreach (var e in collection.Entities)
                            {
                                var solName = e.GetAttributeValue<AliasedValue>("sol.uniquename")?.Value as string ?? string.Empty;
                                var prefix = e.GetAttributeValue<AliasedValue>("pub.customizationprefix")?.Value as string ?? string.Empty;

                                // Skip system layers — same reason as in batch path above.
                                if (solName.Equals("Active", StringComparison.OrdinalIgnoreCase) ||
                                    solName.Equals("Default", StringComparison.OrdinalIgnoreCase))
                                    continue;

                                if (!result.ContainsKey(id)) result[id] = new List<LayerInfo>();
                                result[id].Add(new LayerInfo
                                {
                                    ComponentId   = id,
                                    SolutionName  = solName,
                                    LayerOrder    = 1,
                                    PublisherName = prefix
                                });
                            }
                        }
                        catch { }
                    }
                }
            }

            foreach(var key in result.Keys.ToList())
            {
                result[key] = result[key].GroupBy(l => l.SolutionName).Select(g => g.First()).ToList();
            }

            return result;
        }

        // ── helpers ───────────────────────────────────────────────────────────

        private static bool TryParseComponentId(Entity e, out Guid id)
        {
            id = Guid.Empty;
            var val = e.GetAttributeValue<object>("msdyn_componentid");
            if (val is Guid g) { id = g; return true; }
            if (val is string s && Guid.TryParse(s, out id)) return true;
            return false;
        }

        private static LayerInfo BuildLayer(Guid componentId, Entity e) => new LayerInfo
        {
            ComponentId   = componentId,
            SolutionName  = e.GetAttributeValue<string>("msdyn_solutionname")  ?? string.Empty,
            LayerOrder    = e.GetAttributeValue<int>("msdyn_order"),
            PublisherName = e.GetAttributeValue<string>("msdyn_publishername") ?? string.Empty
        };

        private static void LogError(Exception ex)
        {
            try
            {
                System.IO.File.WriteAllText(
                    @"d:\DeployGuard\New folder (6)\SolutionDeploymentAdvisor (5)\SolutionDeploymentAdvisor\layer_error.log",
                    ex.ToString());
            }
            catch { }
        }
    }
}
