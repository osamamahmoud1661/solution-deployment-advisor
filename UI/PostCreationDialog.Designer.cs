namespace SolutionDeploymentAdvisor.UI
{
    partial class PostCreationDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.chkSolutions = new System.Windows.Forms.CheckedListBox();
            this.lblType = new System.Windows.Forms.Label();
            this.cmbExportType = new System.Windows.Forms.ComboBox();
            this.lblFolder = new System.Windows.Forms.Label();
            this.txtOutputFolder = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnOpenFolder = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.lblStatus = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.Location = new System.Drawing.Point(12, 13);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(273, 17);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "The following solutions have been created:";
            // 
            // chkSolutions
            // 
            this.chkSolutions.CheckOnClick = true;
            this.chkSolutions.FormattingEnabled = true;
            this.chkSolutions.Location = new System.Drawing.Point(15, 37);
            this.chkSolutions.Name = "chkSolutions";
            this.chkSolutions.Size = new System.Drawing.Size(437, 109);
            this.chkSolutions.TabIndex = 1;
            // 
            // lblType
            // 
            this.lblType.AutoSize = true;
            this.lblType.Location = new System.Drawing.Point(12, 161);
            this.lblType.Name = "lblType";
            this.lblType.Size = new System.Drawing.Size(77, 13);
            this.lblType.TabIndex = 2;
            this.lblType.Text = "Export As Type:";
            // 
            // cmbExportType
            // 
            this.cmbExportType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbExportType.FormattingEnabled = true;
            this.cmbExportType.Location = new System.Drawing.Point(95, 158);
            this.cmbExportType.Name = "cmbExportType";
            this.cmbExportType.Size = new System.Drawing.Size(121, 21);
            this.cmbExportType.TabIndex = 3;
            // 
            // lblFolder
            // 
            this.lblFolder.AutoSize = true;
            this.lblFolder.Location = new System.Drawing.Point(12, 192);
            this.lblFolder.Name = "lblFolder";
            this.lblFolder.Size = new System.Drawing.Size(74, 13);
            this.lblFolder.TabIndex = 4;
            this.lblFolder.Text = "Output Folder:";
            // 
            // txtOutputFolder
            // 
            this.txtOutputFolder.Location = new System.Drawing.Point(95, 189);
            this.txtOutputFolder.Name = "txtOutputFolder";
            this.txtOutputFolder.Size = new System.Drawing.Size(276, 20);
            this.txtOutputFolder.TabIndex = 5;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(377, 187);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 6;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnExport
            // 
            this.btnExport.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(120)))), ((int)(((byte)(212)))));
            this.btnExport.FlatAppearance.BorderSize = 0;
            this.btnExport.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnExport.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnExport.ForeColor = System.Drawing.Color.White;
            this.btnExport.Location = new System.Drawing.Point(15, 230);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(120, 30);
            this.btnExport.TabIndex = 7;
            this.btnExport.Text = "Export Selected";
            this.btnExport.UseVisualStyleBackColor = false;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnOpenFolder
            // 
            this.btnOpenFolder.Location = new System.Drawing.Point(145, 230);
            this.btnOpenFolder.Name = "btnOpenFolder";
            this.btnOpenFolder.Size = new System.Drawing.Size(95, 30);
            this.btnOpenFolder.TabIndex = 8;
            this.btnOpenFolder.Text = "Open Folder";
            this.btnOpenFolder.UseVisualStyleBackColor = true;
            this.btnOpenFolder.Visible = false;
            this.btnOpenFolder.Click += new System.EventHandler(this.btnOpenFolder_Click);
            // 
            // btnClose
            // 
            this.btnClose.Location = new System.Drawing.Point(377, 230);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 30);
            this.btnClose.TabIndex = 9;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 275);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(0, 13);
            this.lblStatus.TabIndex = 10;
            // 
            // PostCreationDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 300);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnOpenFolder);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtOutputFolder);
            this.Controls.Add(this.lblFolder);
            this.Controls.Add(this.cmbExportType);
            this.Controls.Add(this.lblType);
            this.Controls.Add(this.chkSolutions);
            this.Controls.Add(this.lblTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PostCreationDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Export Solutions";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.CheckedListBox chkSolutions;
        private System.Windows.Forms.Label lblType;
        private System.Windows.Forms.ComboBox cmbExportType;
        private System.Windows.Forms.Label lblFolder;
        private System.Windows.Forms.TextBox txtOutputFolder;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnOpenFolder;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Label lblStatus;
    }
}
