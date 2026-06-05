using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Crm.Sdk.Messages;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.Services
{
    /// <summary>Creates and manages Dataverse solutions.</summary>
    public class SolutionService
    {
        private readonly IOrganizationService _service;

        public SolutionService(IOrganizationService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        /// <summary>
        /// Adds a component to an unmanaged solution.
        /// </summary>
        public void AddComponentToSolution(string solutionUniqueName, Guid componentId, int componentType)
        {
            var request = new AddSolutionComponentRequest
            {
                ComponentId = componentId,
                ComponentType = componentType,
                SolutionUniqueName = solutionUniqueName.Replace(" ", "_"),
                AddRequiredComponents = false
            };
            _service.Execute(request);
        }

        /// <summary>
        /// Creates a new unmanaged solution or clones as patch if parentUniqueName is provided.
        /// Returns the real unique name of the created solution/patch (important after CloneAsPatch
        /// because Dataverse auto-assigns the unique name and it may differ from what we requested).
        /// </summary>
        public (Guid SolutionId, string UniqueName) CreateSolution(string uniqueName, string friendlyName, string version, string publisherId, string? parentUniqueName = null)
        {
            if (!string.IsNullOrEmpty(parentUniqueName))
            {
                // CloneAsPatch on the SOURCE (unmanaged) environment.
                // Dataverse auto-assigns the unique name — we cannot control it.
                // We MUST query the real unique name after creation to use for AddSolutionComponent.
                var request = new CloneAsPatchRequest
                {
                    ParentSolutionUniqueName = parentUniqueName,
                    DisplayName  = friendlyName,
                    VersionNumber = version
                };
                var response = (CloneAsPatchResponse)_service.Execute(request);

                // Retrieve the system-assigned unique name of the new patch
                var patchEntity = _service.Retrieve("solution", response.SolutionId,
                    new Microsoft.Xrm.Sdk.Query.ColumnSet("uniquename"));
                var realUniqueName = patchEntity.GetAttributeValue<string>("uniquename");

                return (response.SolutionId, realUniqueName);
            }

            if (!Guid.TryParse(publisherId, out var pubGuid))
                throw new ArgumentException($"Invalid publisher GUID: {publisherId}", nameof(publisherId));

            var solution = new Entity("solution")
            {
                ["uniquename"]   = uniqueName.Replace(" ", "_"),
                ["friendlyname"] = friendlyName,
                ["version"]      = version,
                ["publisherid"]  = new EntityReference("publisher", pubGuid)
            };

            var newId = _service.Create(solution);
            return (newId, uniqueName.Replace(" ", "_"));
        }

        /// <summary>Convenience overload that reads metadata from a <see cref="SolutionPreview"/>.</summary>
        public (Guid SolutionId, string UniqueName) CreateSolution(SolutionPreview preview, string? parentUniqueName = null)
            => CreateSolution(
                preview.SolutionName.Replace(" ", "_"),
                preview.SolutionName,
                preview.Version,
                preview.PublisherId,
                parentUniqueName);

        /// <summary>
        /// Exports a solution as a ZIP file.
        /// </summary>
        public void ExportSolution(string solutionUniqueName, bool managed, string outputFilePath)
        {
            var request = new ExportSolutionRequest
            {
                SolutionName = solutionUniqueName,
                Managed = managed,
                ExportAutoNumberingSettings = false,
                ExportCalendarSettings = false,
                ExportCustomizationSettings = false,
                ExportEmailTrackingSettings = false,
                ExportGeneralSettings = false,
                ExportIsvConfig = false,
                ExportMarketingSettings = false,
                ExportOutlookSynchronizationSettings = false,
                ExportRelationshipRoles = false,
                ExportSales = false
            };

            var response = (ExportSolutionResponse)_service.Execute(request);
            System.IO.File.WriteAllBytes(outputFilePath, response.ExportSolutionFile);
        }
    }
}
