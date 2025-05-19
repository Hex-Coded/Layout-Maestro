using System.Diagnostics;
using WindowPlacementManager.Helpers;
using WindowPlacementManager.Models;
using WindowPlacementManager.Services;

namespace WindowPlacementManager;

public partial class FormMain : Form
{
    readonly SettingsManager settingsManager;
    readonly StartupManager startupManager;
    readonly WindowMonitorService windowMonitorService;
    readonly WindowActionService windowActionService;

    AppSettingsData appSettings;
    Profile selectedProfileForEditing;
    bool isFormLoaded = false;

    public FormMain()
    {
        InitializeComponent();
        settingsManager = new SettingsManager();
        startupManager = new StartupManager();
        windowActionService = new WindowActionService();
        windowMonitorService = new WindowMonitorService(settingsManager, windowActionService);

        WindowActionService.AppLaunchedForPositioning += windowMonitorService.NotifyAppLaunched;

        TrayIconUIManager.InitializeNotifyIcon(this.notifyIconMain);
    }

    void FormMain_FormClosed(object sender, FormClosedEventArgs e) => WindowActionService.AppLaunchedForPositioning -= windowMonitorService.NotifyAppLaunched;


    async void buttonLaunchAllProfileApps_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select a profile to launch apps from.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if(!selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled))
        {
            MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configurations to launch.", "No Enabled Configurations", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Debug.WriteLine($"FormMain: 'Launch All Missing' button clicked for profile '{selectedProfileForEditing.Name}'.");
        buttonLaunchAllProfileApps.Enabled = false;

        try
        {
            await windowActionService.ProcessAllAppsInProfile(
                profile: selectedProfileForEditing,
                launchIfNotRunning: true,
                bringToForegroundIfRunning: false,
                closeIfRunning: false,
                supressErrorDialogsForBatch: true
            );
            Debug.WriteLine($"FormMain: 'Launch All Missing' operation completed for profile '{selectedProfileForEditing.Name}'.");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"FormMain: Error during 'Launch All Missing' operation for profile '{selectedProfileForEditing.Name}': {ex.Message}");
            MessageBox.Show($"An unexpected error occurred while trying to launch missing applications:\n{ex.Message}", "Launch Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            buttonLaunchAllProfileApps.Enabled = true;
            ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, selectedProfileForEditing);
        }
    }





    async void buttonFocusAllProfileApps_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        buttonFocusAllProfileApps.Enabled = false;
        try
        {
            await windowActionService.ProcessAllAppsInProfile(selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: true,
                closeIfRunning: false);
        }
        finally
        {
            buttonFocusAllProfileApps.Enabled = true;
            ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, selectedProfileForEditing);
        }
    }

    void UpdateAllButtonStates()
    {
        ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, selectedProfileForEditing);
        WindowConfigGridUIManager.UpdateSelectionDependentButtons(dataGridViewWindowConfigs, buttonRemoveWindowConfig, buttonActivateLaunchApp, buttonFocus, buttonCloseApp, buttonFetchPosition, buttonFetchSize);
        ProfileUIManager.UpdateProfileManagementButtons(buttonRemoveProfile, buttonRenameProfile, buttonCloneProfile, buttonAddWindowConfig, selectedProfileForEditing != null, appSettings.Profiles.Count);
    }

    void dataGridViewWindowConfigs_SelectionChanged(object sender, EventArgs e) => UpdateAllButtonStates();

    void FormMain_Load(object sender, EventArgs e)
    {
        isFormLoaded = false;

        LoadAppSettingsAndInitializeMonitorServiceGlobals();

        WindowConfigGridUIManager.InitializeDataGridView(dataGridViewWindowConfigs);
        StartupOptionsUIManager.InitializeComboBox(comboBoxStartupOptions);

        SetupInitialActiveProfile();

        UpdateUIFromSettings();

        UpdateAllButtonStates();

        dataGridViewWindowConfigs.CellValueChanged += dataGridViewWindowConfigs_CellValueChanged;

        windowMonitorService.InitializeTimer();

        isFormLoaded = true;

        HandleDisableProgramActivityChanged();
    }

    void LoadAppSettingsAndInitializeMonitorServiceGlobals()
    {
        appSettings = settingsManager.LoadSettings();
        if(!appSettings.Profiles.Any())
        {
            var defaultProfile = new Profile("Default Profile");
            appSettings.Profiles.Add(defaultProfile);
            appSettings.ActiveProfileName = defaultProfile.Name;
            settingsManager.SaveSettings(appSettings);
            appSettings = settingsManager.LoadSettings();
        }
        windowMonitorService.LoadAndApplySettings();
    }

    void SetupInitialActiveProfile()
    {
        string initialActiveProfileName = windowMonitorService.GetActiveProfileNameFromSettings();
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, initialActiveProfileName);

        selectedProfileForEditing = comboBoxActiveProfile.SelectedItem as Profile;

        if(selectedProfileForEditing == null && appSettings.Profiles.Any())
        {
            selectedProfileForEditing = appSettings.Profiles.First();
            comboBoxActiveProfile.SelectedItem = selectedProfileForEditing;
        }

        appSettings.ActiveProfileName = selectedProfileForEditing?.Name ?? string.Empty;

        LoadWindowConfigsForCurrentProfile();

        windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        Debug.WriteLine($"FormMain_Load: Initial active profile '{selectedProfileForEditing?.Name ?? "null"}' passed to WindowMonitorService.");
    }


    void HandleActiveProfileChange()
    {
        if(!isFormLoaded) return;

        Profile selected = comboBoxActiveProfile.SelectedItem as Profile;
        if(selectedProfileForEditing == selected && selected != null)
        {
        }

        selectedProfileForEditing = selected;
        appSettings.ActiveProfileName = selected?.Name ?? string.Empty;

        LoadWindowConfigsForCurrentProfile();

        windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        Debug.WriteLine($"HandleActiveProfileChange: Profile '{selectedProfileForEditing?.Name ?? "null"}' passed to WindowMonitorService.");
        UpdateAllButtonStates();
    }

    void dataGridViewWindowConfigs_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if(!isFormLoaded || e.RowIndex < 0 || selectedProfileForEditing == null) return;

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
            windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        }
    }

    void SaveAppSettings()
    {
        if(!isFormLoaded) return;

        if(comboBoxActiveProfile.SelectedItem is Profile selectedActiveCbProfile)
            appSettings.ActiveProfileName = selectedActiveCbProfile.Name;
        else if(appSettings.Profiles.Any())
            appSettings.ActiveProfileName = appSettings.Profiles.First().Name;
        else
            appSettings.ActiveProfileName = string.Empty;


        StartupType newlySelectedStartupOption = StartupOptionsUIManager.GetSelectedStartupType(comboBoxStartupOptions);
        StartupType currentSystemStartupOption = startupManager.GetCurrentStartupType();

        if(newlySelectedStartupOption != currentSystemStartupOption)
        {
            Debug.WriteLine($"Startup option changed from {currentSystemStartupOption} to {newlySelectedStartupOption}. Applying change.");
            startupManager.SetStartup(newlySelectedStartupOption);
            appSettings.StartupOption = newlySelectedStartupOption;
        }
        else
        {
            appSettings.StartupOption = newlySelectedStartupOption;
            Debug.WriteLine($"Startup option ({newlySelectedStartupOption}) unchanged from current system state.");
        }

        appSettings.DisableProgramActivity = checkBoxDisableProgram.Checked;

        settingsManager.SaveSettings(appSettings);
        windowMonitorService.LoadAndApplySettings();

        windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        Debug.WriteLine($"SaveAppSettings: Profile '{selectedProfileForEditing?.Name ?? "null"}' re-passed to WindowMonitorService after save.");

        MessageBox.Show("Settings saved.", "Window Positioner", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void HandleDisableProgramActivityChanged()
    {
        if(!isFormLoaded && !checkBoxDisableProgram.IsHandleCreated) return;

        bool isDisabled = checkBoxDisableProgram.Checked;

        windowMonitorService.SetPositioningActive(!isDisabled);

        GeneralUIManager.UpdateProgramActivityUI(checkBoxDisableProgram, groupBoxWindowConfigs, groupBoxProfiles, isDisabled);
    }

    void UpdateUIFromSettings()
    {
        StartupOptionsUIManager.SelectCurrentOption(comboBoxStartupOptions, startupManager.GetCurrentStartupType());
        checkBoxDisableProgram.Checked = appSettings.DisableProgramActivity;
    }

    void LoadWindowConfigsForCurrentProfile() => WindowConfigGridUIManager.LoadWindowConfigsForProfile(dataGridViewWindowConfigs, groupBoxWindowConfigs, selectedProfileForEditing);

    void LoadAppSettings()
    {
        appSettings = settingsManager.LoadSettings();
        if(!appSettings.Profiles.Any())
        {
            var defaultProfile = new Profile("Default Profile");
            appSettings.Profiles.Add(defaultProfile);
            appSettings.ActiveProfileName = defaultProfile.Name;
        }
        windowMonitorService.LoadAndApplySettings();
    }

    IntPtr FindWindowForConfig(WindowConfig config) => (config == null) ? IntPtr.Zero : (WindowEnumerationService.FindMostSuitableWindow(config)?.HWnd ?? IntPtr.Zero);

    void buttonAddWindowConfig_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Please select or create a profile first.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }

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

            WindowConfigGridUIManager.AddAndSelectWindowConfig(dataGridViewWindowConfigs, selectedProfileForEditing, newConfig);
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

        var windowInfo = windowActionService.FindManagedWindow(selectedConfig);
        if(windowInfo?.GetProcess() == null || windowInfo.GetProcess().HasExited) { MessageBox.Show($"App '{selectedConfig.ProcessName}' not running.", "Not Running", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        string appIdentifier = $"{selectedConfig.ProcessName} (PID: {windowInfo.GetProcess().Id})";
        DialogResult dr = MessageBox.Show($"Close '{appIdentifier}'?\nForce kill if graceful close fails/times out?", "Confirm Close", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
        if(dr == DialogResult.Cancel) return;

        bool success = windowActionService.CloseApp(selectedConfig, dr == DialogResult.Yes, 2000);
        MessageBox.Show(success ? $"Close attempt for '{appIdentifier}' initiated." : $"Failed to close '{appIdentifier}'.", success ? "Close Attempted" : "Close Failed", MessageBoxButtons.OK, success ? MessageBoxIcon.Information : MessageBoxIcon.Error);
    }


    async void buttonCloseAllProfileApps_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select a profile to close apps from.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if(!selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled))
        {
            MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configurations to close.", "No Enabled Configurations", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        DialogResult dr = MessageBox.Show(
        $"Are you sure you want to attempt to close all enabled applications in the profile '{selectedProfileForEditing.Name}'?\n\nChoose 'Yes' to force kill if graceful close fails/times out.\nChoose 'No' for graceful close attempt only.",
        "Confirm Close All Profile Apps",
        MessageBoxButtons.YesNoCancel,
        MessageBoxIcon.Warning,
        MessageBoxDefaultButton.Button2
    );

        if(dr == DialogResult.Cancel)
        {
            Debug.WriteLine("FormMain: 'Close All Profile Apps' action cancelled by user.");
            return;
        }

        bool forceKill = (dr == DialogResult.Yes);
        Debug.WriteLine($"FormMain: 'Close All Profile Apps' button clicked for profile '{selectedProfileForEditing.Name}'. Force kill: {forceKill}");

        buttonCloseAllProfileApps.Enabled = false;

        try
        {
            await windowActionService.ProcessAllAppsInProfile(
                profile: selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: false,
                closeIfRunning: true,
                forceKillIfNotClosed: forceKill,
                closeGracePeriodMs: 1500,
                supressErrorDialogsForBatch: true
            );
            Debug.WriteLine($"FormMain: 'Close All Profile Apps' operation completed for profile '{selectedProfileForEditing.Name}'.");
            MessageBox.Show($"Attempted to close all enabled applications in profile '{selectedProfileForEditing.Name}'. Check debug logs for details.", "Operation Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"FormMain: Error during 'Close All Profile Apps' operation for profile '{selectedProfileForEditing.Name}': {ex.Message}");
            MessageBox.Show($"An unexpected error occurred while trying to close applications:\n{ex.Message}", "Close Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            buttonCloseAllProfileApps.Enabled = true;
            ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonCloseAllProfileApps, buttonTestSelectedProfile, selectedProfileForEditing);
        }
    }
    void buttonTestSelectedProfile_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Select a profile to test.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!selectedProfileForEditing.WindowConfigs.Any(wc => wc.IsEnabled)) { MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configs.", "No Configs", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        windowMonitorService.TestProfileLayout(selectedProfileForEditing);
    }

    void buttonAddProfile_Click(object sender, EventArgs e)
    {
        var newProfile = ProfileUIManager.HandleAddProfile(appSettings.Profiles);
        if(newProfile != null)
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
            comboBoxActiveProfile.SelectedItem = newProfile;
        }
    }

    void buttonRemoveProfile_Click(object sender, EventArgs e)
    {
        string newActiveProfileName = ProfileUIManager.HandleRemoveProfile(appSettings.Profiles, selectedProfileForEditing, appSettings.ActiveProfileName);
        if(newActiveProfileName != null)
        {
            appSettings.ActiveProfileName = newActiveProfileName;
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
        }
        else if(appSettings.Profiles.Contains(selectedProfileForEditing))
        {
        }
        else
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
        }
    }

    void buttonRenameProfile_Click(object sender, EventArgs e)
    {
        string newActiveProfileName = ProfileUIManager.HandleRenameProfile(appSettings.Profiles, selectedProfileForEditing, appSettings.ActiveProfileName);
        if(newActiveProfileName != null)
        {
            appSettings.ActiveProfileName = newActiveProfileName;
        }
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
    }

    void buttonCloneProfile_Click(object sender, EventArgs e)
    {
        var clonedProfile = ProfileUIManager.HandleCloneProfile(appSettings.Profiles, selectedProfileForEditing);
        if(clonedProfile != null)
        {
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
            comboBoxActiveProfile.SelectedItem = clonedProfile;
        }
    }

    void comboBoxActiveProfile_SelectedIndexChanged(object sender, EventArgs e) { if(isFormLoaded) HandleActiveProfileChange(); }

    void buttonRemoveWindowConfig_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedProfileForEditing == null || selectedConfig == null) { MessageBox.Show("Select a window configuration to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
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
        if(!windowActionService.ActivateOrLaunchApp(selectedConfig) && windowActionService.FindManagedWindow(selectedConfig)?.HWnd != IntPtr.Zero)
            MessageBox.Show($"Failed to focus window for '{selectedConfig.ProcessName}'.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    void settingsToolStripMenuItem_Click(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void notifyIconMain_DoubleClick(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void FormMain_FormClosing(object sender, FormClosingEventArgs e) { if(e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain); } }
    protected override void WndProc(ref Message m) { if(TrayIconUIManager.HandleMinimizeToTray(ref m, () => TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain))) return; base.WndProc(ref m); }
    void FormMain_Shown(object sender, EventArgs e) { if(this.WindowState == FormWindowState.Minimized && (this.ShowInTaskbar == false || this.Visible == false)) TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain); else if(notifyIconMain != null && this.Visible) notifyIconMain.Visible = false; }
    void exitToolStripMenuItem_Click(object sender, EventArgs e) => ForceExitApplication();

    void ForceExitApplication()
    {
        windowMonitorService?.Dispose();
        TrayIconUIManager.DisposeNotifyIcon(notifyIconMain);
        WindowActionService.AppLaunchedForPositioning -= windowMonitorService.NotifyAppLaunched;
        notifyIconMain = null; Environment.Exit(0);
    }

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

        var windowInfo = windowActionService.FindManagedWindow(selectedConfig);

        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{selectedConfig.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Attempting to focus.");
            bool success = windowActionService.BringWindowToForeground(windowInfo.HWnd);
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