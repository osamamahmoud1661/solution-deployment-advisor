using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SolutionDeploymentAdvisor.Models;

namespace SolutionDeploymentAdvisor.UI
{
    /// <summary>
    /// Pre-flight review dialog shown before creating patches.
    /// Lists every patch group where the program would create a newer version,
    /// and — when an existing open patch is already present in the source
    /// environment — lets the user choose per-row whether to create a brand-new
    /// patch or append to the existing one.
    /// </summary>
    public class PatchDecisionDialog : Form
    {
        // ── option strings shown in the Decision combo ─────────────────────
        private const string OPT_CREATE = "Create New Patch(Recommeded)";
        private const string OPT_APPEND = "Append to Existing(select if you want to delete it in Target and re-deploy again)";

        private readonly List<PatchDecisionRow> _rows;
        private DataGridView _grid = null!;

        public PatchDecisionDialog(List<PatchDecisionRow> rows)
        {
            _rows = rows;

            Text            = "Patch Version Review – Confirm Decisions";
            Width           = 980;
            Height          = 520;
            MinimumSize     = new Size(720, 380);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = true;
            MinimizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);
            BackColor       = Color.White;

            BuildUI();
        }

        // ─────────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Banner ───────────────────────────────────────────────────────
            int conflictCount = 0;
            foreach (var r in _rows)
                if (r.ExistingPatch != null) conflictCount++;

            var banner = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 68,
                BackColor = Color.FromArgb(230, 242, 255),
                Padding   = new Padding(16, 10, 16, 10)
            };

            var lblIcon = new Label
            {
                Text      = "🔍",
                Font      = new Font("Segoe UI Emoji", 22f),
                AutoSize  = true,
                Location  = new Point(14, 10)
            };

            var lblTitle = new Label
            {
                Text      = "Patch Version Review",
                Font      = new Font("Segoe UI Semibold", 12f),
                AutoSize  = true,
                Location  = new Point(59, 8),
                ForeColor = Color.FromArgb(20, 60, 120)
            };

            var lblSub = new Label
            {
                Text      = conflictCount == 0
                    ? $"The program will create {_rows.Count} new patch(es). No existing open patches were detected."
                    : $"{conflictCount} of {_rows.Count} patch group(s) already have an open patch in the source environment. " +
                      "Review each row and set your decision, then click Confirm All.",
                Font      = new Font("Segoe UI", 9f),
                AutoSize  = true,
                Location  = new Point(59, 34),
                ForeColor = Color.FromArgb(50, 70, 120)
            };

            banner.Controls.Add(lblIcon);
            banner.Controls.Add(lblTitle);
            banner.Controls.Add(lblSub);

            // ── Grid ─────────────────────────────────────────────────────────
            _grid = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = false,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible     = false,
                BorderStyle           = BorderStyle.None,
                BackgroundColor       = Color.White,
                GridColor             = Color.FromArgb(220, 225, 235),
                Font                  = new Font("Segoe UI", 9f),
                ColumnHeadersHeight   = 30,
                EnableHeadersVisualStyles = false,
                CellBorderStyle       = DataGridViewCellBorderStyle.SingleHorizontal
            };
            _grid.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI Semibold", 9f);
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(235, 240, 250);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(30, 50, 100);
            _grid.ColumnHeadersDefaultCellStyle.Padding   = new Padding(6, 0, 0, 0);
            _grid.DefaultCellStyle.Padding                = new Padding(6, 2, 6, 2);

            // Columns
            var colBase = new DataGridViewTextBoxColumn
            {
                Name       = "colBase",
                HeaderText = "Base Solution",
                FillWeight = 22,
                ReadOnly   = true
            };
            var colTargetVer = new DataGridViewTextBoxColumn
            {
                Name       = "colTargetVer",
                HeaderText = "Target Highest",
                FillWeight = 14,
                ReadOnly   = true
            };
            var colSourceVer = new DataGridViewTextBoxColumn
            {
                Name       = "colSourceVer",
                HeaderText = "Source Highest",
                FillWeight = 14,
                ReadOnly   = true
            };
            var colExisting = new DataGridViewTextBoxColumn
            {
                Name       = "colExisting",
                HeaderText = "Existing Open Patch in Source",
                FillWeight = 22,
                ReadOnly   = true
            };
            var colNew = new DataGridViewTextBoxColumn
            {
                Name       = "colNew",
                HeaderText = "Proposed New Version  ✓",
                FillWeight = 12,
                ReadOnly   = true
            };
            var colDecision = new DataGridViewComboBoxColumn
            {
                Name        = "colDecision",
                HeaderText  = "Your Decision  ▼",
                FillWeight  = 16,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle    = FlatStyle.Flat
            };
            colDecision.Items.AddRange(OPT_CREATE, OPT_APPEND);

            _grid.Columns.Add(colBase);
            _grid.Columns.Add(colTargetVer);
            _grid.Columns.Add(colSourceVer);
            _grid.Columns.Add(colExisting);
            _grid.Columns.Add(colNew);
            _grid.Columns.Add(colDecision);

            // Populate rows
            for (int i = 0; i < _rows.Count; i++)
            {
                var row = _rows[i];
                bool hasConflict = row.ExistingPatch != null;

                // Detect the specific scenario: source highest >= target highest
                // meaning source already had a version that would have collided
                bool sourceAheadOfTarget =
                    Version.TryParse(row.SourceHighestVersion, out var sv) &&
                    Version.TryParse(row.TargetHighestVersion, out var tv) &&
                    sv >= tv && sv.ToString() != row.TargetHighestVersion;

                string existingText = hasConflict
                    ? $"{row.ExistingPatch!.UniqueName}  (v{row.ExistingPatch.Version})"
                    : "(none)";

                int rowIdx = _grid.Rows.Add(
                    row.BaseSolution,
                    row.TargetHighestVersion,
                    row.SourceHighestVersion,
                    existingText,
                    row.ProposedNewVersion,
                    OPT_CREATE   // default: always create new
                );

                var dgvRow = _grid.Rows[rowIdx];

                if (sourceAheadOfTarget)
                {
                    // Source highest is >= target highest — the source was already ahead.
                    // Mark the Source Highest cell in orange so the user sees why the
                    // proposed version was bumped beyond what target alone would suggest.
                    dgvRow.Cells["colSourceVer"].Style.ForeColor  = Color.FromArgb(180, 80, 0);
                    dgvRow.Cells["colSourceVer"].Style.Font       = new Font("Segoe UI Semibold", 9f);
                    dgvRow.Cells["colSourceVer"].ToolTipText      =
                        "⚠ Source already had this version — the proposed new version was\n" +
                        "automatically bumped above it to prevent a version collision.";
                }

                if (hasConflict)
                {
                    // Highlight rows with existing patch conflicts (yellow background)
                    dgvRow.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 225);
                    dgvRow.Cells["colExisting"].Style.ForeColor = Color.FromArgb(180, 80, 0);
                    dgvRow.Cells["colExisting"].Style.Font      = new Font("Segoe UI Semibold", 9f);
                }
                else
                {
                    // No existing patch — lock the decision combo
                    dgvRow.Cells["colDecision"].ReadOnly         = true;
                    dgvRow.Cells["colDecision"].Style.BackColor  = Color.FromArgb(240, 242, 245);
                    dgvRow.Cells["colDecision"].Style.ForeColor  = Color.Gray;
                    dgvRow.DefaultCellStyle.ForeColor            = Color.FromArgb(80, 80, 80);
                }

                // Always highlight the Proposed New Version in blue
                dgvRow.Cells["colNew"].Style.ForeColor = Color.FromArgb(0, 80, 160);
                dgvRow.Cells["colNew"].Style.Font      = new Font("Segoe UI Semibold", 9f);
            }

            // Make read-only text cells non-editable visually
            _grid.CellBeginEdit += (s, e) =>
            {
                var col = _grid.Columns[e.ColumnIndex];
                if (col.Name != "colDecision")
                    ((DataGridView)s!).CancelEdit();
            };

            // ── Legend ───────────────────────────────────────────────────────
            var pnlLegend = new Panel
            {
                Dock      = DockStyle.Bottom,
                Height    = 28,
                BackColor = Color.FromArgb(245, 247, 252),
                Padding   = new Padding(14, 5, 0, 0)
            };
            var lblLegend = new Label
            {
                Text      = "🟡 Yellow rows = existing open patch in source (choose action).  " +
                            "🟠 Orange 'Source Highest' = source was ahead of target; version auto-bumped to avoid collision.  " +
                            "⬜ White = no patch.",
                AutoSize  = true,
                Font      = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(80, 80, 100)
            };
            pnlLegend.Controls.Add(lblLegend);

            // ── Bottom button bar ─────────────────────────────────────────────
            var pnlButtons = new Panel
            {
                Dock    = DockStyle.Bottom,
                Height  = 52,
                Padding = new Padding(10, 8, 12, 8),
                BackColor = Color.White
            };

            var btnCancel = new Button
            {
                Text         = "Cancel",
                Width        = 90,
                Height       = 34,
                FlatStyle    = FlatStyle.Flat,
                Anchor       = AnchorStyles.Right | AnchorStyles.Top,
                DialogResult = DialogResult.Cancel,
                Font         = new Font("Segoe UI", 9.5f)
            };
            btnCancel.Left = pnlButtons.ClientSize.Width - btnCancel.Width - 12;
            btnCancel.Top  = 9;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 190);

            var btnConfirm = new Button
            {
                Text      = "✔  Confirm All",
                Width     = 130,
                Height    = 34,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 137, 62),
                ForeColor = Color.White,
                Font      = new Font("Segoe UI Semibold", 9.5f),
                Anchor    = AnchorStyles.Right | AnchorStyles.Top
            };
            btnConfirm.Left = pnlButtons.ClientSize.Width - btnCancel.Width - btnConfirm.Width - 22;
            btnConfirm.Top  = 9;
            btnConfirm.FlatAppearance.BorderSize = 0;
            btnConfirm.Click += BtnConfirm_Click;

            pnlButtons.Controls.Add(btnCancel);
            pnlButtons.Controls.Add(btnConfirm);

            var sep = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(210, 215, 225) };

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;

            Controls.Add(_grid);
            Controls.Add(pnlLegend);
            Controls.Add(sep);
            Controls.Add(pnlButtons);
            Controls.Add(banner);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void BtnConfirm_Click(object? sender, EventArgs e)
        {
            // Commit any in-progress combo edit
            _grid.EndEdit();

            // Read back the user's decision for each row
            for (int i = 0; i < _rows.Count; i++)
            {
                var cell  = _grid.Rows[i].Cells["colDecision"];
                var value = cell.Value?.ToString() ?? OPT_CREATE;

                _rows[i].Decision = value == OPT_APPEND
                    ? PatchDecision.AppendToExisting
                    : PatchDecision.CreateNew;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
