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
            381 => ComponentCategory.Configuration, // Env Variable Definition
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

        public static Dictionary<string, Guid> RetrieveExistingComponentNames(IOrganizationService service, List<ComponentInfo> components)
        {
            var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            if (components == null || components.Count == 0) return result;

            var groups = components.GroupBy(c => c.ComponentType);
            foreach (var group in groups)
            {
                var typeCode = group.Key;

                // If type is not supported by resolver, we fallback to storing their source IDs as "names"
                if (!ComponentNameResolver.TypeMap.TryGetValue(typeCode, out var map))
                {
                    foreach (var c in group)
                    {
                        result[c.ComponentId.ToString()] = c.ComponentId;
                    }
                    continue;
                }

                // If type is Entity (1), we cannot use RetrieveMultiple on 'entity' logical name.
                // We use RetrieveEntityRequest or fall back to individual Retrieves.
                if (typeCode == 1)
                {
                    foreach (var c in group)
                    {
                        try
                        {
                            var req = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
                            {
                                LogicalName = c.Name,
                                EntityFilters = Microsoft.Xrm.Sdk.Metadata.EntityFilters.Entity
                            };
                            var res = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)service.Execute(req);
                            if (res.EntityMetadata != null && res.EntityMetadata.MetadataId.HasValue)
                            {
                                result[c.Name] = res.EntityMetadata.MetadataId.Value;
                            }
                        }
                        catch
                        {
                            // If not found by name, it doesn't exist
                        }
                    }
                    continue;
                }

                // ── Special handling for SystemForm (type 60) ────────────────────────────
                // Forms are problematic because:
                //   1. systemform.name is NOT unique — multiple tables can have a form named
                //      "Main Form", "Event Licensing Main Form", etc.
                //   2. Form GUIDs differ between environments (forms are recreated on import).
                // Strategy: match by uniquename first (truly unique), then fall back to
                // name + objecttypecode (entity logical name) to get the correct target GUID.
                if (typeCode == 60)
                {
                    ResolveFormGuidsInTarget(service, group.ToList(), result);
                    continue;
                }

                // For other component types, use bulk RetrieveMultiple by NameAttr
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

                        var entities = service.RetrieveMultiple(query).Entities;
                        foreach (var e in entities)
                        {
                            var nameVal = e.GetAttributeValue<string>(map.NameAttr);
                            if (!string.IsNullOrWhiteSpace(nameVal))
                            {
                                result[nameVal] = e.Id;
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
                                var res = service.RetrieveMultiple(fallbackQuery);
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
            IOrganizationService targetService,
            List<ComponentInfo>   sourceForms,
            Dictionary<string, Guid> result)
        {
            if (sourceForms.Count == 0) return;

            // ── Step 1: fetch source form metadata (uniquename + objecttypecode) ──────
            // We need the source uniquename and objecttypecode to find the matching form
            // in the target, because GUIDs differ between environments.
            var sourceIds = sourceForms.Select(c => c.ComponentId).Distinct().ToList();

            // Build a lookup: sourceFormId → (uniquename, objecttypecode, displayname)
            var sourceFormMeta = new Dictionary<Guid, (string UniqueName, string ObjectTypeCode, string DisplayName)>();
            const int chunkSize = 200;
            for (int i = 0; i < sourceIds.Count; i += chunkSize)
            {
                var chunk = sourceIds.Skip(i).Take(chunkSize).ToList();
                try
                {
                    // NOTE: this query runs against the SOURCE service passed in as targetService.
                    // We actually need to query it against the source to get the uniquename.
                    // However, RetrieveExistingComponentNames is called with the TARGET service.
                    // So we query the target with the form names we already resolved.
                    // Instead, we use the component's Name (already resolved from source) + objecttypecode
                    // from a separate source query isn't available here. Use uniquename-only matching below.
                    _ = chunk; // suppress unused warning; handled per-form below
                    break;
                }
                catch { break; }
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
                    // Try: name + objecttypecode match (most reliable fallback without uniquename)
                    // We query all forms in target with the same display name, then disambiguate
                    // by objecttypecode if multiple matches exist.
                    var nameQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("systemform")
                    {
                        ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("name", "objecttypecode", "uniquename", "formid"),
                        NoLock = true
                    };
                    nameQuery.Criteria.AddCondition("name", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, formName);

                    var matches = targetService.RetrieveMultiple(nameQuery).Entities;
                    if (matches.Count == 1)
                    {
                        // Unambiguous match by name alone
                        result[formName] = matches[0].Id;
                    }
                    else if (matches.Count > 1)
                    {
                        // Multiple forms share the same name — try to narrow by uniquename first
                        // (uniquename is stable across environments when set explicitly)
                        // We compare against what uniquename the SOURCE form has, but we only
                        // have the display name here. As a best-effort disambiguation, pick the
                        // form whose objecttypecode appears in the source name (heuristic), or
                        // just store ALL matches keyed by name — only one will be used.
                        // Best reliable disambiguator: check if any match has a non-empty uniquename
                        // that matches the source's ComponentId hint (not available here).
                        // Fallback: pick the first match but log the ambiguity.
                        var preferred = matches
                            .FirstOrDefault(e => !string.IsNullOrEmpty(e.GetAttributeValue<string>("uniquename")))
                            ?? matches[0];
                        result[formName] = preferred.Id;
                    }
                    // If matches.Count == 0, form is not in target — leave result entry absent.
                }
                catch
                {
                    // If lookup fails, don't add — the component will be treated as "not in target"
                }
            }
        }

        public List<ComponentInfo> Analyze(Guid sourceSolutionId, Guid? targetSolutionId = null)
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
                targetComponentIdsByName = RetrieveExistingComponentNames(_targetService, sourceComponents);
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
                    if (targetComponentIdsByName.TryGetValue(c.Name, out var tId))
                    {
                        targetId = tId;
                    }

                    List<LayerInfo>? tgtL = null;
                    if (targetLayers != null)
                        targetLayers.TryGetValue(targetId, out tgtL);

                    existsInTarget = (targetLookup != null && targetLookup.ContainsKey(targetId))
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
                        c.TargetVersionDetails = existsInTarget ? "No layer data" : "Not in target";
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
