namespace WindowPlacementManager
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
        private System.Windows.Forms.CheckBox checkBoxDisableProgram;

        private System.Windows.Forms.Button buttonActivateLaunchApp;
        private System.Windows.Forms.Button buttonLaunchAllProfileApps;
        private System.Windows.Forms.Button buttonFocusAllProfileApps;


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
            checkBoxDisableProgram = new CheckBox();
            groupBoxWindowConfigs = new GroupBox();
            buttonLaunchAllProfileApps = new Button();
            buttonFocusAllProfileApps = new Button();
            buttonTestSelectedProfile = new Button();
            buttonAddWindowConfig = new Button();
            buttonRemoveWindowConfig = new Button();
            buttonActivateLaunchApp = new Button();
            buttonFetchPosition = new Button();
            buttonFetchSize = new Button();
            dataGridViewWindowConfigs = new DataGridView();
            groupBoxProfiles = new GroupBox();
            labelActiveProfile = new Label();
            comboBoxActiveProfile = new ComboBox();
            buttonAddProfile = new Button();
            buttonRenameProfile = new Button();
            buttonCloneProfile = new Button();
            buttonRemoveProfile = new Button();
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
            tabPageProfiles.Controls.Add(checkBoxDisableProgram);
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
            // checkBoxDisableProgram
            // 
            checkBoxDisableProgram.AutoSize = true;
            checkBoxDisableProgram.Location = new Point(608, 7);
            checkBoxDisableProgram.Name = "checkBoxDisableProgram";
            checkBoxDisableProgram.Size = new Size(156, 19);
            checkBoxDisableProgram.TabIndex = 2;
            checkBoxDisableProgram.Text = "Disable Program Activity";
            toolTipGeneral.SetToolTip(checkBoxDisableProgram, "If this checkbox is enabled, the program will not adjust any windows.");
            checkBoxDisableProgram.UseVisualStyleBackColor = true;
            checkBoxDisableProgram.CheckedChanged += checkBoxDisableProgram_CheckedChanged;
            // 
            // groupBoxWindowConfigs
            // 
            groupBoxWindowConfigs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxWindowConfigs.Controls.Add(buttonLaunchAllProfileApps);
            groupBoxWindowConfigs.Controls.Add(buttonFocusAllProfileApps);
            groupBoxWindowConfigs.Controls.Add(buttonTestSelectedProfile);
            groupBoxWindowConfigs.Controls.Add(buttonAddWindowConfig);
            groupBoxWindowConfigs.Controls.Add(buttonRemoveWindowConfig);
            groupBoxWindowConfigs.Controls.Add(buttonActivateLaunchApp);
            groupBoxWindowConfigs.Controls.Add(buttonFetchPosition);
            groupBoxWindowConfigs.Controls.Add(buttonFetchSize);
            groupBoxWindowConfigs.Controls.Add(dataGridViewWindowConfigs);
            groupBoxWindowConfigs.Location = new Point(200, 30);
            groupBoxWindowConfigs.Name = "groupBoxWindowConfigs";
            groupBoxWindowConfigs.Size = new Size(570, 457);
            groupBoxWindowConfigs.TabIndex = 1;
            groupBoxWindowConfigs.TabStop = false;
            groupBoxWindowConfigs.Text = "Window Configurations for Selected Profile";
            // 
            // buttonLaunchAllProfileApps
            // 
            buttonLaunchAllProfileApps.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonLaunchAllProfileApps.Enabled = false;
            buttonLaunchAllProfileApps.Location = new Point(250, 390);
            buttonLaunchAllProfileApps.Name = "buttonLaunchAllProfileApps";
            buttonLaunchAllProfileApps.Size = new Size(100, 25);
            buttonLaunchAllProfileApps.TabIndex = 1;
            buttonLaunchAllProfileApps.Text = "Launch All";
            toolTipGeneral.SetToolTip(buttonLaunchAllProfileApps, "Launch all configured apps in this profile that are not currently running.");
            buttonLaunchAllProfileApps.UseVisualStyleBackColor = true;
            buttonLaunchAllProfileApps.Click += buttonLaunchAllProfileApps_Click;
            // 
            // buttonFocusAllProfileApps
            // 
            buttonFocusAllProfileApps.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFocusAllProfileApps.Enabled = false;
            buttonFocusAllProfileApps.Location = new Point(357, 390);
            buttonFocusAllProfileApps.Name = "buttonFocusAllProfileApps";
            buttonFocusAllProfileApps.Size = new Size(100, 25);
            buttonFocusAllProfileApps.TabIndex = 2;
            buttonFocusAllProfileApps.Text = "Focus All";
            toolTipGeneral.SetToolTip(buttonFocusAllProfileApps, "Bring all running configured apps in this profile to the foreground.");
            buttonFocusAllProfileApps.UseVisualStyleBackColor = true;
            buttonFocusAllProfileApps.Click += buttonFocusAllProfileApps_Click;
            // 
            // buttonTestSelectedProfile
            // 
            buttonTestSelectedProfile.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonTestSelectedProfile.Enabled = false;
            buttonTestSelectedProfile.Location = new Point(464, 390);
            buttonTestSelectedProfile.Name = "buttonTestSelectedProfile";
            buttonTestSelectedProfile.Size = new Size(100, 25);
            buttonTestSelectedProfile.TabIndex = 3;
            buttonTestSelectedProfile.Text = "Test Layout";
            toolTipGeneral.SetToolTip(buttonTestSelectedProfile, "Apply window positions/sizes for the currently selected profile.");
            buttonTestSelectedProfile.UseVisualStyleBackColor = true;
            buttonTestSelectedProfile.Click += buttonTestSelectedProfile_Click;
            // 
            // buttonAddWindowConfig
            // 
            buttonAddWindowConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonAddWindowConfig.Location = new Point(7, 422);
            buttonAddWindowConfig.Name = "buttonAddWindowConfig";
            buttonAddWindowConfig.Size = new Size(115, 25);
            buttonAddWindowConfig.TabIndex = 4;
            buttonAddWindowConfig.Text = "Add Window";
            toolTipGeneral.SetToolTip(buttonAddWindowConfig, "Add a new window configuration by selecting a running process");
            buttonAddWindowConfig.UseVisualStyleBackColor = true;
            buttonAddWindowConfig.Click += buttonAddWindowConfig_Click;
            // 
            // buttonRemoveWindowConfig
            // 
            buttonRemoveWindowConfig.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonRemoveWindowConfig.Enabled = false;
            buttonRemoveWindowConfig.Location = new Point(129, 422);
            buttonRemoveWindowConfig.Name = "buttonRemoveWindowConfig";
            buttonRemoveWindowConfig.Size = new Size(115, 25);
            buttonRemoveWindowConfig.TabIndex = 5;
            buttonRemoveWindowConfig.Text = "Remove Window";
            buttonRemoveWindowConfig.UseVisualStyleBackColor = true;
            buttonRemoveWindowConfig.Click += buttonRemoveWindowConfig_Click;
            // 
            // buttonActivateLaunchApp
            // 
            buttonActivateLaunchApp.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonActivateLaunchApp.Enabled = false;
            buttonActivateLaunchApp.Location = new Point(251, 422);
            buttonActivateLaunchApp.Name = "buttonActivateLaunchApp";
            buttonActivateLaunchApp.Size = new Size(100, 25);
            buttonActivateLaunchApp.TabIndex = 6;
            buttonActivateLaunchApp.Text = "Activate Sel.";
            toolTipGeneral.SetToolTip(buttonActivateLaunchApp, "Launch the selected app if not running, or bring its window to the foreground.");
            buttonActivateLaunchApp.UseVisualStyleBackColor = true;
            buttonActivateLaunchApp.Click += buttonActivateLaunchApp_Click;
            // 
            // buttonFetchPosition
            // 
            buttonFetchPosition.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFetchPosition.Enabled = false;
            buttonFetchPosition.Location = new Point(357, 422);
            buttonFetchPosition.Name = "buttonFetchPosition";
            buttonFetchPosition.Size = new Size(100, 25);
            buttonFetchPosition.TabIndex = 7;
            buttonFetchPosition.Text = "Fetch Position";
            toolTipGeneral.SetToolTip(buttonFetchPosition, "Update X,Y from the live window matching this configuration");
            buttonFetchPosition.UseVisualStyleBackColor = true;
            buttonFetchPosition.Click += buttonFetchPosition_Click;
            // 
            // buttonFetchSize
            // 
            buttonFetchSize.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonFetchSize.Enabled = false;
            buttonFetchSize.Location = new Point(464, 422);
            buttonFetchSize.Name = "buttonFetchSize";
            buttonFetchSize.Size = new Size(100, 25);
            buttonFetchSize.TabIndex = 8;
            buttonFetchSize.Text = "Fetch Size";
            toolTipGeneral.SetToolTip(buttonFetchSize, "Update Width,Height from the live window matching this configuration");
            buttonFetchSize.UseVisualStyleBackColor = true;
            buttonFetchSize.Click += buttonFetchSize_Click;
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
            dataGridViewWindowConfigs.Size = new Size(557, 360);
            dataGridViewWindowConfigs.TabIndex = 0;
            dataGridViewWindowConfigs.CellEndEdit += dataGridViewWindowConfigs_CellEndEdit;
            dataGridViewWindowConfigs.SelectionChanged += dataGridViewWindowConfigs_SelectionChanged;
            // 
            // groupBoxProfiles
            // 
            groupBoxProfiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left;
            groupBoxProfiles.Controls.Add(labelActiveProfile);
            groupBoxProfiles.Controls.Add(comboBoxActiveProfile);
            groupBoxProfiles.Controls.Add(buttonAddProfile);
            groupBoxProfiles.Controls.Add(buttonRenameProfile);
            groupBoxProfiles.Controls.Add(buttonCloneProfile);
            groupBoxProfiles.Controls.Add(buttonRemoveProfile);
            groupBoxProfiles.Location = new Point(7, 7);
            groupBoxProfiles.Name = "groupBoxProfiles";
            groupBoxProfiles.Size = new Size(187, 480);
            groupBoxProfiles.TabIndex = 0;
            groupBoxProfiles.TabStop = false;
            groupBoxProfiles.Text = "Profile Editor";
            // 
            // labelActiveProfile
            // 
            labelActiveProfile.AutoSize = true;
            labelActiveProfile.Location = new Point(7, 20);
            labelActiveProfile.Name = "labelActiveProfile";
            labelActiveProfile.Size = new Size(80, 15);
            labelActiveProfile.TabIndex = 0;
            labelActiveProfile.Text = "Active Profile:";
            // 
            // comboBoxActiveProfile
            // 
            comboBoxActiveProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            comboBoxActiveProfile.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxActiveProfile.FormattingEnabled = true;
            comboBoxActiveProfile.Location = new Point(7, 38);
            comboBoxActiveProfile.Name = "comboBoxActiveProfile";
            comboBoxActiveProfile.Size = new Size(174, 23);
            comboBoxActiveProfile.TabIndex = 1;
            comboBoxActiveProfile.SelectedIndexChanged += comboBoxActiveProfile_SelectedIndexChanged;
            // 
            // buttonAddProfile
            // 
            buttonAddProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonAddProfile.Location = new Point(7, 70);
            buttonAddProfile.Name = "buttonAddProfile";
            buttonAddProfile.Size = new Size(174, 25);
            buttonAddProfile.TabIndex = 2;
            buttonAddProfile.Text = "Add New Profile";
            buttonAddProfile.UseVisualStyleBackColor = true;
            buttonAddProfile.Click += buttonAddProfile_Click;
            // 
            // buttonRenameProfile
            // 
            buttonRenameProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonRenameProfile.Location = new Point(7, 101);
            buttonRenameProfile.Name = "buttonRenameProfile";
            buttonRenameProfile.Size = new Size(174, 25);
            buttonRenameProfile.TabIndex = 3;
            buttonRenameProfile.Text = "Rename Selected";
            buttonRenameProfile.UseVisualStyleBackColor = true;
            buttonRenameProfile.Click += buttonRenameProfile_Click;
            // 
            // buttonCloneProfile
            // 
            buttonCloneProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonCloneProfile.Location = new Point(7, 132);
            buttonCloneProfile.Name = "buttonCloneProfile";
            buttonCloneProfile.Size = new Size(174, 25);
            buttonCloneProfile.TabIndex = 4;
            buttonCloneProfile.Text = "Clone Selected";
            buttonCloneProfile.UseVisualStyleBackColor = true;
            buttonCloneProfile.Click += buttonCloneProfile_Click;
            // 
            // buttonRemoveProfile
            // 
            buttonRemoveProfile.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            buttonRemoveProfile.Location = new Point(7, 163);
            buttonRemoveProfile.Name = "buttonRemoveProfile";
            buttonRemoveProfile.Size = new Size(174, 25);
            buttonRemoveProfile.TabIndex = 5;
            buttonRemoveProfile.Text = "Remove Selected";
            buttonRemoveProfile.UseVisualStyleBackColor = true;
            buttonRemoveProfile.Click += buttonRemoveProfile_Click;
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
            linkLabelGitHub.Size = new Size(119, 15);
            linkLabelGitHub.TabIndex = 3;
            linkLabelGitHub.TabStop = true;
            linkLabelGitHub.Text = "Visit my GitHub page";
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
            Text = "Window Placement Manager";
            FormClosing += FormMain_FormClosing;
            Load += FormMain_Load;
            Shown += FormMain_Shown;
            contextMenuStripTray.ResumeLayout(false);
            tabControlMain.ResumeLayout(false);
            tabPageProfiles.ResumeLayout(false);
            tabPageProfiles.PerformLayout();
            groupBoxWindowConfigs.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewWindowConfigs).EndInit();
            groupBoxProfiles.ResumeLayout(false);
            groupBoxProfiles.PerformLayout();
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            ResumeLayout(false);





            this.notifyIconMain.ContextMenuStrip = this.contextMenuStripTray;

            if(this.notifyIconMain.Icon == null)
            {
                // Attempt to use a system icon if available and no custom icon set
                try { this.notifyIconMain.Icon = System.Drawing.SystemIcons.Application; }
                catch { }
            }
            this.notifyIconMain.Text = "Window Placement Manager";
            this.notifyIconMain.Visible = true;
            this.notifyIconMain.DoubleClick += new System.EventHandler(this.notifyIconMain_DoubleClick);
        }
    }
}