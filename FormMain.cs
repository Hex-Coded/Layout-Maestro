using System.Data;
using System.Diagnostics;
using Microsoft.VisualBasic;
using WindowPlacementManager.Models;
using WindowPlacementManager.Services;

namespace WindowPlacementManager;

public partial class FormMain : Form
{
    readonly SettingsManager _settingsManager;
    readonly StartupManager _startupManager;
    readonly WindowMonitorService _windowMonitorService;
    readonly WindowActionService _windowActionService;

    AppSettingsData _appSettings;
    Profile _selectedProfileForEditing;
    bool _isFormLoaded = false;


    public FormMain()
    {
        InitializeComponent();

        _settingsManager = new SettingsManager();
        _startupManager = new StartupManager();
        _windowMonitorService = new WindowMonitorService(_settingsManager);
        _windowActionService = new WindowActionService();
    }

    void buttonAddWindowConfig_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select or create a profile first.",
                            "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var formSelectProcess = new FormSelectProcess(); // Assuming this form correctly identifies selected process
        if(formSelectProcess.ShowDialog(this) == DialogResult.OK)
        {
            Process selectedProcess = formSelectProcess.SelectedProcess; // This is the live Process object
            IntPtr selectedHWnd = formSelectProcess.SelectedWindowHandle;
            string selectedTitle = formSelectProcess.SelectedWindowTitle;

            if(selectedProcess != null && selectedHWnd != IntPtr.Zero)
            {
                try
                {
                    // Re-check if process is still running and accessible
                    // GetProcessById will throw if not found. Check HasExited for already acquired process.
                    if(selectedProcess.HasExited)
                    {
                        Process.GetProcessById(selectedProcess.Id); // This will throw if truly gone after initial selection
                    }


                    if(Native.GetWindowRect(selectedHWnd, out RECT currentRect))
                    {
                        if(currentRect.Width <= 0 || currentRect.Height <= 0)
                        {
                            MessageBox.Show($"The selected window for '{selectedProcess.ProcessName}' (PID: {selectedProcess.Id}) has invalid dimensions (e.g., 0x0).\nThis can occur if the target application is running with higher privileges and this program is not. Try running Window Placement Manager as Administrator.",
                                            "Dimension Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }

                        string executablePath = string.Empty;
                        bool isSelectedProcessElevated = false; // Default to not elevated

                        try
                        {
                            if(!selectedProcess.HasExited)
                            {
                                executablePath = selectedProcess.MainModule?.FileName;
                                // Check if the selected process is elevated
                                isSelectedProcessElevated = ProcessPrivilegeChecker.IsProcessElevated(selectedProcess.Id, out bool accessDenied);
                                if(accessDenied && !isSelectedProcessElevated)
                                {
                                    // If access was denied trying to check, and we couldn't confirm elevation otherwise,
                                    // we might heuristically assume it *could* be elevated if our own app isn't admin.
                                    // For simplicity here, if access is denied, we won't automatically check "Run as Admin"
                                    // unless our app IS admin and could determine it.
                                    // Or, if our app isn't admin, and access is denied, it's a good hint it IS elevated.
                                    if(!ProcessPrivilegeChecker.IsCurrentProcessElevated())
                                    {
                                        // Our app is not admin, and we couldn't query target. Good chance target is admin.
                                        isSelectedProcessElevated = true;
                                        Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {selectedProcess.ProcessName} due to access denied from non-admin WPM.");
                                    }
                                }
                            }
                        }
                        catch(System.ComponentModel.Win32Exception ex)
                        {
                            Debug.WriteLine($"Could not get ExecutablePath for {selectedProcess.ProcessName}: {ex.Message}. This often happens with elevated processes if WPM is not admin.");
                            // If we can't get exe path due to elevation, good chance it's elevated.
                            if(!ProcessPrivilegeChecker.IsCurrentProcessElevated() && ex.NativeErrorCode == 5 /*ACCESS_DENIED*/)
                            {
                                isSelectedProcessElevated = true;
                                Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {selectedProcess.ProcessName} due to Win32Exception accessing MainModule from non-admin WPM.");
                            }
                        }
                        catch(InvalidOperationException ex)
                        {
                            Debug.WriteLine($"Could not get ExecutablePath for {selectedProcess.ProcessName} as it may have exited: {ex.Message}");
                        }


                        var newConfig = new WindowConfig
                        {
                            IsEnabled = true,
                            ProcessName = selectedProcess.ProcessName,
                            ExecutablePath = executablePath,
                            WindowTitleHint = selectedTitle,
                            LaunchAsAdmin = isSelectedProcessElevated, // <<< SET BASED ON DETECTED ELEVATION
                            ControlPosition = true,
                            TargetX = currentRect.Left,
                            TargetY = currentRect.Top,
                            ControlSize = true,
                            TargetWidth = currentRect.Width,
                            TargetHeight = currentRect.Height
                        };

                        if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bindingList)
                        {
                            bindingList.Add(newConfig);
                        }
                        else
                        {
                            _selectedProfileForEditing.WindowConfigs.Add(newConfig);
                            LoadWindowConfigsForSelectedProfile();
                        }

                        if(dataGridViewWindowConfigs.Rows.Count > 0)
                        {
                            dataGridViewWindowConfigs.ClearSelection();
                            DataGridViewRow newRow = dataGridViewWindowConfigs.Rows
                                .Cast<DataGridViewRow>()
                                .FirstOrDefault(r => r.DataBoundItem == newConfig);
                            if(newRow != null)
                            {
                                newRow.Selected = true;
                                dataGridViewWindowConfigs.CurrentCell = newRow.Cells[0];
                                dataGridViewWindowConfigs.FirstDisplayedScrollingRowIndex = newRow.Index;
                            }
                        }
                        UpdateProfileSpecificActionButtonsState();
                    }
                    else
                    {
                        MessageBox.Show($"Could not get window dimensions for '{selectedProcess.ProcessName}'. The window might have closed or is inaccessible.",
                                        "Dimension Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
                catch(ArgumentException argEx)
                {
                    MessageBox.Show($"The selected process '{selectedProcess.ProcessName}' (PID: {selectedProcess.Id}) is no longer running or is inaccessible: {argEx.Message}",
                                        "Process Exited or Inaccessible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Error processing selected window: {ex.Message}", "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Debug.WriteLine($"Error adding window config from selected process: {ex.Message}");
                }
                finally
                {
                    selectedProcess?.Dispose(); // Dispose the Process object we might have acquired
                }
            }
        }
    }
    void buttonTestSelectedProfile_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing != null)
        {
            if(!_selectedProfileForEditing.WindowConfigs.Any(wc => wc.IsEnabled))
            {
                MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled window configurations defined.",
                                "No Enabled Configurations", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _windowMonitorService.TestProfileLayout(_selectedProfileForEditing);
        }
        else
        {
            MessageBox.Show("Please select a profile from the ComboBox to test.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }


    void PopulateActiveProfileComboBox()
    {
        string previouslySelectedProfileNameInComboBox = (comboBoxActiveProfile.SelectedItem as Profile)?.Name;

        comboBoxActiveProfile.BeginUpdate();
        comboBoxActiveProfile.Items.Clear();
        comboBoxActiveProfile.DisplayMember = nameof(Profile.Name);

        foreach(var profile in _appSettings.Profiles)
        {
            comboBoxActiveProfile.Items.Add(profile);
        }
        comboBoxActiveProfile.EndUpdate();

        Profile profileToSelectInComboBox = null;

        if(!string.IsNullOrEmpty(previouslySelectedProfileNameInComboBox))
        {
            profileToSelectInComboBox = _appSettings.Profiles.FirstOrDefault(p => p.Name == previouslySelectedProfileNameInComboBox);
        }

        if(profileToSelectInComboBox == null && !string.IsNullOrEmpty(_appSettings.ActiveProfileName))
        {
            profileToSelectInComboBox = _appSettings.Profiles.FirstOrDefault(p => p.Name == _appSettings.ActiveProfileName);
        }

        if(profileToSelectInComboBox == null && _appSettings.Profiles.Any())
        {
            profileToSelectInComboBox = _appSettings.Profiles.First();
        }

        if(profileToSelectInComboBox != null)
        {
            comboBoxActiveProfile.SelectedItem = profileToSelectInComboBox;
        }
        else if(_appSettings.Profiles.Any())
        {
            comboBoxActiveProfile.SelectedIndex = 0;
        }
        else
        {
            _appSettings.ActiveProfileName = string.Empty;
        }
    }


    void FormMain_Load(object sender, EventArgs e)
    {
        _isFormLoaded = false;

        LoadSettings();
        InitializeDataGridView();
        InitializeStartupOptionsComboBox();

        PopulateActiveProfileComboBox();

        _selectedProfileForEditing = comboBoxActiveProfile.SelectedItem as Profile;

        if(_selectedProfileForEditing != null)
        {
            if(_appSettings.ActiveProfileName != _selectedProfileForEditing.Name)
            {
                _appSettings.ActiveProfileName = _selectedProfileForEditing.Name;
            }
            LoadWindowConfigsForSelectedProfile();
        }
        else
        {
            _appSettings.ActiveProfileName = string.Empty;
            LoadWindowConfigsForSelectedProfile();
        }

        UpdateUIFromSettings();
        dataGridViewWindowConfigs_SelectionChanged(null, null);
        UpdateProfileSpecificActionButtonsState();
        UpdateProfileManagementButtonsState();

        _isFormLoaded = true;
        checkBoxDisableProgram.Checked = _appSettings.DisableProgramActivity;
        UpdateDisabledCheckbox();
    }


    void exitToolStripMenuItem_Click(object sender, EventArgs e) => ForceExitApplication();

    void HideForm() => HideFormAndShowTrayIcon();

    void ShowForm() => ShowFormFromTrayIcon();

    void FormMain_Shown(object sender, EventArgs e)
    {
        if(this.WindowState == FormWindowState.Minimized && (this.ShowInTaskbar == false || this.Visible == false))
        {
            HideFormAndShowTrayIcon();
        }
        else
        {
            if(notifyIconMain != null && this.Visible) notifyIconMain.Visible = false;
        }
    }

    void UpdateProfileSpecificActionButtonsState()
    {
        bool profileSelectedAndHasConfigs = _selectedProfileForEditing != null &&
                                            _selectedProfileForEditing.WindowConfigs.Any(wc => wc.IsEnabled);

        buttonTestSelectedProfile.Enabled = profileSelectedAndHasConfigs;
        if(buttonLaunchAllProfileApps != null) buttonLaunchAllProfileApps.Enabled = profileSelectedAndHasConfigs;
        if(buttonFocusAllProfileApps != null) buttonFocusAllProfileApps.Enabled = profileSelectedAndHasConfigs;
    }

    private IntPtr FindWindowForConfig(WindowConfig config)
    {
        if(config == null) return IntPtr.Zero;
        var foundWindow = WindowEnumerationService.FindMostSuitableWindow(config);
        return foundWindow?.HWnd ?? IntPtr.Zero;
    }


    void UpdateProfileManagementButtonsState()
    {
        bool profileSelected = _selectedProfileForEditing != null;
        buttonRemoveProfile.Enabled = profileSelected && _appSettings.Profiles.Count > 1;
        buttonRenameProfile.Enabled = profileSelected;
        buttonCloneProfile.Enabled = profileSelected;
        buttonAddWindowConfig.Enabled = profileSelected;
    }


    void InitializeStartupOptionsComboBox()
    {
        comboBoxStartupOptions.Items.Clear();
        comboBoxStartupOptions.Items.Add(new ComboBoxItem("Don't Boot with Windows", StartupType.None));
        comboBoxStartupOptions.Items.Add(new ComboBoxItem("Boot with Windows (User)", StartupType.Normal));
        comboBoxStartupOptions.Items.Add(new ComboBoxItem("Boot with Windows (Administrator)", StartupType.Admin));
        comboBoxStartupOptions.DisplayMember = "DisplayName";
        comboBoxStartupOptions.ValueMember = "Value";
    }

    public class ComboBoxItem
    {
        public string DisplayName { get; set; }
        public object Value { get; set; }
        public ComboBoxItem(string displayName, object value)
        {
            DisplayName = displayName;
            Value = value;
        }
        public override string ToString() => DisplayName;
    }

    void UpdateUIFromSettings()
    {
        StartupType currentStartup = _startupManager.GetCurrentStartupType();
        foreach(ComboBoxItem item in comboBoxStartupOptions.Items)
        {
            if((StartupType)item.Value == currentStartup)
            {
                comboBoxStartupOptions.SelectedItem = item;
                break;
            }
        }
        if(comboBoxStartupOptions.SelectedItem == null && comboBoxStartupOptions.Items.Count > 0)
        {
            comboBoxStartupOptions.SelectedIndex = 0;
        }

        checkBoxDisableProgram.Checked = _appSettings.DisableProgramActivity;
    }

    void InitializeDataGridView()
    {
        dataGridViewWindowConfigs.AutoGenerateColumns = false;
        dataGridViewWindowConfigs.Columns.Clear();


        var enabledCol = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.IsEnabled),
            HeaderText = "On",
            Width = 35,
            Frozen = true,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        dataGridViewWindowConfigs.Columns.Add(enabledCol);

        var procNameCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.ProcessName),
            HeaderText = "Process Name",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 20,
            MinimumWidth = 100
        };
        dataGridViewWindowConfigs.Columns.Add(procNameCol);

        var execPathCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.ExecutablePath),
            HeaderText = "Executable Path",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 30,
            MinimumWidth = 150,
            ToolTipText = "Full path to the executable (optional, helps with launching)"
        };
        dataGridViewWindowConfigs.Columns.Add(execPathCol);

        var titleHintCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.WindowTitleHint),
            HeaderText = "Window Title",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            FillWeight = 25,
            MinimumWidth = 120
        };
        dataGridViewWindowConfigs.Columns.Add(titleHintCol);

        var launchAdminCol = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.LaunchAsAdmin),
            HeaderText = "Run Adm?",
            ToolTipText = "Launch this application with Administrator privileges",
            Width = 65,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        dataGridViewWindowConfigs.Columns.Add(launchAdminCol);

        var ctrlPosCol = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.ControlPosition),
            HeaderText = "Pos?",
            ToolTipText = "Control Position",
            Width = 40,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        dataGridViewWindowConfigs.Columns.Add(ctrlPosCol);

        var xCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.TargetX),
            HeaderText = "X",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        dataGridViewWindowConfigs.Columns.Add(xCol);

        var yCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.TargetY),
            HeaderText = "Y",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        dataGridViewWindowConfigs.Columns.Add(yCol);

        var ctrlSizeCol = new DataGridViewCheckBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.ControlSize),
            HeaderText = "Size?",
            ToolTipText = "Control Size",
            Width = 45,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        dataGridViewWindowConfigs.Columns.Add(ctrlSizeCol);

        var wCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.TargetWidth),
            HeaderText = "W",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        dataGridViewWindowConfigs.Columns.Add(wCol);

        var hCol = new DataGridViewTextBoxColumn
        {
            DataPropertyName = nameof(WindowConfig.TargetHeight),
            HeaderText = "H",
            Width = 50,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight }
        };
        dataGridViewWindowConfigs.Columns.Add(hCol);


    }

    void LoadSettings()
    {
        _appSettings = _settingsManager.LoadSettings();
        if(!_appSettings.Profiles.Any())
        {
            var defaultProfile = new Profile("Default Profile");
            _appSettings.Profiles.Add(defaultProfile);
            _appSettings.ActiveProfileName = defaultProfile.Name;
        }
        _windowMonitorService.LoadAndApplySettings();
    }

    void SaveSettings()
    {
        if(!_isFormLoaded) return;

        if(comboBoxActiveProfile.SelectedItem is Profile selectedActiveProfile)
        {
            _appSettings.ActiveProfileName = selectedActiveProfile.Name;
        }
        else if(_appSettings.Profiles.Any())
        {
            _appSettings.ActiveProfileName = _appSettings.Profiles.First().Name;
        }
        else
        {
            _appSettings.ActiveProfileName = string.Empty;
        }


        if(comboBoxStartupOptions.SelectedItem is ComboBoxItem selectedStartupItem)
        {
            _appSettings.StartupOption = (StartupType)selectedStartupItem.Value;
        }
        else
        {
            _appSettings.StartupOption = StartupType.None;
        }
        _startupManager.SetStartup(_appSettings.StartupOption);

        _appSettings.DisableProgramActivity = checkBoxDisableProgram.Checked;

        _settingsManager.SaveSettings(_appSettings);
        _windowMonitorService.LoadAndApplySettings();
    }

    void LoadWindowConfigsForSelectedProfile()
    {
        if(_selectedProfileForEditing != null)
        {
            var bindableList = new SortableBindingList<WindowConfig>(_selectedProfileForEditing.WindowConfigs);
            dataGridViewWindowConfigs.DataSource = bindableList;
            groupBoxWindowConfigs.Text = $"Window Configurations for '{_selectedProfileForEditing.Name}'";
        }
        else
        {
            dataGridViewWindowConfigs.DataSource = null;
            groupBoxWindowConfigs.Text = "Window Configurations (No Profile Selected)";
        }
        dataGridViewWindowConfigs_SelectionChanged(null, null);
        UpdateProfileSpecificActionButtonsState();
    }

    void settingsToolStripMenuItem_Click(object sender, EventArgs e) => ShowForm();

    void notifyIconMain_DoubleClick(object sender, EventArgs e) => ShowForm();

    void FormMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        if(e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideForm();
        }
    }

    void buttonAddProfile_Click(object sender, EventArgs e)
    {
        string newProfileName = Interaction.InputBox("Enter new profile name:", "Add Profile", "New Profile " + (_appSettings.Profiles.Count + 1));
        if(!string.IsNullOrWhiteSpace(newProfileName))
        {
            if(_appSettings.Profiles.Any(p => p.Name.Equals(newProfileName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            var newProfile = new Profile(newProfileName);
            _appSettings.Profiles.Add(newProfile);
            PopulateActiveProfileComboBox();
            comboBoxActiveProfile.SelectedItem = newProfile;
        }
    }

    void buttonRemoveProfile_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing != null)
        {
            if(_appSettings.Profiles.Count <= 1)
            {
                MessageBox.Show("Cannot remove the last profile.", "Action Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if(MessageBox.Show($"Are you sure you want to delete profile '{_selectedProfileForEditing.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string removedProfileName = _selectedProfileForEditing.Name;
                _appSettings.Profiles.Remove(_selectedProfileForEditing);

                if(_appSettings.ActiveProfileName == removedProfileName)
                {
                    _appSettings.ActiveProfileName = _appSettings.Profiles.FirstOrDefault()?.Name ?? string.Empty;
                }
                PopulateActiveProfileComboBox();
            }
        }
        else
        {
            MessageBox.Show("Please select a profile to remove.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void buttonRenameProfile_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing != null)
        {
            string newName = Interaction.InputBox("Enter new name for profile:", "Rename Profile", _selectedProfileForEditing.Name);
            if(!string.IsNullOrWhiteSpace(newName) && newName != _selectedProfileForEditing.Name)
            {
                if(_appSettings.Profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != _selectedProfileForEditing))
                {
                    MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                bool isActiveProfile = (_appSettings.ActiveProfileName == _selectedProfileForEditing.Name);
                _selectedProfileForEditing.Name = newName;
                if(isActiveProfile)
                {
                    _appSettings.ActiveProfileName = newName;
                }

                PopulateActiveProfileComboBox();
            }
        }
    }

    void comboBoxActiveProfile_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(!_isFormLoaded) return;

        Profile selectedProfile = comboBoxActiveProfile.SelectedItem as Profile;
        _selectedProfileForEditing = selectedProfile;

        if(selectedProfile != null)
        {
            _appSettings.ActiveProfileName = selectedProfile.Name;
        }
        else
        {
            _appSettings.ActiveProfileName = string.Empty;
        }

        LoadWindowConfigsForSelectedProfile();
        _windowMonitorService.LoadAndApplySettings();

        UpdateProfileSpecificActionButtonsState();
        UpdateProfileManagementButtonsState();
    }

    void buttonRemoveWindowConfig_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing != null && dataGridViewWindowConfigs.SelectedRows.Count > 0)
        {
            if(dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
            {
                if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bindingList)
                {
                    bindingList.Remove(selectedConfig);
                    UpdateProfileSpecificActionButtonsState();
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a window configuration to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void dataGridViewWindowConfigs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
    }

    void dataGridViewWindowConfigs_SelectionChanged(object sender, EventArgs e)
    {
        bool rowSelected = dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
                           dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig;
        buttonFetchPosition.Enabled = rowSelected;
        buttonFetchSize.Enabled = rowSelected;
        buttonRemoveWindowConfig.Enabled = rowSelected;

        if(buttonActivateLaunchApp != null) buttonActivateLaunchApp.Enabled = rowSelected;
    }

    void buttonFetchPosition_Click(object sender, EventArgs e)
    {
        if(dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
            dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
        {
            IntPtr hWnd = FindWindowForConfig(selectedConfig);
            if(hWnd != IntPtr.Zero)
            {
                if(Native.GetWindowRect(hWnd, out RECT currentRect))
                {
                    selectedConfig.TargetX = currentRect.Left;
                    selectedConfig.TargetY = currentRect.Top;
                    if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl) bl.ResetItem(bl.IndexOf(selectedConfig));
                    else dataGridViewWindowConfigs.Refresh();
                }
                else MessageBox.Show("Could not get current window position. The window might have closed or is inaccessible.", "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else MessageBox.Show($"Could not find a running window for Process: '{selectedConfig.ProcessName}'" + (!string.IsNullOrWhiteSpace(selectedConfig.WindowTitleHint) ? $" with Window Title: '{selectedConfig.WindowTitleHint}'" : "") + ".\nEnsure the application is running and the title hint (if any) is correct.", "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void buttonFetchSize_Click(object sender, EventArgs e)
    {
        if(dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
            dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
        {
            IntPtr hWnd = FindWindowForConfig(selectedConfig);
            if(hWnd != IntPtr.Zero)
            {
                if(Native.GetWindowRect(hWnd, out RECT currentRect))
                {
                    selectedConfig.TargetWidth = currentRect.Width;
                    selectedConfig.TargetHeight = currentRect.Height;
                    if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl) bl.ResetItem(bl.IndexOf(selectedConfig));
                    else dataGridViewWindowConfigs.Refresh();
                }
                else MessageBox.Show("Could not get current window size. The window might have closed or is inaccessible.", "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else MessageBox.Show($"Could not find a running window for Process: '{selectedConfig.ProcessName}'" + (!string.IsNullOrWhiteSpace(selectedConfig.WindowTitleHint) ? $" with Window Title: '{selectedConfig.WindowTitleHint}'" : "") + ".\nEnsure the application is running and the title hint (if any) is correct.", "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void buttonSaveChanges_Click(object sender, EventArgs e)
    {
        SaveSettings();
        MessageBox.Show("Settings saved.", "Window Positioner", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void buttonCloneProfile_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing != null)
        {
            string originalProfileName = _selectedProfileForEditing.Name;
            string newProfileNameSuggestion = originalProfileName + " (Copy)";
            int copyCount = 1;

            while(_appSettings.Profiles.Any(p => p.Name.Equals(newProfileNameSuggestion, StringComparison.OrdinalIgnoreCase)))
            {
                copyCount++;
                newProfileNameSuggestion = $"{originalProfileName} (Copy {copyCount})";
            }

            string newProfileName = Interaction.InputBox("Enter name for the cloned profile:", "Clone Profile", newProfileNameSuggestion);

            if(string.IsNullOrWhiteSpace(newProfileName))
            {
                return;
            }

            if(_appSettings.Profiles.Any(p => p.Name.Equals(newProfileName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var clonedProfile = new Profile(newProfileName);

            foreach(var windowConfig in _selectedProfileForEditing.WindowConfigs)
            {
                clonedProfile.WindowConfigs.Add(new WindowConfig
                {
                    IsEnabled = windowConfig.IsEnabled,
                    ProcessName = windowConfig.ProcessName,
                    ExecutablePath = windowConfig.ExecutablePath,
                    WindowTitleHint = windowConfig.WindowTitleHint,
                    LaunchAsAdmin = windowConfig.LaunchAsAdmin,
                    ControlPosition = windowConfig.ControlPosition,
                    TargetX = windowConfig.TargetX,
                    TargetY = windowConfig.TargetY,
                    ControlSize = windowConfig.ControlSize,
                    TargetWidth = windowConfig.TargetWidth,
                    TargetHeight = windowConfig.TargetHeight
                });
            }

            _appSettings.Profiles.Add(clonedProfile);

            PopulateActiveProfileComboBox();
            var newlyClonedProfileInList = comboBoxActiveProfile.Items
                                            .OfType<Profile>()
                                            .FirstOrDefault(p => p.Name == clonedProfile.Name);
            if(newlyClonedProfileInList != null)
            {
                comboBoxActiveProfile.SelectedItem = newlyClonedProfileInList;
            }
        }
        else
        {
            MessageBox.Show("Please select a profile to clone.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
    void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            this.linkLabelGitHub.LinkVisited = true;
            System.Diagnostics.Process.Start(new ProcessStartInfo("https://github.com/BitSwapper") { UseShellExecute = true });
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void checkBoxDisableProgram_CheckedChanged(object sender, EventArgs e) => UpdateDisabledCheckbox();

    void UpdateDisabledCheckbox()
    {
        if(!_isFormLoaded) return;

        bool isProgramDisabled = checkBoxDisableProgram.Checked;
        _appSettings.DisableProgramActivity = isProgramDisabled;
        _windowMonitorService.SetPositioningActive(!isProgramDisabled);

        groupBoxWindowConfigs.Enabled = !isProgramDisabled;
        groupBoxProfiles.Enabled = !isProgramDisabled;

        groupBoxWindowConfigs.BackColor = isProgramDisabled ? SystemColors.ControlDark : SystemColors.Control;
        checkBoxDisableProgram.ForeColor = isProgramDisabled ? Color.Red : SystemColors.ControlText;
    }

    private void buttonActivateLaunchApp_Click(object sender, EventArgs e)
    {
        if(dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
            dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
        {
            if(!selectedConfig.IsEnabled)
            {
                MessageBox.Show($"This configuration for '{selectedConfig.ProcessName}' is disabled.", "Action Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            bool success = _windowActionService.ActivateOrLaunchApp(selectedConfig);

            if(!success)
            {
                var windowInfo = _windowActionService.FindManagedWindow(selectedConfig);
                string appIdentifier = string.IsNullOrWhiteSpace(selectedConfig.ExecutablePath) ? selectedConfig.ProcessName : selectedConfig.ExecutablePath;
                appIdentifier = string.IsNullOrWhiteSpace(appIdentifier) ? "(Unnamed App)" : appIdentifier;

                if(windowInfo?.HWnd != IntPtr.Zero)
                {
                    MessageBox.Show($"Failed to bring the window for '{appIdentifier}' to the foreground.\nThis can happen if another application is actively preventing focus changes.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    Debug.WriteLine($"ActivateOrLaunchApp failed for '{appIdentifier}'. LaunchApp should have shown a specific error.");
                }
            }
        }
        else
        {
            MessageBox.Show("Please select a window configuration from the list.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void buttonLaunchAllProfileApps_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select a profile first.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if(!_selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled))
        {
            MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled window configurations to launch.", "No Action Taken", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _windowActionService.ActivateOrLaunchAllAppsInProfile(_selectedProfileForEditing, launchIfNotRunning: true, bringToForegroundIfRunning: false);
        MessageBox.Show($"Attempted to launch all missing enabled apps in profile '{_selectedProfileForEditing.Name}'.\nCheck Debug Output for details.", "Launch All Requested", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void buttonFocusAllProfileApps_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select a profile first.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if(!_selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled))
        {
            MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled window configurations to focus.", "No Action Taken", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        _windowActionService.ActivateOrLaunchAllAppsInProfile(_selectedProfileForEditing, launchIfNotRunning: false, bringToForegroundIfRunning: true);
        MessageBox.Show($"Attempted to bring all running, enabled apps in profile '{_selectedProfileForEditing.Name}' to the foreground.\nCheck Debug Output for details.", "Focus All Requested", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected override void WndProc(ref Message m)
    {
        if(m.Msg == 0x0112 && m.WParam.ToInt32() == 0xF020)
        {
            HideFormAndShowTrayIcon();
            return;
        }
        base.WndProc(ref m);
    }

    private void HideFormAndShowTrayIcon()
    {
        this.Hide();
        if(notifyIconMain != null)
        {
            notifyIconMain.Visible = true;
        }
    }

    private void ShowFormFromTrayIcon()
    {
        if(notifyIconMain != null) notifyIconMain.Visible = false;
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.Activate();
        this.BringToFront();
    }


    private void ForceExitApplication()
    {
        _windowMonitorService?.Dispose();
        if(notifyIconMain != null)
        {
            notifyIconMain.Visible = false;
            notifyIconMain.Dispose();
            notifyIconMain = null;
        }
        Environment.Exit(0);
    }
}