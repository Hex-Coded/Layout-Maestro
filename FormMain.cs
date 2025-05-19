using System.Diagnostics;
using WindowPlacementManager.Helpers;
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
        _windowActionService = new WindowActionService();
        _windowMonitorService = new WindowMonitorService(_settingsManager, _windowActionService);
        TrayIconUIManager.InitializeNotifyIcon(this.notifyIconMain);
    }


    async void buttonLaunchAllProfileApps_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!_selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        buttonLaunchAllProfileApps.Enabled = false;
        try
        {
            await _windowActionService.ProcessAllAppsInProfile(_selectedProfileForEditing,
                launchIfNotRunning: true,
                bringToForegroundIfRunning: false,
                closeIfRunning: false,
                delayBetweenLaunchesMs: 200);
        }
        finally
        {
            buttonLaunchAllProfileApps.Enabled = true;
            ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, _selectedProfileForEditing);
        }
    }

    async void buttonFocusAllProfileApps_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!_selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        buttonFocusAllProfileApps.Enabled = false;
        try
        {
            await _windowActionService.ProcessAllAppsInProfile(_selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: true,
                closeIfRunning: false,
                delayBetweenLaunchesMs: 100);
        }
        finally
        {
            buttonFocusAllProfileApps.Enabled = true;
            ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, _selectedProfileForEditing);
        }
    }

    void UpdateAllButtonStates()
    {
        ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, _selectedProfileForEditing);
        WindowConfigGridUIManager.UpdateSelectionDependentButtons(dataGridViewWindowConfigs, buttonRemoveWindowConfig, buttonActivateLaunchApp, buttonFocus, buttonCloseApp, buttonFetchPosition, buttonFetchSize);
        ProfileUIManager.UpdateProfileManagementButtons(buttonRemoveProfile, buttonRenameProfile, buttonCloneProfile, buttonAddWindowConfig, _selectedProfileForEditing != null, _appSettings.Profiles.Count);
    }

    void dataGridViewWindowConfigs_SelectionChanged(object sender, EventArgs e) => UpdateAllButtonStates();

    void FormMain_Load(object sender, EventArgs e)
    {
        _isFormLoaded = false;

        LoadAppSettingsAndInitializeMonitorServiceGlobals();

        WindowConfigGridUIManager.InitializeDataGridView(dataGridViewWindowConfigs);
        StartupOptionsUIManager.InitializeComboBox(comboBoxStartupOptions);

        SetupInitialActiveProfile();

        UpdateUIFromSettings();

        UpdateAllButtonStates();

        dataGridViewWindowConfigs.CellValueChanged += dataGridViewWindowConfigs_CellValueChanged;

        _windowMonitorService.InitializeTimer();

        _isFormLoaded = true;

        HandleDisableProgramActivityChanged();
    }

    void LoadAppSettingsAndInitializeMonitorServiceGlobals()
    {
        _appSettings = _settingsManager.LoadSettings();
        if(!_appSettings.Profiles.Any())
        {
            var defaultProfile = new Profile("Default Profile");
            _appSettings.Profiles.Add(defaultProfile);
            _appSettings.ActiveProfileName = defaultProfile.Name;
            _settingsManager.SaveSettings(_appSettings);
            _appSettings = _settingsManager.LoadSettings();
        }
        _windowMonitorService.LoadAndApplySettings();
    }

    void SetupInitialActiveProfile()
    {
        string initialActiveProfileName = _windowMonitorService.GetActiveProfileNameFromSettings();
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, initialActiveProfileName);

        _selectedProfileForEditing = comboBoxActiveProfile.SelectedItem as Profile;

        if(_selectedProfileForEditing == null && _appSettings.Profiles.Any())
        {
            _selectedProfileForEditing = _appSettings.Profiles.First();
            comboBoxActiveProfile.SelectedItem = _selectedProfileForEditing;
        }

        _appSettings.ActiveProfileName = _selectedProfileForEditing?.Name ?? string.Empty;

        LoadWindowConfigsForCurrentProfile();

        _windowMonitorService.UpdateActiveProfileReference(_selectedProfileForEditing);
        Debug.WriteLine($"FormMain_Load: Initial active profile '{_selectedProfileForEditing?.Name ?? "null"}' passed to WindowMonitorService.");
    }


    void HandleActiveProfileChange()
    {
        if(!_isFormLoaded) return;

        Profile selected = comboBoxActiveProfile.SelectedItem as Profile;
        if(_selectedProfileForEditing == selected && selected != null)
        {
        }

        _selectedProfileForEditing = selected;
        _appSettings.ActiveProfileName = selected?.Name ?? string.Empty;

        LoadWindowConfigsForCurrentProfile();

        _windowMonitorService.UpdateActiveProfileReference(_selectedProfileForEditing);
        Debug.WriteLine($"HandleActiveProfileChange: Profile '{_selectedProfileForEditing?.Name ?? "null"}' passed to WindowMonitorService.");
        UpdateAllButtonStates();
    }

    void dataGridViewWindowConfigs_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if(!_isFormLoaded || e.RowIndex < 0 || _selectedProfileForEditing == null) return;

        DataGridViewColumn changedColumn = dataGridViewWindowConfigs.Columns[e.ColumnIndex];
        WindowConfig changedConfig = dataGridViewWindowConfigs.Rows[e.RowIndex].DataBoundItem as WindowConfig;

        if(changedConfig == null) return;

        if(changedColumn.DataPropertyName == nameof(WindowConfig.AutoRelaunchEnabled) ||
            changedColumn.DataPropertyName == nameof(WindowConfig.IsEnabled) ||
            changedColumn.DataPropertyName == nameof(WindowConfig.ProcessName) ||
            changedColumn.DataPropertyName == nameof(WindowConfig.ExecutablePath) ||
            changedColumn.DataPropertyName == nameof(WindowConfig.WindowTitleHint))
        {
            Debug.WriteLine($"DGV CellValueChanged: {changedColumn.DataPropertyName} for '{changedConfig.ProcessName}'. Notifying monitor service.");
            _windowMonitorService.UpdateActiveProfileReference(_selectedProfileForEditing);
        }
    }

    void SaveAppSettings()
    {
        if(!_isFormLoaded) return;

        if(comboBoxActiveProfile.SelectedItem is Profile selectedActiveCbProfile)
            _appSettings.ActiveProfileName = selectedActiveCbProfile.Name;
        else if(_appSettings.Profiles.Any())
            _appSettings.ActiveProfileName = _appSettings.Profiles.First().Name;
        else
            _appSettings.ActiveProfileName = string.Empty;


        StartupType newlySelectedStartupOption = StartupOptionsUIManager.GetSelectedStartupType(comboBoxStartupOptions);
        StartupType currentSystemStartupOption = _startupManager.GetCurrentStartupType();

        if(newlySelectedStartupOption != currentSystemStartupOption)
        {
            Debug.WriteLine($"Startup option changed from {currentSystemStartupOption} to {newlySelectedStartupOption}. Applying change.");
            _startupManager.SetStartup(newlySelectedStartupOption);
            _appSettings.StartupOption = newlySelectedStartupOption;
        }
        else
        {
            _appSettings.StartupOption = newlySelectedStartupOption;
            Debug.WriteLine($"Startup option ({newlySelectedStartupOption}) unchanged from current system state.");
        }

        _appSettings.DisableProgramActivity = checkBoxDisableProgram.Checked;

        _settingsManager.SaveSettings(_appSettings);
        _windowMonitorService.LoadAndApplySettings();

        _windowMonitorService.UpdateActiveProfileReference(_selectedProfileForEditing);
        Debug.WriteLine($"SaveAppSettings: Profile '{_selectedProfileForEditing?.Name ?? "null"}' re-passed to WindowMonitorService after save.");

        MessageBox.Show("Settings saved.", "Window Positioner", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void HandleDisableProgramActivityChanged()
    {
        if(!_isFormLoaded && !checkBoxDisableProgram.IsHandleCreated) return;

        bool isDisabled = checkBoxDisableProgram.Checked;

        _windowMonitorService.SetPositioningActive(!isDisabled);

        GeneralUIManager.UpdateProgramActivityUI(checkBoxDisableProgram, groupBoxWindowConfigs, groupBoxProfiles, isDisabled);
    }

    void UpdateUIFromSettings()
    {
        StartupOptionsUIManager.SelectCurrentOption(comboBoxStartupOptions, _startupManager.GetCurrentStartupType());
        checkBoxDisableProgram.Checked = _appSettings.DisableProgramActivity;
    }

    void LoadWindowConfigsForCurrentProfile() => WindowConfigGridUIManager.LoadWindowConfigsForProfile(dataGridViewWindowConfigs, groupBoxWindowConfigs, _selectedProfileForEditing);

    void LoadAppSettings()
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

    IntPtr FindWindowForConfig(WindowConfig config) => (config == null) ? IntPtr.Zero : (WindowEnumerationService.FindMostSuitableWindow(config)?.HWnd ?? IntPtr.Zero);

    void buttonAddWindowConfig_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null) { MessageBox.Show("Please select or create a profile first.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

        using var formSelectProcess = new FormSelectProcess();
        if(formSelectProcess.ShowDialog(this) != DialogResult.OK) return;

        Process selectedProcess = formSelectProcess.SelectedProcess;
        IntPtr selectedHWnd = formSelectProcess.SelectedWindowHandle;
        string selectedTitle = formSelectProcess.SelectedWindowTitle;

        if(selectedProcess == null || selectedHWnd == IntPtr.Zero) return;

        try
        {
            if(selectedProcess.HasExited || !Native.GetWindowRect(selectedHWnd, out RECT currentRect)) { ShowProcessOrWindowError(selectedProcess.ProcessName, "Could not get window dimensions or process exited."); return; }
            if(currentRect.Width <= 0 || currentRect.Height <= 0) { ShowProcessOrWindowError(selectedProcess.ProcessName, "Selected window has invalid dimensions (e.g., 0x0). If target is admin, run this as admin.", "Dimension Error"); return; }

            string executablePath = GetExecutablePathSafe(selectedProcess);
            bool isSelectedProcessElevated = DetermineElevation(selectedProcess, executablePath);

            var newConfig = new WindowConfig
            {
                IsEnabled = true,
                ProcessName = selectedProcess.ProcessName,
                ExecutablePath = executablePath,
                WindowTitleHint = selectedTitle,
                LaunchAsAdmin = isSelectedProcessElevated,
                ControlPosition = true,
                TargetX = currentRect.Left,
                TargetY = currentRect.Top,
                ControlSize = true,
                TargetWidth = currentRect.Width,
                TargetHeight = currentRect.Height
            };

            WindowConfigGridUIManager.AddAndSelectWindowConfig(dataGridViewWindowConfigs, _selectedProfileForEditing, newConfig);
            UpdateAllButtonStates();
        }
        catch(ArgumentException argEx) { ShowProcessOrWindowError(selectedProcess.ProcessName, $"Process (PID: {selectedProcess.Id}) is no longer running or is inaccessible: {argEx.Message}", "Process Exited or Inaccessible"); }
        catch(Exception ex) { ShowProcessOrWindowError(selectedProcess.ProcessName, $"Error processing selected window: {ex.Message}", "Processing Error"); Debug.WriteLine($"Error adding window config: {ex.Message}"); }
        finally { selectedProcess?.Dispose(); }
    }

    string GetExecutablePathSafe(Process process)
    {
        try { return process.HasExited ? string.Empty : process.MainModule?.FileName; }
        catch(System.ComponentModel.Win32Exception ex) { Debug.WriteLine($"Could not get ExecutablePath for {process.ProcessName}: {ex.Message}."); return string.Empty; }
        catch(InvalidOperationException ex) { Debug.WriteLine($"Could not get ExecutablePath for {process.ProcessName} (may have exited): {ex.Message}"); return string.Empty; }
    }

    bool DetermineElevation(Process process, string executablePath)
    {
        if(process.HasExited) return false;
        bool isElevated = ProcessPrivilegeChecker.IsProcessElevated(process.Id, out bool accessDenied);
        if(accessDenied && !isElevated && !ProcessPrivilegeChecker.IsCurrentProcessElevated())
        {
            Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {process.ProcessName} due to access denied from non-admin WPM.");
            return true;
        }
        if(string.IsNullOrEmpty(executablePath) && !ProcessPrivilegeChecker.IsCurrentProcessElevated())
        {
            try
            {
                var _ = process.MainModule?.FileName;
            }
            catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 5)
            {
                Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {process.ProcessName} due to Win32Exception accessing MainModule.");
                return true;
            }
            catch(InvalidOperationException)
            {
                Debug.WriteLine($"InvalidOperationException for {process.ProcessName} when trying to access MainModule, likely exited.");
            }
        }
        return isElevated;
    }

    void ShowProcessOrWindowError(string processName, string message, string title = "Error") => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    void buttonCloseApp_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedConfig == null) { MessageBox.Show("Please select a window configuration.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if(!selectedConfig.IsEnabled) { MessageBox.Show($"Configuration for '{selectedConfig.ProcessName}' is disabled.", "Action Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        var windowInfo = _windowActionService.FindManagedWindow(selectedConfig);
        if(windowInfo?.GetProcess() == null || windowInfo.GetProcess().HasExited) { MessageBox.Show($"App '{selectedConfig.ProcessName}' not running.", "Not Running", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        string appIdentifier = $"{selectedConfig.ProcessName} (PID: {windowInfo.GetProcess().Id})";
        DialogResult dr = MessageBox.Show($"Close '{appIdentifier}'?\nForce kill if graceful close fails/times out?", "Confirm Close", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if(dr == DialogResult.Cancel) return;

        bool success = _windowActionService.CloseApp(selectedConfig, dr == DialogResult.Yes, 2000);
        MessageBox.Show(success ? $"Close attempt for '{appIdentifier}' initiated." : $"Failed to close '{appIdentifier}'.", success ? "Close Attempted" : "Close Failed", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }

    void buttonCloseAllProfileApps_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!_selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        DialogResult dr = MessageBox.Show($"Close all apps in profile '{_selectedProfileForEditing.Name}'?\nForce kill if graceful close fails?", "Confirm Close All", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if(dr == DialogResult.Cancel) return;
        _windowActionService.ProcessAllAppsInProfile(_selectedProfileForEditing, false, false, true, dr == DialogResult.Yes, 1500);
    }

    void buttonTestSelectedProfile_Click(object sender, EventArgs e)
    {
        if(_selectedProfileForEditing == null) { MessageBox.Show("Select a profile to test.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!_selectedProfileForEditing.WindowConfigs.Any(wc => wc.IsEnabled)) { MessageBox.Show($"Profile '{_selectedProfileForEditing.Name}' has no enabled configs.", "No Configs", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        _windowMonitorService.TestProfileLayout(_selectedProfileForEditing);
    }

    void buttonAddProfile_Click(object sender, EventArgs e)
    {
        var newProfile = ProfileUIManager.HandleAddProfile(_appSettings.Profiles);
        if(newProfile != null)
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, _appSettings.ActiveProfileName);
            comboBoxActiveProfile.SelectedItem = newProfile;
        }
    }

    void buttonRemoveProfile_Click(object sender, EventArgs e)
    {
        string newActiveProfileName = ProfileUIManager.HandleRemoveProfile(_appSettings.Profiles, _selectedProfileForEditing, _appSettings.ActiveProfileName);
        if(newActiveProfileName != null)
        {
            _appSettings.ActiveProfileName = newActiveProfileName;
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, _appSettings.ActiveProfileName);
        }
        else if(_appSettings.Profiles.Contains(_selectedProfileForEditing))
        {
        }
        else
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, _appSettings.ActiveProfileName);
        }
    }

    void buttonRenameProfile_Click(object sender, EventArgs e)
    {
        string newActiveProfileName = ProfileUIManager.HandleRenameProfile(_appSettings.Profiles, _selectedProfileForEditing, _appSettings.ActiveProfileName);
        if(newActiveProfileName != null)
        {
            _appSettings.ActiveProfileName = newActiveProfileName;
        }
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, _appSettings.ActiveProfileName);
    }

    void buttonCloneProfile_Click(object sender, EventArgs e)
    {
        var clonedProfile = ProfileUIManager.HandleCloneProfile(_appSettings.Profiles, _selectedProfileForEditing);
        if(clonedProfile != null)
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, _appSettings.Profiles, _appSettings.ActiveProfileName);
            comboBoxActiveProfile.SelectedItem = clonedProfile;
        }
    }

    void comboBoxActiveProfile_SelectedIndexChanged(object sender, EventArgs e) { if(_isFormLoaded) HandleActiveProfileChange(); }

    void buttonRemoveWindowConfig_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(_selectedProfileForEditing == null || selectedConfig == null) { MessageBox.Show("Select a window configuration to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl) bl.Remove(selectedConfig);
        UpdateAllButtonStates();
    }

    void buttonFetchPosition_Click(object sender, EventArgs e) => FetchWindowProperty(config => FindWindowForConfig(config), (config, rect) => { config.TargetX = rect.Left; config.TargetY = rect.Top; }, "position");
    void buttonFetchSize_Click(object sender, EventArgs e) => FetchWindowProperty(config => FindWindowForConfig(config), (config, rect) => { config.TargetWidth = rect.Width; config.TargetHeight = rect.Height; }, "size");

    void FetchWindowProperty(Func<WindowConfig, IntPtr> findFunc, Action<WindowConfig, RECT> updateAction, string propName)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedConfig == null) return;
        IntPtr hWnd = findFunc(selectedConfig);
        if(hWnd == IntPtr.Zero) { MessageBox.Show($"Window not found for '{selectedConfig.ProcessName}'. Ensure app is running.", "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(Native.GetWindowRect(hWnd, out RECT rect))
        {
            updateAction(selectedConfig, rect);
            if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl) bl.ResetItem(bl.IndexOf(selectedConfig)); else dataGridViewWindowConfigs.Refresh();
        }
        else MessageBox.Show($"Could not get window {propName}.", "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void buttonSaveChanges_Click(object sender, EventArgs e) => SaveAppSettings();
    void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) { try { this.linkLabelGitHub.LinkVisited = true; Process.Start(new ProcessStartInfo("https://github.com/BitSwapper") { UseShellExecute = true }); } catch(Exception ex) { MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
    void checkBoxDisableProgram_CheckedChanged(object sender, EventArgs e) => HandleDisableProgramActivityChanged();

    void buttonActivateLaunchApp_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedConfig == null) { MessageBox.Show("Select a window configuration.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if(!selectedConfig.IsEnabled) { MessageBox.Show($"Config for '{selectedConfig.ProcessName}' is disabled.", "Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        if(!_windowActionService.ActivateOrLaunchApp(selectedConfig) && _windowActionService.FindManagedWindow(selectedConfig)?.HWnd != IntPtr.Zero)
            MessageBox.Show($"Failed to focus window for '{selectedConfig.ProcessName}'.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void settingsToolStripMenuItem_Click(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void notifyIconMain_DoubleClick(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void FormMain_FormClosing(object sender, FormClosingEventArgs e) { if(e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain); } }
    protected override void WndProc(ref Message m) { if(TrayIconUIManager.HandleMinimizeToTray(ref m, () => TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain))) return; base.WndProc(ref m); }
    void FormMain_Shown(object sender, EventArgs e) { if(this.WindowState == FormWindowState.Minimized && (this.ShowInTaskbar == false || this.Visible == false)) TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain); else if(notifyIconMain != null && this.Visible) notifyIconMain.Visible = false; }
    void exitToolStripMenuItem_Click(object sender, EventArgs e) => ForceExitApplication();
    void ForceExitApplication() { _windowMonitorService?.Dispose(); TrayIconUIManager.DisposeNotifyIcon(notifyIconMain); notifyIconMain = null; Environment.Exit(0); }
    void dataGridViewWindowConfigs_CellEndEdit(object sender, DataGridViewCellEventArgs e) { }

    void buttonFocus_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedConfig == null)
        {
            MessageBox.Show("Please select a window configuration from the list to focus.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if(!selectedConfig.IsEnabled)
        {
            MessageBox.Show($"This configuration for '{selectedConfig.ProcessName}' is disabled. No action taken.", "Action Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var windowInfo = _windowActionService.FindManagedWindow(selectedConfig);

        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{selectedConfig.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Attempting to focus.");
            bool success = _windowActionService.BringWindowToForeground(windowInfo.HWnd);
            if(!success)
            {
                MessageBox.Show($"Failed to bring the window for '{selectedConfig.ProcessName}' to the foreground.\nThis can happen if another application is actively preventing focus changes or if the window is minimized in a way that prevents normal activation.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show($"Application '{selectedConfig.ProcessName}' is not running or its window could not be found based on the current configuration.", "Not Running or Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}