using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SolutionDeploymentAdvisor.UI
{
    public class AssignSolutionDialog : Form
    {
        public string SelectedSolutionName { get; private set; } = string.Empty;
        public bool ApplySplitStrategy { get; private set; } = true;

        private ComboBox _cmbSolutions;
        private CheckBox _chkApplySplit;

        public AssignSolutionDialog(List<string> existingSolutions, string defaultSolution, bool defaultSplit)
        {
            Text = "Assign Target Solution/Patch";
            Width = 450;
            Height = 220;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            Font = new Font("Segoe UI", 9.5f);

            var lblPrompt = new Label
            {
                Text = "Select an existing solution/patch, or type a new name:",
                Left = 20,
                Top = 20,
                Width = 400,
                AutoSize = true
            };

            _cmbSolutions = new ComboBox
            {
                Left = 20,
                Top = 45,
                Width = 390,
                DropDownStyle = ComboBoxStyle.DropDown
            };
            
            foreach (var sol in existingSolutions)
            {
                if (!string.IsNullOrWhiteSpace(sol))
                    _cmbSolutions.Items.Add(sol);
            }
            _cmbSolutions.Text = defaultSolution;

            //_chkApplySplit = new CheckBox
            //{
            //    Text = "Apply current Split Strategy to this assignment",
            //    Left = 20,
            //    Top = 85,
            //    Width = 390,
            //    Checked = defaultSplit
            //};

            var btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Left = 230,
                Top = 130,
                Width = 85,
                Height = 30
            };

            var btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Left = 325,
                Top = 130,
                Width = 85,
                Height = 30
            };

            Controls.Add(lblPrompt);
            Controls.Add(_cmbSolutions);
            //Controls.Add(_chkApplySplit);
            Controls.Add(btnOk);
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;

            FormClosing += (s, e) =>
            {
                if (DialogResult == DialogResult.OK)
                {
                    if (string.IsNullOrWhiteSpace(_cmbSolutions.Text))
                    {
                        MessageBox.Show("Please enter a valid solution name.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        e.Cancel = true;
                    }
                    else
                    {
                        SelectedSolutionName = _cmbSolutions.Text.Trim();
                        ApplySplitStrategy = false;
                    }
                }
            };
        }
    }
}
