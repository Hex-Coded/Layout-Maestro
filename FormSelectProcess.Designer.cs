namespace WindowPlacementManager
{
    partial class FormSelectProcess
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView listViewProcesses;
        private System.Windows.Forms.Button buttonSelect;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.ColumnHeader columnHeaderProcessName;
        private System.Windows.Forms.ColumnHeader columnHeaderPID;
        private System.Windows.Forms.ColumnHeader columnHeaderWindowTitle;
        private System.Windows.Forms.ColumnHeader columnHeaderAdminStatus;
        private System.Windows.Forms.Button buttonRefresh;
        private System.Windows.Forms.PictureBox pictureBoxAdminHint;
        private System.Windows.Forms.ToolTip toolTipAdminHint;

        protected override void Dispose(bool disposing)
        {
            if(disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            listViewProcesses = new ListView();
            columnHeaderProcessName = new ColumnHeader();
            columnHeaderPID = new ColumnHeader();
            columnHeaderWindowTitle = new ColumnHeader();
            columnHeaderAdminStatus = new ColumnHeader();
            buttonSelect = new Button();
            buttonCancel = new Button();
            buttonRefresh = new Button();
            pictureBoxAdminHint = new PictureBox();
            toolTipAdminHint = new ToolTip(components);
            ((System.ComponentModel.ISupportInitialize)pictureBoxAdminHint).BeginInit();
            SuspendLayout();
            // 
            // listViewProcesses
            // 
            listViewProcesses.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewProcesses.Columns.AddRange(new ColumnHeader[] { columnHeaderProcessName, columnHeaderPID, columnHeaderWindowTitle, columnHeaderAdminStatus });
            listViewProcesses.FullRowSelect = true;
            listViewProcesses.Location = new Point(12, 12);
            listViewProcesses.MultiSelect = false;
            listViewProcesses.Name = "listViewProcesses";
            listViewProcesses.Size = new Size(710, 307);
            listViewProcesses.TabIndex = 0;
            listViewProcesses.UseCompatibleStateImageBehavior = false;
            listViewProcesses.View = View.Details;
            listViewProcesses.SelectedIndexChanged += listViewProcesses_SelectedIndexChanged;
            listViewProcesses.DoubleClick += listViewProcesses_DoubleClick;
            // 
            // columnHeaderProcessName
            // 
            columnHeaderProcessName.Text = "Process Name";
            columnHeaderProcessName.Width = 150;
            // 
            // columnHeaderPID
            // 
            columnHeaderPID.Text = "PID";
            columnHeaderPID.Width = 70;
            // 
            // columnHeaderWindowTitle
            // 
            columnHeaderWindowTitle.Text = "Window Title";
            columnHeaderWindowTitle.Width = 300;
            // 
            // columnHeaderAdminStatus
            // 
            columnHeaderAdminStatus.Text = "Admin";
            columnHeaderAdminStatus.TextAlign = HorizontalAlignment.Center;
            // 
            // buttonSelect
            // 
            buttonSelect.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonSelect.Enabled = false;
            buttonSelect.Location = new Point(566, 325);
            buttonSelect.Name = "buttonSelect";
            buttonSelect.Size = new Size(75, 23);
            buttonSelect.TabIndex = 1;
            buttonSelect.Text = "Select";
            buttonSelect.UseVisualStyleBackColor = true;
            buttonSelect.Click += buttonSelect_Click;
            // 
            // buttonCancel
            // 
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.DialogResult = DialogResult.Cancel;
            buttonCancel.Location = new Point(647, 325);
            buttonCancel.Name = "buttonCancel";
            buttonCancel.Size = new Size(75, 23);
            buttonCancel.TabIndex = 2;
            buttonCancel.Text = "Cancel";
            buttonCancel.UseVisualStyleBackColor = true;
            // 
            // buttonRefresh
            // 
            buttonRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRefresh.Location = new Point(12, 325);
            buttonRefresh.Name = "buttonRefresh";
            buttonRefresh.Size = new Size(75, 23);
            buttonRefresh.TabIndex = 3;
            buttonRefresh.Text = "Refresh";
            buttonRefresh.UseVisualStyleBackColor = true;
            buttonRefresh.Click += buttonRefresh_Click;
            // 
            // pictureBoxAdminHint
            // 
            pictureBoxAdminHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            pictureBoxAdminHint.Location = new Point(93, 326);
            pictureBoxAdminHint.Name = "pictureBoxAdminHint";
            pictureBoxAdminHint.Size = new Size(20, 20);
            pictureBoxAdminHint.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxAdminHint.TabIndex = 4;
            pictureBoxAdminHint.TabStop = false;
            // 
            // toolTipAdminHint
            // 
            toolTipAdminHint.AutoPopDelay = 20000;
            toolTipAdminHint.InitialDelay = 300;
            toolTipAdminHint.ReshowDelay = 100;
            toolTipAdminHint.ToolTipIcon = ToolTipIcon.Warning;
            toolTipAdminHint.ToolTipTitle = "Administrator Privileges Note";
            // 
            // FormSelectProcess
            // 
            AcceptButton = buttonSelect;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = buttonCancel;
            ClientSize = new Size(734, 361);
            Controls.Add(pictureBoxAdminHint);
            Controls.Add(buttonRefresh);
            Controls.Add(buttonCancel);
            Controls.Add(buttonSelect);
            Controls.Add(listViewProcesses);
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = new Size(450, 300);
            Name = "FormSelectProcess";
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "Select Process Window";
            Load += FormSelectProcess_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBoxAdminHint).EndInit();
            ResumeLayout(false);
        }
    }
}