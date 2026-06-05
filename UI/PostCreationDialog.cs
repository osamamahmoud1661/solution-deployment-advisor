using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Xrm.Sdk;
using SolutionDeploymentAdvisor.Services;

namespace SolutionDeploymentAdvisor.UI
{
    public partial class PostCreationDialog : Form
    {
        private readonly IOrganizationService _service;
        private readonly List<string> _solutionNames;
        private BackgroundWorker? _worker;

        public PostCreationDialog(IOrganizationService service, List<string> solutionNames)
        {
            InitializeComponent();
            _service      = service;
            _solutionNames = solutionNames;

            // Populate checklist — all checked by default
            foreach (var sol in _solutionNames)
                chkSolutions.Items.Add(sol, true);

            // Managed / Unmanaged dropdown
            cmbExportType.Items.Add("Managed");
            cmbExportType.Items.Add("Unmanaged");
            cmbExportType.SelectedIndex = 0;

            // Default output folder = Downloads
            txtOutputFolder.Text = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog
            {
                Description  = "Select a folder to save the exported solutions",
                SelectedPath = txtOutputFolder.Text
            };
            if (dlg.ShowDialog() == DialogResult.OK)
                txtOutputFolder.Text = dlg.SelectedPath;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            var selected = chkSolutions.CheckedItems.Cast<string>().ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show("Please select at least one solution to export.",
                    "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var outputFolder = txtOutputFolder.Text;
            if (!Directory.Exists(outputFolder))
            {
                MessageBox.Show("The selected output folder does not exist.",
                    "Invalid Folder", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var isManaged = cmbExportType.SelectedIndex == 0;
            var typeLabel = isManaged ? "managed" : "unmanaged";

            SetUiEnabled(false);
            lblStatus.Text        = "Exporting...";
            lblStatus.ForeColor   = Color.Blue;
            btnOpenFolder.Visible = false;

            _worker = new BackgroundWorker();

            _worker.DoWork += (_, args) =>
            {
                var svc   = new SolutionService(_service);
                var files = new List<string>();

                foreach (var sol in (List<string>)args.Argument!)
                {
                    var uniqueName = sol.Replace(" ", "_");
                    var filePath   = Path.Combine(outputFolder, $"{uniqueName}_{typeLabel}.zip");
                    svc.ExportSolution(uniqueName, isManaged, filePath);
                    files.Add(filePath);
                }

                args.Result = files;
            };

            _worker.RunWorkerCompleted += (_, args) =>
            {
                SetUiEnabled(true);

                if (args.Error != null)
                {
                    lblStatus.Text      = "Export failed.";
                    lblStatus.ForeColor = Color.Red;
                    MessageBox.Show($"Export failed:\n\n{args.Error.Message}",
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var exported = (List<string>)args.Result!;
                lblStatus.Text        = $"✔ Successfully exported {exported.Count} solution(s).";
                lblStatus.ForeColor   = Color.Green;
                btnOpenFolder.Visible = true;
            };

            _worker.RunWorkerAsync(selected);
        }

        private void SetUiEnabled(bool enabled)
        {
            btnExport.Enabled     = enabled;
            btnBrowse.Enabled     = enabled;
            chkSolutions.Enabled  = enabled;
            cmbExportType.Enabled = enabled;
        }

        private void btnOpenFolder_Click(object sender, EventArgs e)
        {
            if (Directory.Exists(txtOutputFolder.Text))
                Process.Start("explorer.exe", txtOutputFolder.Text);
        }

        private void btnClose_Click(object sender, EventArgs e) => Close();
    }
}
