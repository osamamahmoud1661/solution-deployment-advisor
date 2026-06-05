using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using McTools.Xrm.Connection;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using SolutionDeploymentAdvisor.Core;
using SolutionDeploymentAdvisor.Models;
using SolutionDeploymentAdvisor.Services;
using XrmToolBox.Extensibility;
using XrmToolBox.Extensibility.Args;
using XrmToolBox.Extensibility.Interfaces;

namespace SolutionDeploymentAdvisor.UI
{
    public partial class MainControl : PluginControlBase, IXrmToolBoxPluginControl
    {
        private List<ComponentInfo>?      _analysisResult;
        private List<ComponentInfo>?      _filteredResult;
        private IOrganizationService?     _targetService;
        private string                    _targetEnvName = string.Empty;
        private string                    _sourceEnvName = string.Empty;
        private VersionDecision?          _suggestedVersionDecision;

        public MainControl()
        {
            InitializeComponent();
            cmbSplitStrategy.DataSource = Enum.GetValues(typeof(SplitStrategy));
            this.ConnectionUpdated += MainControl_ConnectionUpdated;
        }

        private void MainControl_ConnectionUpdated(object sender, ConnectionUpdatedEventArgs e)
        {
            ConnectionDetailsUpdated(EventArgs.Empty);
        }

        // ══════════════════════════════════════════════════════════════════
        // XrmToolBox connection events
        // ══════════════════════════════════════════════════════════════════
        protected void ConnectionDetailsUpdated(EventArgs e)
        {
            // Show current (source) env name in label
            var connName = ConnectionDetail?.ConnectionName;

            if (!string.IsNullOrEmpty(connName))
            {
                lblSourceEnvValue.Text = connName;
                lblSourceEnvValue.ForeColor = Color.DarkGreen;
                btnSelectSource.Text = "✔ Connected";
                btnSelectSource.BackColor = Color.FromArgb(16, 137, 62);
                btnSelectSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 110, 50);
                btnSelectSource.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 85, 40);
            }
            else
            {
                lblSourceEnvValue.Text = "Not connected";
                lblSourceEnvValue.ForeColor = Color.Gray;
                btnSelectSource.Text = "⚡ Connect Source";
                btnSelectSource.BackColor = Color.FromArgb(16, 137, 62);
                btnSelectSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 110, 50);
                btnSelectSource.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 85, 40);
            }

            // Reset source solution picker
            cmbSourceSolutions.DataSource = null;
            lblSourceVersion.Text = string.Empty;
            grid.Rows.Clear();
            ResetSummary();

            btnAnalyze.Enabled = Service != null;
            btnLoadSource.Enabled = Service != null;

            // Auto-load source solutions (only if handle is created to prevent errors on plugin load)
            if (Service != null && this.IsHandleCreated)
                btnLoadSource_Click(this, EventArgs.Empty);
        }


        private void btnSelectSource_Click(object sender, EventArgs e)
        {
            try
            {
                RaiseRequestConnectionEvent(new RequestConnectionEventArgs { ActionName = "", Control = this });
            }
            catch (NullReferenceException) { /* Ignore XrmToolBox framework bug */ }
        }

        // ══════════════════════════════════════════════════════════════════
        // SOURCE SOLUTIONS
        // ══════════════════════════════════════════════════════════════════
        private void btnLoadSource_Click(object sender, EventArgs e)
        {
            if (Service == null) { ShowNoConnection(); return; }
            btnLoadSource.Enabled = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading source solutions...",
                Work = (_, args) =>
                {
                    args.Result = LoadSolutionItems(Service);
                },
                PostWorkCallBack = (args) =>
                {
                    btnLoadSource.Enabled = true;
                    if (args.Error != null) { ShowError("Load source solutions failed", args.Error); return; }

                    var items = (List<SolutionItem>)args.Result;
                    cmbSourceSolutions.DataSource    = null;
                    cmbSourceSolutions.DisplayMember = nameof(SolutionItem.FriendlyName);
                    cmbSourceSolutions.ValueMember   = nameof(SolutionItem.SolutionId);
                    cmbSourceSolutions.DataSource    = items;

                    if (items.Count > 0)
                        UpdateSourceVersionLabel();

                    cmbSourceSolutions.SelectedIndexChanged -= SourceSolution_Changed;
                    cmbSourceSolutions.SelectedIndexChanged += SourceSolution_Changed;
                }
            });
        }

        private void SourceSolution_Changed(object? sender, EventArgs e) => UpdateSourceVersionLabel();

        private void UpdateSourceVersionLabel()
        {
            if (cmbSourceSolutions.SelectedItem is SolutionItem sol)
                lblSourceVersion.Text = $"v{sol.Version}";
        }

        // ══════════════════════════════════════════════════════════════════
        // TARGET ENVIRONMENT  –  uses XrmToolBox connection manager
        // ══════════════════════════════════════════════════════════════════
        private void btnSelectTarget_Click(object sender, EventArgs e)
        {
            try
            {
                RaiseRequestConnectionEvent(new RequestConnectionEventArgs
                {
                    ActionName        = "SelectTarget",
                    Control           = this
                });
            }
            catch (NullReferenceException)
            {
                // Ignore XrmToolBox framework bug where it throws an NRE after a successful secondary connection.
            }
        }

        /// <summary>
        /// Called by XrmToolBox framework when the user picks a connection
        /// from the connection manager dialog.
        /// </summary>
        public override void UpdateConnection(IOrganizationService newService,
            ConnectionDetail detail, string actionName, object parameter)
        {
            if (actionName == "SelectTarget")
            {
                _targetService  = newService;
                _targetEnvName  = detail.ConnectionName;

                lblTargetEnvValue.Text      = _targetEnvName;
                lblTargetEnvValue.ForeColor = Color.DarkGreen;

                // Transform button to a "connected" state
                btnSelectTarget.Text      = "✔ Connected";
                btnSelectTarget.BackColor = Color.FromArgb(16, 137, 62);
                btnSelectTarget.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 110, 50);
                btnSelectTarget.FlatAppearance.MouseDownBackColor = Color.FromArgb(10,  85, 40);

                if (cmbTargetSolutions != null) cmbTargetSolutions.Enabled  = true;
                if (btnLoadTarget != null) btnLoadTarget.Enabled       = true;
                if (cmbPublishers != null) cmbPublishers.Enabled       = true;
                if (btnLoadPublishers != null) btnLoadPublishers.Enabled   = true;

                // Auto-load target solutions
                if (btnLoadTarget != null)
                    btnLoadTarget_Click(this, EventArgs.Empty);
            }
            else
            {
                // Default – source connection update
                base.UpdateConnection(newService, detail, actionName, parameter);
            }
        }

        // ══════════════════════════════════════════════════════════════════
        // TARGET SOLUTIONS
        // ══════════════════════════════════════════════════════════════════
        private void btnLoadTarget_Click(object sender, EventArgs e)
        {
            if (_targetService == null || btnLoadTarget == null || cmbTargetSolutions == null) return;
            btnLoadTarget.Enabled = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading target solutions...",
                Work = (_, args) => args.Result = LoadSolutionItems(_targetService),
                PostWorkCallBack = (args) =>
                {
                    btnLoadTarget.Enabled = true;
                    if (args.Error != null) { ShowError("Load target solutions failed", args.Error); return; }

                    var items = (List<SolutionItem>)args.Result;
                    items.Insert(0, new SolutionItem
                    {
                        SolutionId = Guid.Empty,
                        FriendlyName = "(Search Entire Target Environment)",
                        UniqueName = string.Empty
                    });

                    cmbTargetSolutions.DataSource    = null;
                    cmbTargetSolutions.DisplayMember = nameof(SolutionItem.FriendlyName);
                    cmbTargetSolutions.ValueMember   = nameof(SolutionItem.SolutionId);
                    cmbTargetSolutions.DataSource    = items;
                    cmbTargetSolutions.SelectedIndex = 0;
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        // PUBLISHERS
        // ══════════════════════════════════════════════════════════════════
        private void btnLoadPublishers_Click(object sender, EventArgs e)
        {
            if (_targetService == null || btnLoadPublishers == null || cmbPublishers == null) return;
            btnLoadPublishers.Enabled = false;

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Loading publishers...",
                Work = (_, args) =>
                {
                    var qe = new QueryExpression("publisher")
                    {
                        ColumnSet = new ColumnSet("publisherid", "friendlyname", "uniquename")
                    };
                    qe.Criteria.AddCondition("ismanaged", ConditionOperator.Equal, false);
                    qe.AddOrder("friendlyname", OrderType.Ascending);
                    var entities = _targetService.RetrieveMultiple(qe).Entities;

                    var list = new List<PublisherItem>();
                    foreach (var e2 in entities)
                        list.Add(new PublisherItem
                        {
                            PublisherId  = e2.Id,
                            FriendlyName = e2.GetAttributeValue<string>("friendlyname") ?? e2.Id.ToString(),
                            UniqueName   = e2.GetAttributeValue<string>("uniquename") ?? string.Empty
                        });
                    args.Result = list;
                },
                PostWorkCallBack = (args) =>
                {
                    btnLoadPublishers.Enabled = true;
                    if (args.Error != null) { ShowError("Load publishers failed", args.Error); return; }

                    var items = (List<PublisherItem>)args.Result;
                    cmbPublishers.DataSource    = null;
                    cmbPublishers.DisplayMember = nameof(PublisherItem.FriendlyName);
                    cmbPublishers.ValueMember   = nameof(PublisherItem.PublisherId);
                    cmbPublishers.DataSource    = items;
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        // ANALYZE
        // ══════════════════════════════════════════════════════════════════
        private void btnAnalyze_Click(object sender, EventArgs e)
        {
            if (Service == null) { ShowNoConnection(); return; }
            if (_targetService == null) { ShowNoTargetConnection(); return; }
            if (cmbSourceSolutions.SelectedItem is not SolutionItem srcSol)
            {
                MessageBox.Show("Please load and select a source solution.", "No Source Solution",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var targetSol = cmbTargetSolutions?.SelectedItem as SolutionItem;
            Guid? targetSolutionId = (targetSol != null && targetSol.SolutionId != Guid.Empty)
                ? targetSol.SolutionId
                : null;

            btnAnalyze.Enabled   = false;
            btnCreate.Enabled    = false;
            btnExportCsv.Enabled = false;
            if (btnExportPac != null) btnExportPac.Enabled = false;
            grid.Rows.Clear();
            ResetSummary();

            WorkAsync(new WorkAsyncInfo
            {
                Message = "Analyzing components...",
                Work = (_, args) =>
                {
                    var engine = new AnalysisEngine(Service, _targetService);
                    var components = engine.Analyze(srcSol.SolutionId, targetSolutionId);

                    // Retrieve target solution versions to calculate highest version
                    var targetSolutions = _targetService != null
                        ? AnalysisEngine.RetrieveSolutionVersions(_targetService)
                        : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                    var highestVersion = AnalysisEngine.GetHighestTargetVersion(srcSol.UniqueName, targetSolutions, srcSol.Version);
                    var decision = new VersionStrategyEngine().Decide(highestVersion, components);

                    args.Result = new AnalysisRunResult
                    {
                        Components = components,
                        VersionDecision = decision
                    };
                },
                PostWorkCallBack = (args) =>
                {
                    btnAnalyze.Enabled = true;
                    if (args.Error != null) { ShowError("Analysis failed", args.Error); return; }

                    var runResult = (AnalysisRunResult)args.Result;
                    _analysisResult = runResult.Components;
                    _filteredResult = _analysisResult;
                    _suggestedVersionDecision = runResult.VersionDecision;

                    PopulateGrid(_filteredResult);
                    UpdateSummary(_analysisResult);
                    UpdateVersionPreview(_analysisResult);

                    btnCreate.Enabled    = _analysisResult.Count > 0;
                    btnExportCsv.Enabled = _analysisResult.Count > 0;
                    if (btnExportPac != null) btnExportPac.Enabled = _analysisResult.Count > 0;
                    cmbFilter.SelectedIndex = 0;
                }
            });
        }

        // ══════════════════════════════════════════════════════════════════
        // CREATE SOLUTIONS
        // ══════════════════════════════════════════════════════════════════
        private void btnCreate_Click(object sender, EventArgs e)
        {
            if (_analysisResult == null || _analysisResult.Count == 0) return;

            var srcSol = cmbSourceSolutions?.SelectedItem as SolutionItem;
            if (srcSol == null)
            {
                MessageBox.Show("Please select a source solution first.", "No Source Solution", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Fetch the publisher from the selected source solution since the user removed the Publisher UI
            var sourceSolEntity = Service.Retrieve("solution", srcSol.SolutionId, new Microsoft.Xrm.Sdk.Query.ColumnSet("publisherid"));
            var pubId = sourceSolEntity.GetAttributeValue<Microsoft.Xrm.Sdk.EntityReference>("publisherid")?.Id.ToString() ?? Guid.Empty.ToString();

            var decision = _suggestedVersionDecision ?? new VersionStrategyEngine().Decide(_analysisResult);
            var parentUniqueName = srcSol.UniqueName;
            var targetVersionStr = decision.CurrentVersion ?? "1.0.0.0";

            // ── Step 1: Check if a higher patch already exists in source ──────────
            var sourceSolutions = LoadSolutionItems(Service);
            // Show a confirmation dialog listing the patch solutions to be created
            
            
            
            
            var patchPrefix = parentUniqueName + "_Patch_";
            Version? highestSourcePatchVersion = null;
            Version.TryParse(targetVersionStr, out var targetVersionParsed);

            foreach (var solItem in sourceSolutions)
            {
                if (solItem.UniqueName.StartsWith(patchPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    if (Version.TryParse(solItem.Version, out var sourcePatchVer))
                    {
                        if (highestSourcePatchVersion == null || sourcePatchVer > highestSourcePatchVersion)
                            highestSourcePatchVersion = sourcePatchVer;
                    }
                }
            }

            // Build previews
            var previews = new List<SolutionPreview>();
            var groupedComponents = _analysisResult.GroupBy(c => c.LastTargetSolutionName ?? srcSol.UniqueName);

            // Retrieve target solution versions to ensure patches are higher version
            var targetSolutions = _targetService != null
                ? AnalysisEngine.RetrieveSolutionVersions(_targetService)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var grp in groupedComponents)
            {
                var groupKey = grp.Key;

                // Existing components: ALWAYS create as CloneAsPatch from the BASE target solution.
                var baseTargetSol = groupKey;

                // Find highest version for THIS specific base solution in the target environment.
                // Use the source base solution version as fallback to ensure new patch version is higher than source if target has none.
                var currentTargetHighest = AnalysisEngine.GetHighestTargetVersion(
                    baseTargetSol,
                    targetSolutions,
                    srcSol.Version ?? "1.0.0.0");

                // Decide next version based on existing components.
                var versionDecision = new VersionStrategyEngine().Decide(currentTargetHighest, grp.ToList());
                var nextVersion = versionDecision.SuggestedVersion;

                // Ensure the new version is strictly greater than any existing target version.
                if (Version.TryParse(nextVersion, out var nxtVer) && Version.TryParse(currentTargetHighest, out var curVer))
                {
                    while (nxtVer <= curVer)
                    {
                        var parts = nxtVer.ToString().Split('.');
                        if (parts.Length == 4 && int.TryParse(parts[3], out var rev))
                        {
                            rev++;
                            nxtVer = new Version(int.Parse(parts[0]), int.Parse(parts[1]), int.Parse(parts[2]), rev);
                            nextVersion = nxtVer.ToString();
                        }
                        else break;
                    }
                }

                var solName = $"{baseTargetSol}_Patch";
                if (solName.Length > 48) solName = solName.Substring(0, 48);

                previews.Add(new SolutionPreview
                {
                    SolutionName = solName,
                    Version = nextVersion,
                    PublisherId = pubId,
                    PatchParent = baseTargetSol,
                    Components = grp.ToList()
                });
            }

            // ── Show patch preview to developer; abort if they cancel ──────
            using var confirmDlg = new ConfirmDialog(previews);
            if (confirmDlg.ShowDialog(this) != DialogResult.OK)
                return;

            // After user confirmation, create the patches asynchronously
            var patchCount = previews.Count;
            btnCreate.Enabled = false;
            WorkAsync(new WorkAsyncInfo
            {
                Message = $"Creating {patchCount} solution(s)...",
                Work = (_, args) =>
                {
                    var svc = new SolutionService(Service);
                    foreach (var preview in previews)
                    {
                        var (_, realUniqueName) = svc.CreateSolution(preview, preview.PatchParent);
                        foreach (var comp in preview.Components)
                        {
                            try
                            {
                                svc.AddComponentToSolution(realUniqueName, comp.ComponentId, comp.ComponentType);
                            }
                            catch { }
                        }
                    }
                },
                PostWorkCallBack = (args) =>
                {
                    btnCreate.Enabled = true;
                    if (args.Error != null) { ShowError("Create patches failed", args.Error); return; }
                    using var dialog = new PostCreationDialog(Service, previews.Select(p => p.SolutionName).ToList());
                    dialog.ShowDialog();
                }
            });
        }
        private static string GetLifecycleDisplayName(ComponentLifecycle lifecycle)
        {
            return lifecycle switch
            {
                ComponentLifecycle.New => "New",
                ComponentLifecycle.ExistingUpdated => "Existing (Updated)",
                ComponentLifecycle.Unchanged => "Unchanged",
                ComponentLifecycle.Deleted => "Deleted",
                _ => "Unknown"
            };
        }

        // ══════════════════════════════════════════════════════════════════
        // EXPORT
        // ══════════════════════════════════════════════════════════════════
        private void btnExportCsv_Click(object sender, EventArgs e)
        {
            if (_filteredResult == null) return;
            using var dlg = new SaveFileDialog
                { Filter = "CSV files|*.csv", FileName = "ComponentAnalysis.csv" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            ExportService.SaveToFile(ExportService.ToCsv(_filteredResult), dlg.FileName);
            MessageBox.Show("CSV exported successfully.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnExportPac_Click(object sender, EventArgs e)
        {
            if (_analysisResult == null) return;
            using var dlg = new SaveFileDialog
                { Filter = "Shell scripts|*.sh|All files|*.*", FileName = "deploy.sh" };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            var solutionName = (cmbSourceSolutions.SelectedItem as SolutionItem)?.UniqueName ?? "MySolution";
            ExportService.SaveToFile(ExportService.ToPacCli(_analysisResult, solutionName), dlg.FileName);
            MessageBox.Show("PAC CLI script exported.", "Export", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        // ══════════════════════════════════════════════════════════════════
        // FILTER
        // ══════════════════════════════════════════════════════════════════
        private void cmbFilter_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_analysisResult == null) return;
            var filter = cmbFilter.SelectedItem?.ToString() ?? "All";
            _filteredResult = filter == "All"
                ? _analysisResult
                : _analysisResult.Where(c =>
                    c.Risk.ToString() == filter ||
                    c.Lifecycle.ToString() == filter).ToList();
            PopulateGrid(_filteredResult);
        }

        // ══════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════

        /// <summary>Queries solutions from any org service into a bindable list.</summary>
        private static List<SolutionItem> LoadSolutionItems(IOrganizationService svc)
        {
            var qe = new QueryExpression("solution")
            {
                ColumnSet = new ColumnSet("solutionid", "friendlyname", "uniquename", "version", "createdon", "ismanaged")
            };
            qe.Criteria.AddCondition("uniquename", ConditionOperator.NotEqual, "Default");
            qe.AddOrder("createdon", OrderType.Ascending);

            var items = new List<SolutionItem>();
            var svcEntityies = svc.RetrieveMultiple(qe).Entities;
            foreach (var e in svcEntityies)
            {
                var friendlyName = e.GetAttributeValue<string>("friendlyname") ?? e.Id.ToString();
                var isManaged    = e.GetAttributeValue<bool>("ismanaged");
                items.Add(new SolutionItem
                {
                    SolutionId   = e.Id,
                    FriendlyName = isManaged ? $"{friendlyName} (Managed)" : friendlyName,
                    UniqueName   = e.GetAttributeValue<string>("uniquename")   ?? string.Empty,
                    Version      = e.GetAttributeValue<string>("version")      ?? "1.0.0.0",
                    IsManaged    = isManaged
                });
            }
            return items;
        }

        private void PopulateGrid(List<ComponentInfo> components)
        {
            grid.Rows.Clear();
            foreach (var c in components)
            {
                int idx = grid.Rows.Add(
            c.Name,
            !string.IsNullOrEmpty(c.ComponentTypeName)
                ? c.ComponentTypeName
                : ComponentNameResolver.TypeLabel(c.ComponentType),
                    GetLifecycleDisplayName(c.Lifecycle),
                    c.Category.ToString(),
                    c.Risk.ToString(),
                    c.SourceVersionDetails ?? string.Empty,
                    c.TargetVersionDetails ?? string.Empty,
                    c.MissingPatches ?? string.Empty,
                    c.RiskReason ?? string.Empty);
                ApplyRiskColor(grid.Rows[idx], c.Risk);
            }
        }

        private static void ApplyRiskColor(DataGridViewRow row, RiskLevel risk)
        {
            row.DefaultCellStyle.BackColor = risk switch
            {
                RiskLevel.High   => Color.LightCoral,
                RiskLevel.Medium => Color.Khaki,
                _                => Color.LightGreen
            };
        }

        private void UpdateSummary(List<ComponentInfo> components)
        {
            int high = components.Count(c => c.Risk == RiskLevel.High);
            int med  = components.Count(c => c.Risk == RiskLevel.Medium);
            int low  = components.Count(c => c.Risk == RiskLevel.Low);
            lblHighCount.Text  = $"● High: {high}";
            lblMedCount.Text   = $"● Medium: {med}";
            lblLowCount.Text   = $"● Low: {low}";
            lblTotalCount.Text = $"Total: {components.Count}";
        }

        private void UpdateVersionPreview(List<ComponentInfo> components)
        {
            if (_suggestedVersionDecision != null)
            {
                lblVersionPreview.Text = $"Suggested version: {_suggestedVersionDecision.SuggestedVersion}  |  {_suggestedVersionDecision.Reason}";
            }
            else
            {
                var d = new VersionStrategyEngine().Decide(components);
                lblVersionPreview.Text = $"Suggested version: {d.SuggestedVersion}  |  {d.Reason}";
            }
        }

        private void ResetSummary()
        {
            lblHighCount.Text  = "● High: 0";
            lblMedCount.Text   = "● Medium: 0";
            lblLowCount.Text   = "● Low: 0";
            lblTotalCount.Text = "Total: 0";
            lblVersionPreview.Text = string.Empty;
            _suggestedVersionDecision = null;
        }

        private static void ShowNoConnection() =>
            MessageBox.Show("Please connect to a Dataverse environment first.",
                "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private static void ShowNoTargetConnection() =>
           MessageBox.Show("Please connect to Target Dataverse environment first.",
               "No Connection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        private static void ShowError(string title, Exception ex) =>
            MessageBox.Show($"{title}:\n\n{ex.Message}", "Error",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
