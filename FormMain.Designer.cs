namespace WindowPositioner
{
    partial class FormMain
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.NotifyIcon notifyIconMain;
        private System.Windows.Forms.ContextMenuStrip contextMenuStripTray;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageProfiles;
        private System.Windows.Forms.TabPage tabPageSettings;
        private System.Windows.Forms.GroupBox groupBoxProfiles;
        private System.Windows.Forms.Button buttonRemoveProfile;
        private System.Windows.Forms.Button buttonAddProfile;
        private System.Windows.Forms.ListBox listBoxProfiles;
        private System.Windows.Forms.GroupBox groupBoxWindowConfigs;
        private System.Windows.Forms.DataGridView dataGridViewWindowConfigs;
        private System.Windows.Forms.Button buttonRemoveWindowConfig;
        private System.Windows.Forms.Button buttonAddWindowConfig;
        private System.Windows.Forms.Button buttonSaveChanges;
        private System.Windows.Forms.Label labelActiveProfile;
        private System.Windows.Forms.ComboBox comboBoxActiveProfile;
        private System.Windows.Forms.Button buttonRenameProfile;
        private System.Windows.Forms.ToolTip toolTipGeneral;
        private System.Windows.Forms.Button buttonFetchPosition;
        private System.Windows.Forms.Button buttonFetchSize;
        private System.Windows.Forms.Button buttonCloneProfile;
        private System.Windows.Forms.Button buttonTestSelectedProfile;
        private System.Windows.Forms.Label labelStartupOptions;
        private System.Windows.Forms.ComboBox comboBoxStartupOptions;
        private System.Windows.Forms.LinkLabel linkLabelGitHub;
        private System.Windows.Forms.Label labelCredits;

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
            notifyIconMain = new NotifyIcon(components);
            contextMenuStripTray = new ContextMenuStrip(components);
            settingsToolStripMenuItem = new ToolStripMenuItem();
            exitToolStripMenuItem = new ToolStripMenuItem();
            tabControlMain = new TabControl();
            tabPageProfiles = new TabPage();
            groupBoxWindowConfigs = new GroupBox();
            buttonFetchSize = new Button();
            buttonFetchPosition = new Button();
            dataGridViewWindowConfigs = new DataGridView();
            buttonRemoveWindowConfig = new Button();
            buttonAddWindowConfig = new Button();
            buttonTestSelectedProfile = new Button();
            groupBoxProfiles = new GroupBox();
            buttonRenameProfile = new Button();
            labelActiveProfile = new Label();
            comboBoxActiveProfile = new ComboBox();
            buttonRemoveProfile = new Button();
            buttonAddProfile = new Button();
            listBoxProfiles = new ListBox();
            buttonCloneProfile = new Button();
            tabPageSettings = new TabPage();
            linkLabelGitHub = new LinkLabel();
            labelCredits = new Label();
            comboBoxStartupOptions = new ComboBox();
            labelStartupOptions = new Label();
            buttonSaveChanges = new Button();
            toolTipGeneral = new ToolTip(components);
            contextMenuStripTray.SuspendLayout();
            tabControlMain.SuspendLayout();
            tabPageProfiles.SuspendLayout();
            groupBoxWindowConfigs.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewWindowConfigs).BeginInit();
            groupBoxProfiles.SuspendLayout();
            tabPageSettings.SuspendLayout();
            SuspendLayout();
            // 
            // notifyIconMain
            // 
            notifyIconMain.ContextMenuStrip = contextMenuStripTray;
            notifyIconMain.Text = "Window Positioner";
            notifyIconMain.Visible = true;
            notifyIconMain.DoubleClick += notifyIconMain_DoubleClick;
            // 
            // contextMenuStripTray
            // 
            contextMenuStripTray.Items.AddRange(new ToolStripItem[] { settingsToolStripMenuItem, exitToolStripMenuItem });
            contextMenuStripTray.Name = "contextMenuStripTray";
            contextMenuStripTray.Size = new Size(117, 48);
            // 
            // settingsToolStripMenuItem
            // 
            settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            settingsToolStripMenuItem.Size = new Size(116, 22);
            settingsToolStripMenuItem.Text = "&Settings";
            settingsToolStripMenuItem.Click += settingsToolStripMenuItem_Click;
            // 
            // exitToolStripMenuItem
            // 
            exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            exitToolStripMenuItem.Size = new Size(116, 22);
            exitToolStripMenuItem.Text = "&Exit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            // 
            // tabControlMain
            // 
            tabControlMain.Controls.Add(tabPageProfiles);
            tabControlMain.Controls.Add(tabPageSettings);
            tabControlMain.Dock = DockStyle.Fill;
            tabControlMain.Location = new Point(0, 0);
            tabControlMain.Name = "tabControlMain";
            tabControlMain.SelectedIndex = 0;
            tabControlMain.Size = new Size(784, 521);
            tabControlMain.TabIndex = 0;
            // 
            // tabPageProfiles
            // 
            tabPageProfiles.Controls.Add(groupBoxWindowConfigs);
            tabPageProfiles.Controls.Add(groupBoxProfiles);
            tabPageProfiles.Location = new Point(4, 24);
            tabPageProfiles.Name = "tabPageProfiles";
            tabPageProfiles.Padding = new Padding(3);
            tabPageProfiles.Size = new Size(776, 493);
            tabPageProfiles.TabIndex = 0;
            tabPageProfiles.Text = "Profiles & Windows";
            tabPageProfiles.UseVisualStyleBackColor = true;
            // 
            // groupBoxWindowConfigs
            // 
            groupBoxWindowConfigs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxWindowConfigs.Controls.Add(buttonFetchSize);
            groupBoxWindowConfigs.Controls.Add(buttonFetchPosition);
            groupBoxWindowConfigs.Controls.Add(dataGridViewWindowConfigs);
            groupBoxWindowConfigs.Controls.Add(buttonRemoveWindowConfig);
            groupBoxWindowConfigs.Controls.Add(buttonAddWindowConfig);
            groupBoxWindowConfigs.Controls.Add(buttonTestSelectedProfile);
            groupBoxWindowConfigs.Location = new Point(200, 7);
            groupBoxWindowConfigs.Name = "groupBoxWindowConfigs";
            groupBoxWindowConfigs.Size = new Size(570, 480);
            groupBoxWindowConfigs.TabIndex = 1;
            groupBoxWindowConfigs.TabStop = false;
            groupBoxWindowConfigs.Text = "Window Configurations for Selected Profile";
            // 
            // buttonFetchSize
            // 
            buttonFetchSize.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFetchSize.Enabled = false;
            buttonFetchSize.Location = new Point(464, 445);
            buttonFetchSize.Name = "buttonFetchSize";
            buttonFetchSize.Size = new Size(100, 25);
            buttonFetchSize.TabIndex = 4;
            buttonFetchSize.Text = "Fetch Size";
            toolTipGeneral.SetToolTip(buttonFetchSize, "Update Width,Height from the live window matching this configuration");
            buttonFetchSize.UseVisualStyleBackColor = true;
            buttonFetchSize.Click += buttonFetchSize_Click;
            // 
            // buttonFetchPosition
            // 
            buttonFetchPosition.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFetchPosition.Enabled = false;
            buttonFetchPosition.Location = new Point(358, 445);
            buttonFetchPosition.Name = "buttonFetchPosition";
            buttonFetchPosition.Size = new Size(100, 25);
            buttonFetchPosition.TabIndex = 3;
            buttonFetchPosition.Text = "Fetch Position";
            toolTipGeneral.SetToolTip(buttonFetchPosition, "Update X,Y from the live window matching this configuration");
            buttonFetchPosition.UseVisualStyleBackColor = true;
            buttonFetchPosition.Click += buttonFetchPosition_Click;
            // 
            // dataGridViewWindowConfigs
            // 
            dataGridViewWindowConfigs.AllowUserToAddRows = false;
            dataGridViewWindowConfigs.AllowUserToDeleteRows = false;
            dataGridViewWindowConfigs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewWindowConfigs.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridViewWindowConfigs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewWindowConfigs.Location = new Point(7, 23);
            dataGridViewWindowConfigs.MultiSelect = false;
            dataGridViewWindowConfigs.Name = "dataGridViewWindowConfigs";
            dataGridViewWindowConfigs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewWindowConfigs.Size = new Size(557, 412);
            dataGridViewWindowConfigs.TabIndex = 0;
            dataGridViewWindowConfigs.CellEndEdit += dataGridViewWindowConfigs_CellEndEdit;
            dataGridViewWindowConfigs.SelectionChanged += dataGridViewWindowConfigs_SelectionChanged;
            // 
            // buttonRemoveWindowConfig
            // 
            buttonRemoveWindowConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveWindowConfig.Enabled = false;
            buttonRemoveWindowConfig.Location = new Point(88, 445);
            buttonRemoveWindowConfig.Name = "buttonRemoveWindowConfig";
            buttonRemoveWindowConfig.Size = new Size(75, 25);
            buttonRemoveWindowConfig.TabIndex = 2;
            buttonRemoveWindowConfig.Text = "Remove";
            buttonRemoveWindowConfig.UseVisualStyleBackColor = true;
            buttonRemoveWindowConfig.Click += buttonRemoveWindowConfig_Click;
            // 
            // buttonAddWindowConfig
            // 
            buttonAddWindowConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddWindowConfig.Location = new Point(7, 445);
            buttonAddWindowConfig.Name = "buttonAddWindowConfig";
            buttonAddWindowConfig.Size = new Size(75, 25);
            buttonAddWindowConfig.TabIndex = 1;
            buttonAddWindowConfig.Text = "Add New";
            toolTipGeneral.SetToolTip(buttonAddWindowConfig, "Add a new window configuration by selecting a running process");
            buttonAddWindowConfig.UseVisualStyleBackColor = true;
            buttonAddWindowConfig.Click += buttonAddWindowConfig_Click;
            // 
            // buttonTestSelectedProfile
            // 
            buttonTestSelectedProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonTestSelectedProfile.Enabled = false;
            buttonTestSelectedProfile.Location = new Point(200, 445);
            buttonTestSelectedProfile.Name = "buttonTestSelectedProfile";
            buttonTestSelectedProfile.Size = new Size(150, 25);
            buttonTestSelectedProfile.TabIndex = 5;
            buttonTestSelectedProfile.Text = "Test This Profile's Layout";
            toolTipGeneral.SetToolTip(buttonTestSelectedProfile, "Apply window positions/sizes for the currently selected profile in the list above.");
            buttonTestSelectedProfile.UseVisualStyleBackColor = true;
            buttonTestSelectedProfile.Click += buttonTestSelectedProfile_Click;
            // 
            // groupBoxProfiles
            // 
            groupBoxProfiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxProfiles.Controls.Add(buttonRenameProfile);
            groupBoxProfiles.Controls.Add(labelActiveProfile);
            groupBoxProfiles.Controls.Add(comboBoxActiveProfile);
            groupBoxProfiles.Controls.Add(buttonRemoveProfile);
            groupBoxProfiles.Controls.Add(buttonAddProfile);
            groupBoxProfiles.Controls.Add(listBoxProfiles);
            groupBoxProfiles.Controls.Add(buttonCloneProfile);
            groupBoxProfiles.Location = new Point(7, 7);
            groupBoxProfiles.Name = "groupBoxProfiles";
            groupBoxProfiles.Size = new Size(187, 480);
            groupBoxProfiles.TabIndex = 0;
            groupBoxProfiles.TabStop = false;
            groupBoxProfiles.Text = "Profile Editor";
            // 
            // buttonRenameProfile
            // 
            buttonRenameProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRenameProfile.Location = new Point(7, 414);
            buttonRenameProfile.Name = "buttonRenameProfile";
            buttonRenameProfile.Size = new Size(174, 25);
            buttonRenameProfile.TabIndex = 5;
            buttonRenameProfile.Text = "Rename Selected";
            buttonRenameProfile.UseVisualStyleBackColor = true;
            buttonRenameProfile.Click += buttonRenameProfile_Click;
            // 
            // labelActiveProfile
            // 
            labelActiveProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelActiveProfile.AutoSize = true;
            labelActiveProfile.Location = new Point(9, 367);
            labelActiveProfile.Name = "labelActiveProfile";
            labelActiveProfile.Size = new Size(80, 15);
            labelActiveProfile.TabIndex = 4;
            labelActiveProfile.Text = "Active Profile:";
            // 
            // comboBoxActiveProfile
            // 
            comboBoxActiveProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            comboBoxActiveProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxActiveProfile.FormattingEnabled = true;
            comboBoxActiveProfile.Location = new Point(7, 385);
            comboBoxActiveProfile.Name = "comboBoxActiveProfile";
            comboBoxActiveProfile.Size = new Size(174, 23);
            comboBoxActiveProfile.TabIndex = 3;
            comboBoxActiveProfile.SelectedIndexChanged += comboBoxActiveProfile_SelectedIndexChanged;
            // 
            // buttonRemoveProfile
            // 
            buttonRemoveProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveProfile.Location = new Point(98, 445);
            buttonRemoveProfile.Name = "buttonRemoveProfile";
            buttonRemoveProfile.Size = new Size(83, 25);
            buttonRemoveProfile.TabIndex = 2;
            buttonRemoveProfile.Text = "Remove";
            buttonRemoveProfile.UseVisualStyleBackColor = true;
            buttonRemoveProfile.Click += buttonRemoveProfile_Click;
            // 
            // buttonAddProfile
            // 
            buttonAddProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddProfile.Location = new Point(7, 445);
            buttonAddProfile.Name = "buttonAddProfile";
            buttonAddProfile.Size = new Size(83, 25);
            buttonAddProfile.TabIndex = 1;
            buttonAddProfile.Text = "Add New";
            buttonAddProfile.UseVisualStyleBackColor = true;
            buttonAddProfile.Click += buttonAddProfile_Click;
            // 
            // listBoxProfiles
            // 
            listBoxProfiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxProfiles.FormattingEnabled = true;
            listBoxProfiles.ItemHeight = 15;
            listBoxProfiles.Location = new Point(7, 23);
            listBoxProfiles.Name = "listBoxProfiles";
            listBoxProfiles.Size = new Size(174, 229);
            listBoxProfiles.TabIndex = 0;
            listBoxProfiles.SelectedIndexChanged += listBoxProfiles_SelectedIndexChanged;
            // 
            // buttonCloneProfile
            // 
            buttonCloneProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonCloneProfile.Location = new Point(7, 258);
            buttonCloneProfile.Name = "buttonCloneProfile";
            buttonCloneProfile.Size = new Size(174, 25);
            buttonCloneProfile.TabIndex = 6;
            buttonCloneProfile.Text = "Clone Selected Profile";
            buttonCloneProfile.UseVisualStyleBackColor = true;
            buttonCloneProfile.Click += buttonCloneProfile_Click;
            // 
            // tabPageSettings
            // 
            tabPageSettings.Controls.Add(linkLabelGitHub);
            tabPageSettings.Controls.Add(labelCredits);
            tabPageSettings.Controls.Add(comboBoxStartupOptions);
            tabPageSettings.Controls.Add(labelStartupOptions);
            tabPageSettings.Location = new Point(4, 24);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(3);
            tabPageSettings.Size = new Size(776, 493);
            tabPageSettings.TabIndex = 1;
            tabPageSettings.Text = "Application Settings";
            tabPageSettings.UseVisualStyleBackColor = true;

            // 
            // linkLabelGitHub
            // 
            linkLabelGitHub.AutoSize = true;
            linkLabelGitHub.Location = new Point(15, 80);
            linkLabelGitHub.Name = "linkLabelGitHub";
            linkLabelGitHub.Size = new Size(201, 15);
            linkLabelGitHub.TabIndex = 3;
            linkLabelGitHub.TabStop = true;
            linkLabelGitHub.Text = "Visit our GitHub page (Example Link)";
            linkLabelGitHub.LinkClicked += linkLabelGitHub_LinkClicked;
            // 
            // labelCredits
            // 
            labelCredits.AutoSize = true;
            labelCredits.Location = new Point(15, 60);
            labelCredits.Name = "labelCredits";
            labelCredits.Size = new Size(174, 15);
            labelCredits.TabIndex = 2;
            labelCredits.Text = "Found an issue or have an idea?";
            // 
            // comboBoxStartupOptions
            // 
            comboBoxStartupOptions.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxStartupOptions.FormattingEnabled = true;
            comboBoxStartupOptions.Location = new Point(220, 17);
            comboBoxStartupOptions.Name = "comboBoxStartupOptions";
            comboBoxStartupOptions.Size = new Size(250, 23);
            comboBoxStartupOptions.TabIndex = 1;
            // 
            // labelStartupOptions
            // 
            labelStartupOptions.AutoSize = true;
            labelStartupOptions.Location = new Point(15, 20);
            labelStartupOptions.Name = "labelStartupOptions";
            labelStartupOptions.Size = new Size(93, 15);
            labelStartupOptions.TabIndex = 0;
            labelStartupOptions.Text = "Startup Options:";
            // 
            // buttonSaveChanges
            // 
            buttonSaveChanges.Dock = DockStyle.Bottom;
            buttonSaveChanges.Location = new Point(0, 521);
            buttonSaveChanges.Name = "buttonSaveChanges";
            buttonSaveChanges.Size = new Size(784, 40);
            buttonSaveChanges.TabIndex = 1;
            buttonSaveChanges.Text = "Save All Changes";
            buttonSaveChanges.UseVisualStyleBackColor = true;
            buttonSaveChanges.Click += buttonSaveChanges_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(tabControlMain);
            Controls.Add(buttonSaveChanges);
            MinimumSize = new Size(600, 400);
            Name = "FormMain";
            Text = "Window Positioner";
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            Shown += FormMain_Shown;
            contextMenuStripTray.ResumeLayout(false);
            tabControlMain.ResumeLayout(false);
            tabPageProfiles.ResumeLayout(false);
            groupBoxWindowConfigs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewWindowConfigs).EndInit();
            groupBoxProfiles.ResumeLayout(false);
            groupBoxProfiles.PerformLayout();
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            ResumeLayout(false);
        }
    }
}