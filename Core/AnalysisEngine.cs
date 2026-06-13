using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xrm.Sdk;
using SolutionDeploymentAdvisor.Components;
using SolutionDeploymentAdvisor.Models;
using SolutionDeploymentAdvisor.Services;

namespace SolutionDeploymentAdvisor.Core
{
    public class AnalysisEngine
    {
        private readonly IOrganizationService _sourceService;
        private readonly IOrganizationService? _targetService;
        private readonly Dictionary<int, IComponentAnalyzer> _analyzers;

        public AnalysisEngine(IOrganizationService sourceService, IOrganizationService? targetService = null)
        {
            _sourceService = sourceService ?? throw new ArgumentNullException(nameof(sourceService));
            _targetService = targetService;

            _analyzers = new List<IComponentAnalyzer>
            {
                new TableAnalyzer(),
                new FormAnalyzer(),
                new PluginAnalyzer(),
                new PluginStepAnalyzer(),
                new PowerAutomateAnalyzer()
            }.ToDictionary(a => a.ComponentType);
        }

        public static ComponentCategory GetDefaultCategory(int typeCode) => typeCode switch
        {
            1   => ComponentCategory.Data,          // Table
            2   => ComponentCategory.Data,          // Column
            3   => ComponentCategory.Data,          // Relationship
            9   => ComponentCategory.Configuration, // Option Set
            10  => ComponentCategory.Data,          // Entity Relationship
            20  => ComponentCategory.Security,      // Security Role
            26  => ComponentCategory.UI,            // Web Resource
            29  => ComponentCategory.Process,       // Cloud Flow
            44  => ComponentCategory.UI,            // Report
            60  => ComponentCategory.UI,            // Form
            61  => ComponentCategory.UI,            // Saved View
            62  => ComponentCategory.UI,            // Chart
            90  => ComponentCategory.Process,       // Plugin Assembly
            92  => ComponentCategory.Process,       // Plugin Step
            380 => ComponentCategory.Configuration, // Env Variable Definition
            381 => ComponentCategory.Configuration, // Env Variable Value
            2000=> ComponentCategory.UI,            // Model-driven App
            _   => ComponentCategory.Unknown
        };

        private static bool IsVersionLower(string? v1, string? v2)
        {
            if (string.IsNullOrEmpty(v2)) return false;
            if (string.IsNullOrEmpty(v1)) return true;
            if (Version.TryParse(v1, out var ver1) && Version.TryParse(v2, out var ver2))
            {
                return ver1 < ver2;
            }
            return string.Compare(v1, v2, StringComparison.Ordinal) < 0;
        }

        public static Dictionary<string, string> RetrieveSolutionVersions(IOrganizationService service)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var query = new Microsoft.Xrm.Sdk.Query.QueryExpression("solution")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("uniquename", "version")
            };
            try
            {
                var entities = service.RetrieveMultiple(query).Entities;
                foreach (var e in entities)
                {
                    var name = e.GetAttributeValue<string>("uniquename");
                    var ver = e.GetAttributeValue<string>("version");
                    if (!string.IsNullOrEmpty(name))
                    {
                        result[name] = ver ?? "1.0.0.0";
                    }
                }
            }
            catch
            {
                // Ignore
            }
            return result;
        }

        public static string GetHighestTargetVersion(string parentUniqueName, Dictionary<string, string> targetSolutions, string fallbackVersion)
        {
            var versions = new List<Version>();

            // Check parent solution
            if (targetSolutions.TryGetValue(parentUniqueName, out var parentVerStr))
            {
                if (Version.TryParse(parentVerStr, out var pv))
                {
                    versions.Add(pv);
                }
            }

            // Check patches
            foreach (var kv in targetSolutions)
            {
                if (kv.Key.StartsWith(parentUniqueName, StringComparison.OrdinalIgnoreCase))
                {
                    if (Version.TryParse(kv.Value, out var pv))
                    {
                        versions.Add(pv);
                    }
                }
            }

            if (versions.Count > 0)
            {
                var highest = versions.Max();
                return highest.ToString();
            }

            return fallbackVersion;
        }

        public static Dictionary<string, Guid> RetrieveExistingComponentNames(IOrganizationService sourceService, IOrganizationService targetService, List<ComponentInfo> components)
        {
            var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            if (components == null || components.Count == 0) return result;

            // ── Metadata types: MetadataId can differ across environments ──
            // We must resolve target MetadataIds by their logical names/schema names.
            var metadataStableTypes = new HashSet<int> { 2, 3, 9, 10 };
            var metadataComponents = components.Where(c => metadataStableTypes.Contains(c.ComponentType)).ToList();
            if (metadataComponents.Count > 0)
            {
                ResolveMetadataGuidsInTarget(sourceService, targetService, metadataComponents, result);
            }

            var groups = components
                .Where(c => !metadataStableTypes.Contains(c.ComponentType))
                .GroupBy(c => c.ComponentType);

            foreach (var group in groups)
            {
                var typeCode = group.Key;

                // If type is not supported by resolver, fallback to storing their source IDs
                if (!ComponentNameResolver.TypeMap.TryGetValue(typeCode, out var map))
                {
                    foreach (var c in group)
                    {
                        result[c.ComponentId.ToString()] = c.ComponentId;
                    }
                    continue;
                }

                // Entity (type 1): query TARGET metadata by LogicalName via RetrieveEntityRequest
                if (typeCode == 1)
                {
                    foreach (var c in group)
                    {
                        int retries = 3;
                        while (retries > 0)
                        {
                            try
                            {
                                var req = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
                                {
                                    LogicalName = c.Name,
                                    EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
                                };
                                var res = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)targetService.Execute(req);
                                if (res.EntityMetadata != null && res.EntityMetadata.MetadataId.HasValue)
                                {
                                    var targetId = res.EntityMetadata.MetadataId.Value;
                                    result[c.Name] = targetId;
                                    result[c.ComponentId.ToString()] = targetId; // Exact mapping
                                }
                                break; // Success, break out of retry loop
                            }
                            catch (Exception ex)
                            {
                                retries--;
                                if (retries == 0)
                                {
                                    // If not found by name after retries (or persistent connection issue), assume it doesn't exist
                                }
                                else
                                {
                                    System.Threading.Thread.Sleep(500); // Wait before retry
                                }
                            }
                        }
                    }
                    continue;
                }

                // Form (type 60): special GUID-remapping logic (GUIDs differ between envs)
                if (typeCode == 60)
                {
                    ResolveFormGuidsInTarget(sourceService, targetService, group.ToList(), result);
                    continue;
                }

                // For all other types (Workflows, Security Roles, Web Resources etc.):
                // GUIDs differ between environments — resolve by name in target
                var validNames = group.Select(c => c.Name)
                                      .Where(n => !string.IsNullOrWhiteSpace(n) && !Guid.TryParse(n, out _))
                                      .Distinct()
                                      .ToList();

                const int chunkSize = 200;
                for (int i = 0; i < validNames.Count; i += chunkSize)
                {
                    var chunk = validNames.Skip(i).Take(chunkSize).ToList();
                    try
                    {
                        var query = new Microsoft.Xrm.Sdk.Query.QueryExpression(map.Entity)
                        {
                            ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(map.NameAttr),
                            NoLock = true
                        };
                        query.Criteria.AddCondition(map.NameAttr, Microsoft.Xrm.Sdk.Query.ConditionOperator.In, chunk.Cast<object>().ToArray());

                        var entities = targetService.RetrieveMultiple(query).Entities;
                        // Only map names that have exactly 1 match to avoid ambiguity
                        var grouped = entities.GroupBy(e => e.GetAttributeValue<string>(map.NameAttr)).ToList();
                        foreach (var g in grouped)
                        {
                            var nameVal = g.Key;
                            if (!string.IsNullOrWhiteSpace(nameVal) && g.Count() == 1)
                            {
                                result[nameVal] = g.First().Id;
                            }
                        }
                    }
                    catch
                    {
                        // Fallback to individual retrieval if bulk IN fails
                        foreach (var name in chunk)
                        {
                            try
                            {
                                var fallbackQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression(map.Entity)
                                {
                                    ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet(map.NameAttr),
                                    NoLock = true,
                                    TopCount = 1
                                };
                                fallbackQuery.Criteria.AddCondition(map.NameAttr, Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, name);
                                var res = targetService.RetrieveMultiple(fallbackQuery);
                                if (res.Entities.Count > 0)
                                {
                                    result[name] = res.Entities[0].Id;
                                }
                            }
                            catch { }
                        }
                    }
                }

                // Additionally, add component IDs to the result so fallback checks by ID still work if name is a GUID
                foreach (var c in group)
                {
                    if (Guid.TryParse(c.Name, out _))
                    {
                        result[c.Name] = c.ComponentId;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Resolves target-environment MetadataIds for metadata components.
        /// Because MetadataIds can differ across environments if created manually, we must query
        /// the target environment using the component's logical identifier (Name, SchemaName, etc.).
        /// </summary>
        private static void ResolveMetadataGuidsInTarget(
            IOrganizationService sourceService,
            IOrganizationService targetService,
            List<ComponentInfo> components,
            Dictionary<string, Guid> result)
        {
            foreach (var c in components)
            {
                if (string.IsNullOrWhiteSpace(c.Name) || Guid.TryParse(c.Name, out _)) continue;

                try
                {
                    if (c.ComponentType == 2) // Column
                    {
                        // 1. Get EntityLogicalName from Source (because ComponentInfo.Name is only LogicalName)
                        var sourceReq = new Microsoft.Xrm.Sdk.Messages.RetrieveAttributeRequest
                        {
                            MetadataId = c.ComponentId,
                            RetrieveAsIfPublished = true
                        };
                        var sourceResp = (Microsoft.Xrm.Sdk.Messages.RetrieveAttributeResponse)sourceService.Execute(sourceReq);
                        var attrMeta = sourceResp.AttributeMetadata;
                        
                        if (attrMeta != null)
                        {
                            // 2. Query Target by EntityLogicalName and LogicalName
                            var targetReq = new Microsoft.Xrm.Sdk.Messages.RetrieveAttributeRequest
                            {
                                EntityLogicalName = attrMeta.EntityLogicalName,
                                LogicalName = attrMeta.LogicalName,
                                RetrieveAsIfPublished = true
                            };
                            var targetResp = (Microsoft.Xrm.Sdk.Messages.RetrieveAttributeResponse)targetService.Execute(targetReq);
                            if (targetResp?.AttributeMetadata?.MetadataId != null)
                            {
                                var targetId = targetResp.AttributeMetadata.MetadataId.Value;
                                result[c.Name] = targetId;
                                result[c.ComponentId.ToString()] = targetId; // Exact mapping prevents name collisions
                            }
                        }
                    }
                    else if (c.ComponentType == 3 || c.ComponentType == 10) // Relationship
                    {
                        var targetReq = new Microsoft.Xrm.Sdk.Messages.RetrieveRelationshipRequest
                        {
                            Name = c.Name,
                            RetrieveAsIfPublished = true
                        };
                        var targetResp = (Microsoft.Xrm.Sdk.Messages.RetrieveRelationshipResponse)targetService.Execute(targetReq);
                        if (targetResp?.RelationshipMetadata?.MetadataId != null)
                        {
                            var targetId = targetResp.RelationshipMetadata.MetadataId.Value;
                            result[c.Name] = targetId;
                            result[c.ComponentId.ToString()] = targetId;
                        }
                    }
                    else if (c.ComponentType == 9) // OptionSet
                    {
                        var targetReq = new Microsoft.Xrm.Sdk.Messages.RetrieveOptionSetRequest
                        {
                            Name = c.Name,
                            RetrieveAsIfPublished = true
                        };
                        var targetResp = (Microsoft.Xrm.Sdk.Messages.RetrieveOptionSetResponse)targetService.Execute(targetReq);
                        if (targetResp?.OptionSetMetadata?.MetadataId != null)
                        {
                            var targetId = targetResp.OptionSetMetadata.MetadataId.Value;
                            result[c.Name] = targetId;
                            result[c.ComponentId.ToString()] = targetId;
                        }
                    }
                }
                catch
                {
                    // If exception occurs, it means the metadata doesn't exist in target — leave it unmapped
                }
            }
        }



        /// <summary>
        /// Resolves target-environment GUIDs for SystemForm components (type 60).
        /// Forms have two problems: their GUIDs differ across environments, and their
        /// <c>name</c> field is NOT unique (multiple tables can share the same form name).
        /// <para>
        /// Resolution order:
        /// 1. Match by <c>uniquename</c> — truly unique within an environment.
        /// 2. Fall back to <c>name + objecttypecode</c> — unique within a single table.
        /// </para>
        /// The resolved target GUID is stored under the component's display name so the
        /// rest of <see cref="AnalysisEngine"/> can use it for layer queries.
        /// </summary>
        private static void ResolveFormGuidsInTarget(
            IOrganizationService sourceService,
            IOrganizationService targetService,
            List<ComponentInfo>   sourceForms,
            Dictionary<string, Guid> result)
        {
            if (sourceForms.Count == 0) return;

            // ── Step 1: fetch source form metadata (uniquename + objecttypecode) ──────
            var sourceIds = sourceForms.Select(c => c.ComponentId).Distinct().ToList();
            var sourceFormMeta = new Dictionary<Guid, (string UniqueName, string ObjectTypeCode, string DisplayName)>();
            
            const int chunkSize = 200;
            for (int i = 0; i < sourceIds.Count; i += chunkSize)
            {
                var chunk = sourceIds.Skip(i).Take(chunkSize).ToList();
                try
                {
                    var sourceQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("systemform")
                    {
                        ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "objecttypecode", "uniquename"),
                        NoLock = true
                    };
                    sourceQuery.Criteria.AddCondition("formid", Microsoft.Xrm.Sdk.Query.ConditionOperator.In, chunk.Cast<object>().ToArray());
                    
                    var srcEntities = sourceService.RetrieveMultiple(sourceQuery).Entities;
                    foreach (var e in srcEntities)
                    {
                        sourceFormMeta[e.Id] = (
                            e.GetAttributeValue<string>("uniquename"),
                            e.GetAttributeValue<string>("objecttypecode"),
                            e.GetAttributeValue<string>("name")
                        );
                    }
                }
                catch { }
            }

            // ── Step 2: For each source form, find its target counterpart ─────────────
            foreach (var form in sourceForms)
            {
                var formName = form.Name;
                if (string.IsNullOrWhiteSpace(formName) || Guid.TryParse(formName, out _))
                {
                    result[form.ComponentId.ToString()] = form.ComponentId;
                    continue;
                }

                try
                {
                    sourceFormMeta.TryGetValue(form.ComponentId, out var meta);

                    if (meta.UniqueName != null && !string.IsNullOrEmpty(meta.UniqueName))
                    {
                        var nameQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("systemform")
                        {
                            ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("uniquename", "formid"),
                            NoLock = true,
                            TopCount = 1
                        };
                        nameQuery.Criteria.AddCondition("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, meta.UniqueName);
                        var matches = targetService.RetrieveMultiple(nameQuery).Entities;
                        if (matches.Count > 0)
                        {
                            result[formName] = matches[0].Id;
                            continue;
                        }
                    }

                    // Fallback to name + objecttypecode
                    var fallbackQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("systemform")
                    {
                        ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "formid"),
                        NoLock = true,
                        TopCount = 2
                    };
                    fallbackQuery.Criteria.AddCondition("name", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, formName);
                    if (!string.IsNullOrEmpty(meta.ObjectTypeCode))
                    {
                        fallbackQuery.Criteria.AddCondition("objecttypecode", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, meta.ObjectTypeCode);
                    }
                    var fallbackMatches = targetService.RetrieveMultiple(fallbackQuery).Entities;
                    if (fallbackMatches.Count == 1)
                    {
                        result[formName] = fallbackMatches[0].Id;
                    }
                }
                catch { }
            }
        }

        public List<ComponentInfo> Analyze(Guid sourceSolutionId, Guid? targetSolutionId = null, string? targetSolutionName = null)
        {
            var componentSvc  = new SolutionComponentService(_sourceService);
            var nameResolver  = new ComponentNameResolver(_sourceService);
            var sourceComponents = componentSvc.GetComponents(sourceSolutionId);

            // 1. Resolve human-readable names in batch!
            nameResolver.ResolveNamesInBatch(sourceComponents);

            // 2. Fetch all solutions and versions in source & target
            var sourceSolutions = RetrieveSolutionVersions(_sourceService);
            var targetSolutions = _targetService != null ? RetrieveSolutionVersions(_targetService) : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // 6. Build list of existing component names in target environment mapping to Target ComponentIds
            var targetComponentIdsByName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            if (_targetService != null)
            {
                targetComponentIdsByName = RetrieveExistingComponentNames(_sourceService, _targetService, sourceComponents);
            }

            // 3. Fetch component IDs
            var componentIds = sourceComponents.Select(c => c.ComponentId).Distinct().ToList();

            // 4. Retrieve component layers in batch (type name required by msdyn_componentlayer)
            var sourceLayerSvc = new LayerService(_sourceService);
            var sourceLayers = sourceLayerSvc.GetLayersInBatch(sourceComponents);

            // 4b. Augment missing source layers using solutioncomponent fallback
            var missingSourceIds = sourceComponents.Select(c => c.ComponentId).Where(id => !sourceLayers.ContainsKey(id)).ToList();
            if (missingSourceIds.Count > 0)
            {
                var fallbackSrc = sourceLayerSvc.GetFallbackLayersInBatch(missingSourceIds);
                foreach (var kvp in fallbackSrc)
                {
                    sourceLayers[kvp.Key] = kvp.Value;
                }
            }

            Dictionary<Guid, List<LayerInfo>>? targetLayers = null;
            if (_targetService != null)
            {
                var targetLayerSvc = new LayerService(_targetService);
                
                // Construct target components using resolved target IDs for accurate target layer queries!
                var targetComponentsToQuery = new List<ComponentInfo>();
                foreach (var c in sourceComponents)
                {
                    Guid targetId = c.ComponentId;
                    if (targetComponentIdsByName.TryGetValue(c.Name, out var tId))
                    {
                        targetId = tId;
                    }
                    targetComponentsToQuery.Add(new ComponentInfo
                    {
                        ComponentId = targetId,
                        ComponentType = c.ComponentType,
                        Name = c.Name
                    });
                }
                
                targetLayers = targetLayerSvc.GetLayersInBatch(targetComponentsToQuery);

                // Augment missing target layers using solutioncomponent fallback
                var missingTargetIds = targetComponentsToQuery.Select(c => c.ComponentId).Where(id => !targetLayers.ContainsKey(id)).ToList();
                if (missingTargetIds.Count > 0)
                {
                    var fallbackTgt = targetLayerSvc.GetFallbackLayersInBatch(missingTargetIds);
                    foreach (var kvp in fallbackTgt)
                    {
                        targetLayers[kvp.Key] = kvp.Value;
                    }
                }
            }

            // 5. Build target lookup if available
            Dictionary<Guid, ComponentInfo>? targetLookup = null;
            if (_targetService != null && targetSolutionId.HasValue)
            {
                var targetSvc = new SolutionComponentService(_targetService);
                targetLookup  = targetSvc.GetComponents(targetSolutionId.Value)
                    .ToDictionary(c => c.ComponentId);
            }

            var enriched = new List<ComponentInfo>();
            foreach (var c in sourceComponents)
            {
                c.ComponentTypeName = ComponentNameResolver.TypeLabel(c.ComponentType);
                c.Category = GetDefaultCategory(c.ComponentType);

                // ── Source version details ───────────────────────────────────
                sourceLayers.TryGetValue(c.ComponentId, out var srcL);
                if (srcL != null && srcL.Count > 0)
                {
                    var details = srcL.OrderBy(l => l.LayerOrder)
                        .Select(l =>
                        {
                            sourceSolutions.TryGetValue(l.SolutionName, out var ver);
                            return $"{l.SolutionName} (v{ver ?? "?"})";
                        });
                    c.SourceVersionDetails = string.Join(" → ", details);
                }
                else
                {
                    c.SourceVersionDetails = "Active / Default";
                }

                // ── Target version details & missing patches ─────────────────
                bool existsInTarget = false;

                if (_targetService != null)
                {
                    Guid targetId = c.ComponentId;
                    // Prefer original component ID if layers exist for it already;
                    // only remap if layers don't exist under original ID
                    bool origHasLayers = targetLayers != null && targetLayers.ContainsKey(c.ComponentId);
                    if (!origHasLayers)
                    {
                        // Check if we mapped the Source ID -> Target ID exactly (prevents name collisions)
                        if (targetComponentIdsByName.TryGetValue(c.ComponentId.ToString(), out var exactTgtId))
                        {
                            targetId = exactTgtId;
                        }
                        else if (targetComponentIdsByName.TryGetValue(c.Name, out var tId))
                        {
                            targetId = tId;
                        }
                    }

                    List<LayerInfo>? tgtL = null;
                    if (targetLayers != null)
                        targetLayers.TryGetValue(targetId, out tgtL);

                    existsInTarget = (targetLookup != null && (targetLookup.ContainsKey(c.ComponentId) || targetLookup.ContainsKey(targetId)))
                        || targetComponentIdsByName.ContainsKey(c.ComponentId.ToString())
                        || targetComponentIdsByName.ContainsKey(c.Name)
                        || (tgtL != null && tgtL.Count > 0);

                    if (tgtL != null && tgtL.Count > 0)
                    {
                        var orderedTgt = tgtL.OrderBy(l => l.LayerOrder).ToList();
                        var details = orderedTgt.Select(l =>
                            {
                                targetSolutions.TryGetValue(l.SolutionName, out var ver);
                                return $"{l.SolutionName} (v{ver ?? "?"})";
                            });
                        c.TargetVersionDetails = string.Join(" → ", details);

                        // The top layer (highest order) is the last patch this component belongs to in target
                        c.LastTargetSolutionName = orderedTgt.Last().SolutionName;

                        // Detect missing or outdated layers
                        var missing = new List<string>();
                        if (srcL != null)
                        {
                            foreach (var sl in srcL)
                            {
                                sourceSolutions.TryGetValue(sl.SolutionName, out var srcVer);
                                var tgtMatch = tgtL.FirstOrDefault(tl => tl.SolutionName == sl.SolutionName);
                                if (tgtMatch == null)
                                {
                                    missing.Add($"{sl.SolutionName} (v{srcVer ?? "new"} not in target)");
                                }
                                else
                                {
                                    targetSolutions.TryGetValue(tgtMatch.SolutionName, out var tgtVer);
                                    if (IsVersionLower(tgtVer, srcVer))
                                        missing.Add($"{sl.SolutionName} (target v{tgtVer ?? "?"} < source v{srcVer})");
                                }
                            }
                        }
                        c.MissingPatches = missing.Count > 0 ? string.Join(", ", missing) : "None — in sync";

                        // Lifecycle: updated if patches differ, unchanged if in sync
                        c.Lifecycle = existsInTarget
                            ? (missing.Count > 0 ? ComponentLifecycle.ExistingUpdated : ComponentLifecycle.Unchanged)
                            : ComponentLifecycle.New;
                    }
                    else
                    {
                        // Component not found in any target layer
                        if (existsInTarget)
                        {
                            c.TargetVersionDetails = string.IsNullOrEmpty(targetSolutionName) ? "Present in Target (No layer data)" : targetSolutionName;
                            c.LastTargetSolutionName = targetSolutionName;
                        }
                        else
                        {
                            c.TargetVersionDetails = "Not in target";
                        }
                        c.MissingPatches       = existsInTarget ? "Unknown"       : (c.SourceVersionDetails ?? "All source layers missing");
                        c.Lifecycle            = existsInTarget ? ComponentLifecycle.ExistingUpdated : ComponentLifecycle.New;
                    }
                }
                else
                {
                    c.TargetVersionDetails = "No target environment connected";
                    c.MissingPatches       = "N/A";
                    c.Lifecycle            = ComponentLifecycle.New;
                }

                // ── Type-specific enrichment ─────────────────────────────────
                ComponentInfo result = _analyzers.TryGetValue(c.ComponentType, out var analyzer)
                    ? analyzer.Analyze(c, null)
                    : c;

                result.Risk = RiskEngine.Evaluate(result);
                enriched.Add(result);
            }

            return enriched;
        }

        public List<ComponentInfo> Analyze() => new();
    }
}
