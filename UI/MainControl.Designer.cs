using System.Drawing;
using System.Windows.Forms;

namespace SolutionDeploymentAdvisor.UI
{
    partial class MainControl
    {
        private System.ComponentModel.IContainer components = null;

        // ── Panels ─────────────────────────────────────────────────────────
        private Panel pnlTop;
        private Panel pnlSummary;

        private Label    lblSourceEnv;
        private Label    lblSourceEnvValue;
        private Button   btnSelectSource;
        private Label    lblSourceSol;
        private ComboBox cmbSourceSolutions;
        private Button   btnLoadSource;
        private Label    lblSourceVersion;

        // ── Row 2: Target ──────────────────────────────────────────────────
        private Label    lblTargetEnv;
        private Label    lblTargetEnvValue;
        private Button   btnSelectTarget;
        private Label    lblTargetSol;
        private ComboBox cmbTargetSolutions;
        private Button   btnLoadTarget;

        // ── Row 3: Publisher ───────────────────────────────────────────────
        private Label    lblPublisher;
        private ComboBox cmbPublishers;
        private Button   btnLoadPublishers;

        private Label    lblSplitStrategy;
        private ComboBox cmbSplitStrategy;

        // ── Row 4: Actions ─────────────────────────────────────────────────
        private Button   btnAnalyze;
        private Button   btnCreate;
        private Button   btnExportCsv;
        private Button   btnExportPac;
        private Label    lblVersionPreview;
        private LinkLabel lnkAuthorInfo;

        // ── Summary bar & Grid ───────────────────────────────────────────
        private Label    lblHighCount;
        private Label    lblMedCount;
        private Label    lblLowCount;
        private Label    lblTotalCount;
        private Label    lblFilterLabel;
        private ComboBox cmbFilter;
        private DataGridView grid;

        private void InitializeComponent()
        {
            pnlTop = new Panel { Dock = DockStyle.Top, Height = 186, Padding = new Padding(8, 6, 8, 4) };
            pnlSummary = new Panel { Dock = DockStyle.Top, Height = 30, BackColor = Color.WhiteSmoke };

            int col1 = 4, col2 = 110, col3 = 430, col4 = 510, col5 = 730;
            int rowH = 30;

            // ── ROW 1: Source ──────────────────────────────────────────────
            int y = 6;
            lblSourceEnv = new Label
            { Text = "Source:", Left = col1, Top = y + 3, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            lblSourceEnvValue = new Label
            {
                Text = "Not connected",
                Left = col2 + 150,
                Top = y + 5,
                Width = 165,
                AutoEllipsis = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f)
            };

            btnSelectSource = new Button
            {
                Text = "⚡ Connect Source",
                Left = col2 + 4,
                Top = y,
                Width = 142,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(16, 137, 62),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectSource.FlatAppearance.BorderSize = 0;
            btnSelectSource.FlatAppearance.MouseOverBackColor = Color.FromArgb(13, 110, 50);
            btnSelectSource.FlatAppearance.MouseDownBackColor = Color.FromArgb(10, 85, 40);
            btnSelectSource.Click += btnSelectSource_Click;

            lblSourceSol = new Label
            { Text = "Solution:", Left = col2 + 150 + 170, Top = y + 3, Width = 90, TextAlign = ContentAlignment.MiddleRight };
            cmbSourceSolutions = new ComboBox
            {
                Left = col2 + 150 + 260,
                Top = y,
                Width = 400,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };
            btnLoadSource = new Button
            { Text = "Load", Left = col2 + 150 + 464 + 200, Top = y + 1, Width = 52, Height = 24 };
            btnLoadSource.Click += btnLoadSource_Click;

            lblSourceVersion = new Label
            {
                Text = "",
                Left = col2 + 150 + 520 + 200,
                Top = y + 3,
                Width = 160,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f)
            };

            // ── ROW 2: Target ──────────────────────────────────────────────
            y += rowH + 4;
            lblTargetEnv = new Label
            { Text = "Target:", Left = col1, Top = y + 3, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            lblTargetEnvValue = new Label
            {
                Text = "Not connected",
                Left = col2 + 150,
                Top = y + 5,
                Width = 165,
                AutoEllipsis = true,
                ForeColor = Color.Gray,
                Font = new Font("Segoe UI", 8.5f)
            };

            btnSelectTarget = new Button
            {
                Text = "⚡ Connect Target",
                Left = col2 + 4,
                Top = y,
                Width = 142,
                Height = 26,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0, 120, 212),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnSelectTarget.FlatAppearance.BorderSize = 0;
            btnSelectTarget.FlatAppearance.MouseOverBackColor = Color.FromArgb(16, 110, 190);
            btnSelectTarget.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 90, 170);
            btnSelectTarget.Click += btnSelectTarget_Click;

            //lblTargetSol = new Label
            //{ Text = "Solution:", Left = col2 + 150 + 170, Top = y + 3, Width = 90, TextAlign = ContentAlignment.MiddleRight };
            //cmbTargetSolutions = new ComboBox
            //{
            //    Left = col2 + 150 + 260,
            //    Top = y,
            //    Width = 200,
            //    DropDownStyle = ComboBoxStyle.DropDown,
            //    AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            //    AutoCompleteSource = AutoCompleteSource.ListItems,
            //    Enabled = false,
            //    Text = "(connect target first)"
            //};
            //btnLoadTarget = new Button
            //{ Text = "Load", Left = col2 + 150 + 464, Top = y + 1, Width = 52, Height = 24, Enabled = false };
            //btnLoadTarget.Click += btnLoadTarget_Click;

            // ── ROW 3: Publisher ───────────────────────────────────────────
            /* y += rowH + 4;
            lblPublisher = new Label
                { Text = "Publisher:", Left = col1, Top = y+3, Width = 100, TextAlign = ContentAlignment.MiddleRight };
            cmbPublishers = new ComboBox
                { Left = col2, Top = y, Width = 260, DropDownStyle = ComboBoxStyle.DropDown,
                  AutoCompleteMode = AutoCompleteMode.SuggestAppend, AutoCompleteSource = AutoCompleteSource.ListItems,
                  Enabled = false, Text = "(connect target first)" };
            btnLoadPublishers = new Button
                { Text = "Load", Left = col2+264, Top = y+1, Width = 52, Height = 24, Enabled = false };
            btnLoadPublishers.Click += btnLoadPublishers_Click;*/

            // ── ROW 3.5: Split Strategy ────────────────────────────────────
            y += rowH + 4;
            lblSplitStrategy = new Label
            { Text = "Split Strategy (New Components):", Left = col1, Top = y + 3, Width = 180, TextAlign = ContentAlignment.MiddleRight };
            cmbSplitStrategy = new ComboBox
            { Left = 186, Top = y, Width = 184, DropDownStyle = ComboBoxStyle.DropDownList };

            // ── ROW 4: Actions ──────────────────────────────────────────────────────────────────
            y += rowH + 8;
            btnAnalyze = new Button
            { Text = "Analyze", Left = col1, Top = y, Width = 90, Height = 28 };
            btnAnalyze.Click += btnAnalyze_Click;

            btnCreate = new Button
            { Text = "Create Solution(s)/Patch(es)", Left = 98, Top = y, Width = 200, Height = 28, Enabled = false };
            btnCreate.Click += btnCreate_Click;

            btnExportCsv = new Button
            { Text = "Export CSV", Left = 304, Top = y, Width = 90, Height = 28, Enabled = false };
            btnExportCsv.Click += btnExportCsv_Click;

            // btnExportPac = new Button
            // { Text = "Export PAC CLI", Left = 330, Top = y, Width = 110, Height = 28, Enabled = false };
            // btnExportPac.Click += btnExportPac_Click;

            lblVersionPreview = new Label
            {
                Text = "",
                Left = 448,
                Top = y + 5,
                Width = 400,
                Height = 18,
                ForeColor = Color.DimGray,
                Font = new Font("Segoe UI", 8.25f)
            };

            lnkAuthorInfo = new LinkLabel
            {
                Text = "By: Osama Mahmoud Rashed (Software Engineer) | osamamahmoudrashed@gmail.com",
                Left = 900,
                Top = 3,
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Italic),

            };
            

            // ── Add all to pnlTop ──────────────────────────────────────────
            pnlTop.Controls.AddRange(new Control[]
            {
        lblSourceEnv, lblSourceEnvValue, btnSelectSource,
        lblSourceSol, cmbSourceSolutions, btnLoadSource, lblSourceVersion,
        lblTargetEnv, lblTargetEnvValue, btnSelectTarget,
        lblTargetSol, cmbTargetSolutions, btnLoadTarget,
        lblPublisher, cmbPublishers, btnLoadPublishers,
        lblSplitStrategy, cmbSplitStrategy,
        btnAnalyze, btnCreate, btnExportCsv,
                //btnExportPac, 
                lblVersionPreview
            });

            // ── Summary bar ────────────────────────────────────────────────
            lblHighCount = new Label { Text = "● High: 0", Left = 8, Top = 7, Width = 80, ForeColor = Color.Crimson, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            lblMedCount = new Label { Text = "● Medium: 0", Left = 92, Top = 7, Width = 90, ForeColor = Color.DarkGoldenrod, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            lblLowCount = new Label { Text = "● Low: 0", Left = 186, Top = 7, Width = 70, ForeColor = Color.DarkGreen, Font = new Font("Segoe UI", 8.5f, FontStyle.Bold) };
            lblTotalCount = new Label { Text = "Total: 0", Left = 260, Top = 7, Width = 70, ForeColor = Color.Gray };

            lblFilterLabel = new Label { Text = "Filter:", Left = 348, Top = 7, Width = 46 };
            cmbFilter = new ComboBox
            { Left = 398, Top = 3, Width = 140, DropDownStyle = ComboBoxStyle.DropDownList, Height = 22 };
            cmbFilter.Items.AddRange(new object[]
                { "All", "High", "Medium", "Low", "New", "ExistingUpdated", "Unknown" });
            cmbFilter.SelectedIndex = 0;
            cmbFilter.SelectedIndexChanged += cmbFilter_SelectedIndexChanged;

            pnlSummary.Controls.AddRange(new Control[]
                { lblHighCount, lblMedCount, lblLowCount, lblTotalCount, lblFilterLabel, cmbFilter,lnkAuthorInfo });

            // ── Grid ───────────────────────────────────────────────────────
            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                ReadOnly = true,
                AllowUserToAddRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BorderStyle = BorderStyle.None,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize
            };
            grid.ContextMenuStrip = new ContextMenuStrip();
            grid.ContextMenuStrip.Items.Add("Assign to Solution/Patch...", null, (s, e) => AssignToSolution_Click());

            grid.Columns.AddRange(
                new DataGridViewTextBoxColumn { Name = "colName", HeaderText = "Component", Width = 200 },
                new DataGridViewTextBoxColumn { Name = "colType", HeaderText = "Type", Width = 130 },
                new DataGridViewTextBoxColumn { Name = "colLifecycle", HeaderText = "Lifecycle", Width = 100 },
                new DataGridViewTextBoxColumn { Name = "colCategory", HeaderText = "Category", Width = 90 },
                new DataGridViewTextBoxColumn { Name = "colRisk", HeaderText = "Risk", Width = 70 },
                new DataGridViewTextBoxColumn { Name = "colSourceVersion", HeaderText = "Source Version/Patches", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "colTargetVersion", HeaderText = "Target Version/Patches", Width = 150 },
                new DataGridViewTextBoxColumn { Name = "colTargetAssigned", HeaderText = "Target Solution (Assigned)", Width = 170 },
                new DataGridViewTextBoxColumn { Name = "colMissingPatches", HeaderText = "Missing Patches/Updates", Width = 150 },
                new DataGridViewTextBoxColumn
                {
                    Name = "colNote",
                    HeaderText = "Note",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
                }
            );

            Controls.Add(grid);
            Controls.Add(pnlSummary);
            Controls.Add(pnlTop);
        }

    }
}
