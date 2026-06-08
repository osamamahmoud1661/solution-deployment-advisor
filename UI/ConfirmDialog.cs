using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using SolutionDeploymentAdvisor.Core;
using SolutionDeploymentAdvisor.Models;
using SolutionDeploymentAdvisor.Services;

namespace SolutionDeploymentAdvisor.UI
{
    /// <summary>
    /// Shows the developer a preview of all patch solutions that will be created.
    /// Select a patch in the top grid to see its components in the bottom grid.
    /// </summary>
    public class ConfirmDialog : Form
    {
        private readonly List<SolutionPreview> _previews;
        private DataGridView _patchGrid   = null!;
        private DataGridView _compGrid    = null!;

        public ConfirmDialog(List<SolutionPreview> previews)
        {
            _previews = previews;

            Text            = "Review Solution(s)/Patch(es) to Create";
            Width           = 900;
            Height          = 600;
            MinimumSize     = new Size(700, 460);
            FormBorderStyle = FormBorderStyle.Sizable;
            StartPosition   = FormStartPosition.CenterParent;
            MaximizeBox     = true;
            MinimizeBox     = false;
            Font            = new Font("Segoe UI", 9.5f);

            BuildUI();
            SelectPatch(0);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void BuildUI()
        {
            // ── Header ───────────────────────────────────────────────────────
            var lblHeader = new Label
            {
                Text      = $"{_previews.Count} Solution(s)/Patch(es) will be created. " +
                             "Select a row to inspect its components, then click Create." +
                             "Hint!! Solution/Patch Name Column Editable.",
                Dock      = DockStyle.Top,
                Height    = 40,
                Padding   = new Padding(10, 10, 10, 0),
                Font      = new Font("Segoe UI", 9.5f),
                ForeColor = Color.FromArgb(40, 40, 40)
            };

            // ── Patch grid (top) ─────────────────────────────────────────────
            _patchGrid = MakeGrid();
            _patchGrid.ReadOnly = false; // Allow editing in this grid

            var colPatch  = new DataGridViewTextBoxColumn { Name = "colPatch",   HeaderText = "Solution/Patch Name",      FillWeight = 35 };
            var colParent = new DataGridViewTextBoxColumn { Name = "colParent",  HeaderText = "Parent Solution", FillWeight = 30, ReadOnly = true };
            var colVer    = new DataGridViewTextBoxColumn { Name = "colVer",     HeaderText = "New Version",     FillWeight = 15, ReadOnly = true };
            var colCount  = new DataGridViewTextBoxColumn { Name = "colCount",   HeaderText = "Components",      FillWeight = 10, ReadOnly = true };

            _patchGrid.Columns.Add(colPatch);
            _patchGrid.Columns.Add(colParent);
            _patchGrid.Columns.Add(colVer);
            _patchGrid.Columns.Add(colCount);

            foreach (var p in _previews)
                _patchGrid.Rows.Add(p.SolutionName, p.PatchParent ?? "(new)", p.Version, p.Components.Count);

            // Update underlying object when user edits the name
            _patchGrid.CellEndEdit += (s, e) =>
            {
                if (e.ColumnIndex == 0)
                {
                    var newName = _patchGrid.Rows[e.RowIndex].Cells[0].Value?.ToString() ?? string.Empty;
                    _previews[e.RowIndex].SolutionName = newName;
                }
            };

            _patchGrid.SelectionChanged += (_, __) =>
            {
                if (_patchGrid.SelectedRows.Count > 0)
                    SelectPatch(_patchGrid.SelectedRows[0].Index);
            };

            // ── Divider label ────────────────────────────────────────────────
            var lblComponents = new Label
            {
                Text      = "Components in selected solution/patch:",
                Height    = 24,
                Dock      = DockStyle.Top,
                Padding   = new Padding(10, 4, 0, 0),
                Font      = new Font("Segoe UI Semibold", 9f),
                ForeColor = Color.FromArgb(60, 60, 60),
                BackColor = Color.FromArgb(245, 247, 250)
            };

            // ── Component grid (bottom) ──────────────────────────────────────
            _compGrid = MakeGrid();
            _compGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colName",     HeaderText = "Component Name",   FillWeight = 40 });
            _compGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colType",     HeaderText = "Type",             FillWeight = 25 });
            _compGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colLifecycle",HeaderText = "Lifecycle",        FillWeight = 20 });
            _compGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "colRisk",     HeaderText = "Risk",             FillWeight = 15 });

            // ── Bottom button panel ──────────────────────────────────────────
            var pnlBottom = new Panel
            {
                Dock    = DockStyle.Bottom,
                Height  = 52,
                Padding = new Padding(10, 8, 10, 8)
            };

            var btnCancel = new Button
            {
                Text         = "Cancel",
                Width        = 90,
                Height       = 32,
                DialogResult = DialogResult.Cancel,
                FlatStyle    = FlatStyle.Flat,
                Anchor       = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCancel.Left = pnlBottom.ClientSize.Width - btnCancel.Width - 10;
            btnCancel.Top  = 10;
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(180, 180, 180);

            var btnCreate = new Button
            {
                Text         = "✔  Create",
                Width        = 120,
                Height       = 32,
                DialogResult = DialogResult.OK,
                FlatStyle    = FlatStyle.Flat,
                BackColor    = Color.FromArgb(16, 137, 62),
                ForeColor    = Color.White,
                Font         = new Font("Segoe UI Semibold", 9.5f),
                Anchor       = AnchorStyles.Right | AnchorStyles.Top
            };
            btnCreate.Left = pnlBottom.ClientSize.Width - btnCancel.Width - btnCreate.Width - 18;
            btnCreate.Top  = 10;
            btnCreate.FlatAppearance.BorderSize = 0;

            pnlBottom.Controls.Add(btnCancel);
            pnlBottom.Controls.Add(btnCreate);

            var sepLine = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = Color.FromArgb(210, 210, 210) };

            // ── SplitContainer: patch grid on top, component grid on bottom ──
            var split = new SplitContainer
            {
                Dock        = DockStyle.Fill,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 160,
                Panel1MinSize    = 80,
                Panel2MinSize    = 80
            };
            split.Panel1.Controls.Add(_patchGrid);
            split.Panel2.Controls.Add(_compGrid);
            split.Panel2.Controls.Add(lblComponents);

            AcceptButton = btnCreate;
            CancelButton = btnCancel;

            Controls.Add(split);
            Controls.Add(sepLine);
            Controls.Add(pnlBottom);
            Controls.Add(lblHeader);
        }

        // ─────────────────────────────────────────────────────────────────────
        private void SelectPatch(int index)
        {
            if (index < 0 || index >= _previews.Count) return;

            _compGrid.Rows.Clear();
            var preview = _previews[index];

            foreach (var c in preview.Components)
            {
                int rowIdx = _compGrid.Rows.Add(
                    c.Name,
                    !string.IsNullOrEmpty(c.ComponentTypeName)
                        ? c.ComponentTypeName
                        : ComponentNameResolver.TypeLabel(c.ComponentType),
                    LifecycleLabel(c.Lifecycle),
                    c.Risk.ToString());

                _compGrid.Rows[rowIdx].DefaultCellStyle.BackColor = c.Risk switch
                {
                    RiskLevel.High   => Color.FromArgb(255, 220, 220),
                    RiskLevel.Medium => Color.FromArgb(255, 250, 210),
                    _                => Color.White
                };
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        private static DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock                  = DockStyle.Fill,
                ReadOnly              = true,
                AllowUserToAddRows    = false,
                AllowUserToDeleteRows = false,
                SelectionMode         = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect           = false,
                AutoSizeColumnsMode   = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible     = false,
                BorderStyle           = BorderStyle.None,
                BackgroundColor       = Color.White,
                GridColor             = Color.FromArgb(220, 220, 220),
                Font                  = new Font("Segoe UI", 9f),
                ColumnHeadersHeight   = 28,
                EnableHeadersVisualStyles = false
            };
            g.ColumnHeadersDefaultCellStyle.Font      = new Font("Segoe UI Semibold", 9f);
            g.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 242, 245);
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(50, 50, 50);
            return g;
        }

        private static string LifecycleLabel(ComponentLifecycle lc) => lc switch
        {
            ComponentLifecycle.New             => "New",
            ComponentLifecycle.ExistingUpdated => "Updated",
            ComponentLifecycle.Unchanged       => "Unchanged",
            ComponentLifecycle.Deleted         => "Deleted",
            _                                  => "Unknown"
        };
    }
}
