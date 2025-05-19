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


        // NEW ACTION BUTTONS
        private System.Windows.Forms.Button buttonCloseApp;          // For selected app
        private System.Windows.Forms.Button buttonCloseAllProfileApps; // For all apps in profile

        // UI STRUCTURE
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelProfileActions; // New
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanelItemActions;   // New
        private System.Windows.Forms.Label labelProfileActions; // New
        private System.Windows.Forms.Label labelItemActions;    // New


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
            labelItemActions = new Label();
            labelProfileActions = new Label();
            flowLayoutPanelItemActions = new FlowLayoutPanel();
            buttonAddWindowConfig = new Button();
            buttonRemoveWindowConfig = new Button();
            buttonActivateLaunchApp = new Button();
            buttonFocus = new Button();
            buttonCloseApp = new Button();
            buttonFetchPosition = new Button();
            buttonFetchSize = new Button();
            flowLayoutPanelProfileActions = new FlowLayoutPanel();
            buttonLaunchAllProfileApps = new Button();
            buttonFocusAllProfileApps = new Button();
            buttonCloseAllProfileApps = new Button();
            buttonTestSelectedProfile = new Button();
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
            flowLayoutPanelItemActions.SuspendLayout();
            flowLayoutPanelProfileActions.SuspendLayout();
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
            tabControlMain.Size = new Size(1184, 521);
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
            tabPageProfiles.Size = new Size(1176, 493);
            tabPageProfiles.TabIndex = 0;
            tabPageProfiles.Text = "Profiles & Windows";
            tabPageProfiles.UseVisualStyleBackColor = true;
            // 
            // checkBoxDisableProgram
            // 
            checkBoxDisableProgram.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            checkBoxDisableProgram.AutoSize = true;
            checkBoxDisableProgram.Location = new Point(1008, 7);
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
            groupBoxWindowConfigs.Controls.Add(labelItemActions);
            groupBoxWindowConfigs.Controls.Add(labelProfileActions);
            groupBoxWindowConfigs.Controls.Add(flowLayoutPanelItemActions);
            groupBoxWindowConfigs.Controls.Add(flowLayoutPanelProfileActions);
            groupBoxWindowConfigs.Controls.Add(dataGridViewWindowConfigs);
            groupBoxWindowConfigs.Location = new Point(200, 30);
            groupBoxWindowConfigs.Name = "groupBoxWindowConfigs";
            groupBoxWindowConfigs.Size = new Size(970, 457);
            groupBoxWindowConfigs.TabIndex = 1;
            groupBoxWindowConfigs.TabStop = false;
            groupBoxWindowConfigs.Text = "Window Configurations for Selected Profile";
            // 
            // labelItemActions
            // 
            labelItemActions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelItemActions.AutoSize = true;
            labelItemActions.Location = new Point(7, 323);
            labelItemActions.Name = "labelItemActions";
            labelItemActions.Size = new Size(124, 15);
            labelItemActions.TabIndex = 3;
            labelItemActions.Text = "Selected Item Actions:";
            // 
            // labelProfileActions
            // 
            labelProfileActions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            labelProfileActions.AutoSize = true;
            labelProfileActions.Location = new Point(7, 397);
            labelProfileActions.Name = "labelProfileActions";
            labelProfileActions.Size = new Size(87, 15);
            labelProfileActions.TabIndex = 1;
            labelProfileActions.Text = "Profile Actions:";
            // 
            // flowLayoutPanelItemActions
            // 
            flowLayoutPanelItemActions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanelItemActions.Controls.Add(buttonAddWindowConfig);
            flowLayoutPanelItemActions.Controls.Add(buttonRemoveWindowConfig);
            flowLayoutPanelItemActions.Controls.Add(buttonActivateLaunchApp);
            flowLayoutPanelItemActions.Controls.Add(buttonFocus);
            flowLayoutPanelItemActions.Controls.Add(buttonCloseApp);
            flowLayoutPanelItemActions.Controls.Add(buttonFetchPosition);
            flowLayoutPanelItemActions.Controls.Add(buttonFetchSize);
            flowLayoutPanelItemActions.Location = new Point(7, 341);
            flowLayoutPanelItemActions.Name = "flowLayoutPanelItemActions";
            flowLayoutPanelItemActions.Size = new Size(958, 33);
            flowLayoutPanelItemActions.TabIndex = 4;
            // 
            // buttonAddWindowConfig
            // 
            buttonAddWindowConfig.Location = new Point(3, 3);
            buttonAddWindowConfig.Name = "buttonAddWindowConfig";
            buttonAddWindowConfig.Size = new Size(80, 25);
            buttonAddWindowConfig.TabIndex = 0;
            buttonAddWindowConfig.Text = "Add";
            toolTipGeneral.SetToolTip(buttonAddWindowConfig, "Add a new window configuration");
            buttonAddWindowConfig.UseVisualStyleBackColor = true;
            buttonAddWindowConfig.Click += buttonAddWindowConfig_Click;
            // 
            // buttonRemoveWindowConfig
            // 
            buttonRemoveWindowConfig.Enabled = false;
            buttonRemoveWindowConfig.Location = new Point(89, 3);
            buttonRemoveWindowConfig.Name = "buttonRemoveWindowConfig";
            buttonRemoveWindowConfig.Size = new Size(80, 25);
            buttonRemoveWindowConfig.TabIndex = 1;
            buttonRemoveWindowConfig.Text = "Remove";
            toolTipGeneral.SetToolTip(buttonRemoveWindowConfig, "Remove selected window configuration");
            buttonRemoveWindowConfig.UseVisualStyleBackColor = true;
            buttonRemoveWindowConfig.Click += buttonRemoveWindowConfig_Click;
            // 
            // buttonActivateLaunchApp
            // 
            buttonActivateLaunchApp.Enabled = false;
            buttonActivateLaunchApp.Location = new Point(175, 3);
            buttonActivateLaunchApp.Name = "buttonActivateLaunchApp";
            buttonActivateLaunchApp.Size = new Size(80, 25);
            buttonActivateLaunchApp.TabIndex = 2;
            buttonActivateLaunchApp.Text = "Launch";
            toolTipGeneral.SetToolTip(buttonActivateLaunchApp, "Launch selected app if not running, or activate its window");
            buttonActivateLaunchApp.UseVisualStyleBackColor = true;
            buttonActivateLaunchApp.Click += buttonActivateLaunchApp_Click;
            // 
            // buttonFocus
            // 
            buttonFocus.Enabled = false;
            buttonFocus.Location = new Point(261, 3);
            buttonFocus.Name = "buttonFocus";
            buttonFocus.Size = new Size(80, 25);
            buttonFocus.TabIndex = 3;
            buttonFocus.Text = "Focus";
            toolTipGeneral.SetToolTip(buttonFocus, "Update Width,Height from the live window");
            buttonFocus.UseVisualStyleBackColor = true;
            buttonFocus.Click += buttonFocus_Click;
            // 
            // buttonCloseApp
            // 
            buttonCloseApp.Enabled = false;
            buttonCloseApp.Location = new Point(347, 3);
            buttonCloseApp.Name = "buttonCloseApp";
            buttonCloseApp.Size = new Size(80, 25);
            buttonCloseApp.TabIndex = 4;
            buttonCloseApp.Text = "Close";
            toolTipGeneral.SetToolTip(buttonCloseApp, "Attempt to close the selected application");
            buttonCloseApp.UseVisualStyleBackColor = true;
            buttonCloseApp.Click += buttonCloseApp_Click;
            // 
            // buttonFetchPosition
            // 
            buttonFetchPosition.Enabled = false;
            buttonFetchPosition.Location = new Point(433, 3);
            buttonFetchPosition.Name = "buttonFetchPosition";
            buttonFetchPosition.Size = new Size(80, 25);
            buttonFetchPosition.TabIndex = 5;
            buttonFetchPosition.Text = "Fetch Pos";
            toolTipGeneral.SetToolTip(buttonFetchPosition, "Update X,Y from the live window");
            buttonFetchPosition.UseVisualStyleBackColor = true;
            buttonFetchPosition.Click += buttonFetchPosition_Click;
            // 
            // buttonFetchSize
            // 
            buttonFetchSize.Enabled = false;
            buttonFetchSize.Location = new Point(519, 3);
            buttonFetchSize.Name = "buttonFetchSize";
            buttonFetchSize.Size = new Size(80, 25);
            buttonFetchSize.TabIndex = 6;
            buttonFetchSize.Text = "Fetch Size";
            toolTipGeneral.SetToolTip(buttonFetchSize, "Update Width,Height from the live window");
            buttonFetchSize.UseVisualStyleBackColor = true;
            buttonFetchSize.Click += buttonFetchSize_Click;
            // 
            // flowLayoutPanelProfileActions
            // 
            flowLayoutPanelProfileActions.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            flowLayoutPanelProfileActions.Controls.Add(buttonLaunchAllProfileApps);
            flowLayoutPanelProfileActions.Controls.Add(buttonFocusAllProfileApps);
            flowLayoutPanelProfileActions.Controls.Add(buttonCloseAllProfileApps);
            flowLayoutPanelProfileActions.Controls.Add(buttonTestSelectedProfile);
            flowLayoutPanelProfileActions.Location = new Point(7, 415);
            flowLayoutPanelProfileActions.Name = "flowLayoutPanelProfileActions";
            flowLayoutPanelProfileActions.Size = new Size(958, 33);
            flowLayoutPanelProfileActions.TabIndex = 2;
            // 
            // buttonLaunchAllProfileApps
            // 
            buttonLaunchAllProfileApps.Enabled = false;
            buttonLaunchAllProfileApps.Location = new Point(3, 3);
            buttonLaunchAllProfileApps.Name = "buttonLaunchAllProfileApps";
            buttonLaunchAllProfileApps.Size = new Size(120, 25);
            buttonLaunchAllProfileApps.TabIndex = 0;
            buttonLaunchAllProfileApps.Text = "Launch All Missing";
            toolTipGeneral.SetToolTip(buttonLaunchAllProfileApps, "Launch all apps in profile that are not running");
            buttonLaunchAllProfileApps.UseVisualStyleBackColor = true;
            buttonLaunchAllProfileApps.Click += buttonLaunchAllProfileApps_Click;
            // 
            // buttonFocusAllProfileApps
            // 
            buttonFocusAllProfileApps.Enabled = false;
            buttonFocusAllProfileApps.Location = new Point(129, 3);
            buttonFocusAllProfileApps.Name = "buttonFocusAllProfileApps";
            buttonFocusAllProfileApps.Size = new Size(90, 25);
            buttonFocusAllProfileApps.TabIndex = 1;
            buttonFocusAllProfileApps.Text = "Focus All";
            toolTipGeneral.SetToolTip(buttonFocusAllProfileApps, "Bring all running apps in profile to foreground");
            buttonFocusAllProfileApps.UseVisualStyleBackColor = true;
            buttonFocusAllProfileApps.Click += buttonFocusAllProfileApps_Click;
            // 
            // buttonCloseAllProfileApps
            // 
            buttonCloseAllProfileApps.Enabled = false;
            buttonCloseAllProfileApps.Location = new Point(225, 3);
            buttonCloseAllProfileApps.Name = "buttonCloseAllProfileApps";
            buttonCloseAllProfileApps.Size = new Size(90, 25);
            buttonCloseAllProfileApps.TabIndex = 2;
            buttonCloseAllProfileApps.Text = "Close All";
            toolTipGeneral.SetToolTip(buttonCloseAllProfileApps, "Attempt to close all running apps in this profile");
            buttonCloseAllProfileApps.UseVisualStyleBackColor = true;
            buttonCloseAllProfileApps.Click += buttonCloseAllProfileApps_Click;
            // 
            // buttonTestSelectedProfile
            // 
            buttonTestSelectedProfile.Enabled = false;
            buttonTestSelectedProfile.Location = new Point(321, 3);
            buttonTestSelectedProfile.Name = "buttonTestSelectedProfile";
            buttonTestSelectedProfile.Size = new Size(90, 25);
            buttonTestSelectedProfile.TabIndex = 3;
            buttonTestSelectedProfile.Text = "Test Layout";
            toolTipGeneral.SetToolTip(buttonTestSelectedProfile, "Apply window positions/sizes for this profile");
            buttonTestSelectedProfile.UseVisualStyleBackColor = true;
            buttonTestSelectedProfile.Click += buttonTestSelectedProfile_Click;
            // 
            // dataGridViewWindowConfigs
            // 
            dataGridViewWindowConfigs.AllowUserToAddRows = false;
            dataGridViewWindowConfigs.AllowUserToDeleteRows = false;
            dataGridViewWindowConfigs.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridViewWindowConfigs.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewWindowConfigs.Location = new Point(7, 23);
            dataGridViewWindowConfigs.MultiSelect = false;
            dataGridViewWindowConfigs.Name = "dataGridViewWindowConfigs";
            dataGridViewWindowConfigs.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridViewWindowConfigs.Size = new Size(957, 297);
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
            tabPageSettings.Size = new Size(1176, 493);
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
            comboBoxStartupOptions.Location = new Point(114, 17);
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
            buttonSaveChanges.Size = new Size(1184, 40);
            buttonSaveChanges.TabIndex = 1;
            buttonSaveChanges.Text = "Save All Changes";
            buttonSaveChanges.UseVisualStyleBackColor = true;
            buttonSaveChanges.Click += buttonSaveChanges_Click;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1184, 561);
            Controls.Add(tabControlMain);
            Controls.Add(buttonSaveChanges);
            MinimumSize = new Size(700, 520);
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
            groupBoxWindowConfigs.PerformLayout();
            flowLayoutPanelItemActions.ResumeLayout(false);
            flowLayoutPanelProfileActions.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dataGridViewWindowConfigs).EndInit();
            groupBoxProfiles.ResumeLayout(false);
            groupBoxProfiles.PerformLayout();
            tabPageSettings.ResumeLayout(false);
            tabPageSettings.PerformLayout();
            ResumeLayout(false);
        }
        private Button buttonFocus;
    }
}