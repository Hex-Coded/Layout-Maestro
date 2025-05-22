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


    #region Form Init and Close

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

    void FormMain_Load(object sender, EventArgs e)
    {
        isFormLoaded = false;

        InitializeCoreServicesAndSettings();
        InitializeUIComponents();
        LoadInitialProfileData();
        RegisterCustomEventHandlers();
        FinalizeFormLoad();
    }

    void InitializeCoreServicesAndSettings()
    {
        LoadAppSettingsAndEnsureDefaults();
        windowMonitorService.LoadAndApplySettings();
    }

    void InitializeUIComponents()
    {
        WindowConfigGridUIManager.InitializeDataGridView(dataGridViewWindowConfigs);
        StartupOptionsUIManager.InitializeComboBox(comboBoxStartupOptions);

        foreach(DataGridViewColumn column in dataGridViewWindowConfigs.Columns)
        {
            if(column.DataPropertyName == nameof(WindowConfig.WindowTitleHint))
            {
                column.HeaderText = "Window Title (Starts With)";
                column.ToolTipText = "The window title must start with this text (case-insensitive). E.g., 'MyGame' for 'MyGame - 01/01/2024'.";
                break;
            }
        }
        UpdateUIFromSettings();
    }

    void LoadInitialProfileData()
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

    void RegisterCustomEventHandlers() => dataGridViewWindowConfigs.CellValueChanged += dataGridViewWindowConfigs_CellValueChanged;

    void FinalizeFormLoad()
    {
        UpdateAllButtonStates();
        windowMonitorService.InitializeTimer();
        isFormLoaded = true;
        HandleDisableProgramActivityChanged();
    }

    void FormMain_FormClosed(object sender, FormClosedEventArgs e) => WindowActionService.AppLaunchedForPositioning -= windowMonitorService.NotifyAppLaunched;

    void ForceExitApplication()
    {
        windowMonitorService?.Dispose();
        TrayIconUIManager.DisposeNotifyIcon(notifyIconMain);
        WindowActionService.AppLaunchedForPositioning -= windowMonitorService.NotifyAppLaunched;
        notifyIconMain = null;
        Application.Exit();
    }
    #endregion

    #region Settings Management
    void LoadAppSettingsAndEnsureDefaults()
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
        }
        appSettings.StartupOption = newlySelectedStartupOption;

        appSettings.DisableProgramActivity = checkBoxDisableProgram.Checked;

        settingsManager.SaveSettings(appSettings);
        windowMonitorService.LoadAndApplySettings();

        windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        Debug.WriteLine($"SaveAppSettings: Profile '{selectedProfileForEditing?.Name ?? "null"}' re-passed to WindowMonitorService after save.");

        MessageBox.Show("Settings saved.", "Window Positioner", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    void UpdateUIFromSettings()
    {
        StartupOptionsUIManager.SelectCurrentOption(comboBoxStartupOptions, startupManager.GetCurrentStartupType());
        checkBoxDisableProgram.Checked = appSettings.DisableProgramActivity;
    }
    #endregion

    #region UI Update Methods
    void UpdateAllButtonStates()
    {
        UpdateProfileSpecificActionButtonsState();
        WindowConfigGridUIManager.UpdateSelectionDependentButtons(dataGridViewWindowConfigs, buttonRemoveWindowConfig, buttonActivateLaunchApp, buttonFocus, buttonMinimizeOne, buttonCloseApp, buttonFetchPosition, buttonFetchSize);
        ProfileUIManager.UpdateProfileManagementButtons(buttonRemoveProfile, buttonRenameProfile, buttonCloneProfile, buttonAddWindowConfig, selectedProfileForEditing != null, appSettings.Profiles.Count);
    }

    void UpdateProfileSpecificActionButtonsState() => ProfileUIManager.UpdateProfileSpecificActionButtons(buttonLaunchAllProfileApps, buttonFocusAllProfileApps, buttonHideAll, buttonCloseAllProfileApps, buttonTestSelectedProfile, selectedProfileForEditing);
    #endregion

    #region Profile Management & Selection
    void HandleActiveProfileChange()
    {
        if(!isFormLoaded) return;

        Profile selectedGuiProfile = comboBoxActiveProfile.SelectedItem as Profile;

        if(selectedProfileForEditing == selectedGuiProfile && selectedGuiProfile != null)
            return;

        selectedProfileForEditing = selectedGuiProfile;
        appSettings.ActiveProfileName = selectedGuiProfile?.Name ?? string.Empty;

        LoadWindowConfigsForCurrentProfile();
        windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        Debug.WriteLine($"HandleActiveProfileChange: Profile '{selectedProfileForEditing?.Name ?? "null"}' passed to WindowMonitorService.");
        UpdateAllButtonStates();
    }

    void LoadWindowConfigsForCurrentProfile() => WindowConfigGridUIManager.LoadWindowConfigsForProfile(dataGridViewWindowConfigs, groupBoxWindowConfigs, selectedProfileForEditing);

    void buttonAddProfile_Click(object sender, EventArgs e)
    {
        var newProfile = ProfileUIManager.HandleAddProfile(appSettings.Profiles);
        if(newProfile != null)
        {
            appSettings.ActiveProfileName = newProfile.Name;
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
        }
    }

    void buttonRemoveProfile_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) return;

        string currentActiveName = appSettings.ActiveProfileName;
        string newActiveProfileName = ProfileUIManager.HandleRemoveProfile(appSettings.Profiles, selectedProfileForEditing, currentActiveName);

        appSettings.ActiveProfileName = newActiveProfileName ?? (appSettings.Profiles.FirstOrDefault()?.Name ?? string.Empty);
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
    }

    void buttonRenameProfile_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) return;

        string newActiveProfileNameAfterRename = ProfileUIManager.HandleRenameProfile(appSettings.Profiles, selectedProfileForEditing, appSettings.ActiveProfileName);

        if(newActiveProfileNameAfterRename != null)
        {
            appSettings.ActiveProfileName = newActiveProfileNameAfterRename;
        }
        ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
    }

    void buttonCloneProfile_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) return;

        var clonedProfile = ProfileUIManager.HandleCloneProfile(appSettings.Profiles, selectedProfileForEditing);
        if(clonedProfile != null)
        {
            appSettings.ActiveProfileName = clonedProfile.Name;
            ProfileUIManager.PopulateActiveProfileComboBox(comboBoxActiveProfile, appSettings.Profiles, appSettings.ActiveProfileName);
        }
    }

    void comboBoxActiveProfile_SelectedIndexChanged(object sender, EventArgs e)
    {
        if(isFormLoaded)
        {
            HandleActiveProfileChange();
        }
    }
    #endregion

    #region Profile-Wide Window Actions (Launch All, Focus All, etc.)

    async Task ExecuteProfileAppProcessingAsync(
       Button actionButton,
       Func<Task> processActionAsync,
       string operationNameForLog)
    {
        actionButton.Enabled = false;
        Debug.WriteLine($"FormMain: '{operationNameForLog}' action started for profile '{selectedProfileForEditing.Name}'.");

        try
        {
            await processActionAsync();
            Debug.WriteLine($"FormMain: '{operationNameForLog}' operation completed for profile '{selectedProfileForEditing.Name}'.");
        }
        catch(Exception ex)
        {
            Debug.WriteLine($"FormMain: Error during '{operationNameForLog}' for profile '{selectedProfileForEditing.Name}': {ex.Message}");
            MessageBox.Show($"An unexpected error occurred while trying to {operationNameForLog.ToLowerInvariant()} applications:\n{ex.Message}", $"{operationNameForLog} Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            actionButton.Enabled = true;
            UpdateProfileSpecificActionButtonsState();
        }
    }

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

        await ExecuteProfileAppProcessingAsync(
            buttonLaunchAllProfileApps,
            () => windowActionService.ProcessAllAppsInProfile(
                profile: selectedProfileForEditing,
                launchIfNotRunning: true,
                bringToForegroundIfRunning: false,
                closeIfRunning: false,
                supressErrorDialogsForBatch: true
            ),
            "Launch All Missing Apps"
        );
    }

    async void buttonFocusAllProfileApps_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        await ExecuteProfileAppProcessingAsync(
            buttonFocusAllProfileApps,
            () => windowActionService.ProcessAllAppsInProfile(
                selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: true,
                closeIfRunning: false
            ),
            "Focus All Profile Apps"
        );
    }

    async void buttonHideAll_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Select a profile.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!selectedProfileForEditing.WindowConfigs.Any(c => c.IsEnabled)) { MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configs.", "No Action", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        await ExecuteProfileAppProcessingAsync(
            buttonHideAll,
            () => windowActionService.ProcessAllAppsInProfile(
                selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: false,
                closeIfRunning: false,
                minimizeIfFound: true
            ),
            "Hide All Profile Apps"
        );
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
            $"Are you sure you want to attempt to close all enabled applications in profile '{selectedProfileForEditing.Name}'?\n\nChoose 'Yes' to force kill if graceful close fails/times out.\nChoose 'No' for graceful close attempt only.",
            "Confirm Close All Profile Apps",
            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if(dr == DialogResult.Cancel)
        {
            Debug.WriteLine("FormMain: 'Close All Profile Apps' action cancelled by user.");
            return;
        }

        bool forceKill = (dr == DialogResult.Yes);

        await ExecuteProfileAppProcessingAsync(
            buttonCloseAllProfileApps,
            () => windowActionService.ProcessAllAppsInProfile(
                profile: selectedProfileForEditing,
                launchIfNotRunning: false,
                bringToForegroundIfRunning: false,
                closeIfRunning: true,
                forceKillIfNotClosed: forceKill,
                closeGracePeriodMs: 1500,
                supressErrorDialogsForBatch: true
            ),
            "Close All Profile Apps"
        );
    }

    void buttonTestSelectedProfile_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null) { MessageBox.Show("Select a profile to test.", "No Profile", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
        if(!selectedProfileForEditing.WindowConfigs.Any(wc => wc.IsEnabled)) { MessageBox.Show($"Profile '{selectedProfileForEditing.Name}' has no enabled configs.", "No Configs", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }

        windowMonitorService.TestProfileLayout(selectedProfileForEditing);
    }
    #endregion

    #region Window Configuration Management (DataGridView related)
    void dataGridViewWindowConfigs_SelectionChanged(object sender, EventArgs e)
    {
        if(isFormLoaded)
            UpdateAllButtonStates();
    }

    void dataGridViewWindowConfigs_CellValueChanged(object sender, DataGridViewCellEventArgs e)
    {
        if(!isFormLoaded || e.RowIndex < 0 || selectedProfileForEditing == null) return;

        DataGridViewColumn changedColumn = dataGridViewWindowConfigs.Columns[e.ColumnIndex];
        if(dataGridViewWindowConfigs.Rows[e.RowIndex].DataBoundItem is WindowConfig changedConfig)
        {
            string propertyName = changedColumn.DataPropertyName;
            if(propertyName == nameof(WindowConfig.AutoRelaunchEnabled) ||
                propertyName == nameof(WindowConfig.IsEnabled) ||
                propertyName == nameof(WindowConfig.ProcessName) ||
                propertyName == nameof(WindowConfig.ExecutablePath) ||
                propertyName == nameof(WindowConfig.WindowTitleHint))
            {
                Debug.WriteLine($"DGV CellValueChanged: {propertyName} for '{changedConfig.ProcessName}'. Notifying monitor service.");
                windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
            }
        }
    }

    void buttonAddWindowConfig_Click(object sender, EventArgs e)
    {
        if(selectedProfileForEditing == null)
        {
            MessageBox.Show("Please select or create a profile first.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var formSelectProcess = new FormSelectProcess();
        if(formSelectProcess.ShowDialog(this) != DialogResult.OK) return;

        Process selectedProcess = formSelectProcess.SelectedProcess;
        IntPtr selectedHWnd = formSelectProcess.SelectedWindowHandle;
        string selectedTitle = formSelectProcess.SelectedWindowTitle;

        if(selectedProcess == null || selectedHWnd == IntPtr.Zero) return;

        try
        {
            if(selectedProcess.HasExited)
            {
                ShowProcessOrWindowError(selectedProcess.ProcessName, "The selected process has exited.", "Process Exited");
                return;
            }
            if(!Native.GetWindowRect(selectedHWnd, out RECT currentRect))
            {
                ShowProcessOrWindowError(selectedProcess.ProcessName, "Could not get window dimensions. The window might have closed or is inaccessible.", "Window Error");
                return;
            }
            if(currentRect.Width <= 0 || currentRect.Height <= 0)
            {
                ShowProcessOrWindowError(selectedProcess.ProcessName, "Selected window has invalid dimensions (e.g., 0x0). If the target application is running as admin, this program might also need to run as admin.", "Dimension Error");
                return;
            }

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
        catch(ArgumentException argEx)
        {
            ShowProcessOrWindowError(selectedProcess?.ProcessName ?? "Selected Process", $"Process is no longer running or is inaccessible: {argEx.Message}", "Process Exited or Inaccessible");
        }
        catch(Exception ex)
        {
            ShowProcessOrWindowError(selectedProcess?.ProcessName ?? "Selected Process", $"An error occurred while processing the selected window: {ex.Message}", "Processing Error");
            Debug.WriteLine($"Error adding window config: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            selectedProcess?.Dispose();
        }
    }

    string GetExecutablePathSafe(Process process)
    {
        try
        {
            if(process.HasExited) return string.Empty;
            return process.MainModule?.FileName;
        }
        catch(System.ComponentModel.Win32Exception ex)
        {
            Debug.WriteLine($"Could not get ExecutablePath for {process.ProcessName} (PID: {process.Id}): {ex.Message}. This often occurs with elevated processes when this app is not elevated.");
            return string.Empty;
        }
        catch(InvalidOperationException ex)
        {
            Debug.WriteLine($"Could not get ExecutablePath for {process.ProcessName} (PID: {process.Id}) (may have exited or other issue): {ex.Message}");
            return string.Empty;
        }
    }

    bool DetermineElevation(Process process, string executablePath)
    {
        if(process.HasExited) return false;

        bool isElevated = ProcessPrivilegeChecker.IsProcessElevated(process.Id, out bool accessDenied);

        if(accessDenied && !isElevated && !ProcessPrivilegeChecker.IsCurrentProcessElevated())
        {
            Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {process.ProcessName} due to access denied from non-admin WPM during elevation check.");
            return true;
        }

        if(string.IsNullOrEmpty(executablePath) && !ProcessPrivilegeChecker.IsCurrentProcessElevated())
        {
            try { var _ = process.MainModule?.FileName; }
            catch(System.ComponentModel.Win32Exception ex) when(ex.NativeErrorCode == 5)
            {
                Debug.WriteLine($"Heuristically setting LaunchAsAdmin for {process.ProcessName} due to Win32Exception (Access Denied) accessing MainModule.");
                return true;
            }
            catch(InvalidOperationException) { }
        }
        return isElevated;
    }

    void ShowProcessOrWindowError(string processName, string message, string title = "Error") => MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);

    void buttonRemoveWindowConfig_Click(object sender, EventArgs e)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedProfileForEditing == null || selectedConfig == null)
        {
            MessageBox.Show("Select a window configuration to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bindingList)
        {
            bindingList.Remove(selectedConfig);
            windowMonitorService.UpdateActiveProfileReference(selectedProfileForEditing);
        }
        UpdateAllButtonStates();
    }
    #endregion

    #region Misc UI Event Handlers
    void buttonSaveChanges_Click(object sender, EventArgs e) => SaveAppSettings();

    void linkLabelGitHub_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        try
        {
            this.linkLabelGitHub.LinkVisited = true;
            Process.Start(new ProcessStartInfo("https://github.com/BitSwapper") { UseShellExecute = true });
        }
        catch(Exception ex)
        {
            MessageBox.Show($"Could not open link: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    void checkBoxDisableProgram_CheckedChanged(object sender, EventArgs e)
    {
        if(isFormLoaded && checkBoxDisableProgram.IsHandleCreated)
        {
            HandleDisableProgramActivityChanged();
        }
    }

    void HandleDisableProgramActivityChanged()
    {
        bool isProgramActivityDisabled = checkBoxDisableProgram.Checked;
        windowMonitorService.SetPositioningActive(!isProgramActivityDisabled);
        GeneralUIManager.UpdateProgramActivityUI(checkBoxDisableProgram, groupBoxWindowConfigs, groupBoxProfiles, isProgramActivityDisabled);
    }

    void dataGridViewWindowConfigs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
    {
    }
    #endregion

    #region Single Window Configuration Actions
    WindowConfig GetValidatedSelectedWindowConfig(bool checkIsEnabled = true)
    {
        var selectedConfig = WindowConfigGridUIManager.GetSelectedWindowConfig(dataGridViewWindowConfigs);
        if(selectedConfig == null)
        {
            MessageBox.Show("Please select a window configuration from the list.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        if(checkIsEnabled && !selectedConfig.IsEnabled)
        {
            MessageBox.Show($"This configuration for '{selectedConfig.ProcessName}' is disabled. No action taken.", "Action Skipped", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        return selectedConfig;
    }

    void buttonActivateLaunchApp_Click(object sender, EventArgs e)
    {
        var selectedConfig = GetValidatedSelectedWindowConfig();
        if(selectedConfig == null) return;

        bool success = windowActionService.ActivateOrLaunchApp(selectedConfig);
        if(!success && windowActionService.FindManagedWindow(selectedConfig)?.HWnd != IntPtr.Zero)
        {
            MessageBox.Show($"Failed to focus window for '{selectedConfig.ProcessName}'. It might be unresponsive or minimized in a way that prevents normal activation.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    void buttonFocus_Click(object sender, EventArgs e)
    {
        var selectedConfig = GetValidatedSelectedWindowConfig();
        if(selectedConfig == null) return;

        var windowInfo = windowActionService.FindManagedWindow(selectedConfig);
        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{selectedConfig.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Attempting to focus.");
            if(!windowActionService.BringWindowToForeground(windowInfo.HWnd))
            {
                MessageBox.Show($"Failed to bring the window for '{selectedConfig.ProcessName}' to the foreground.\nThis can happen if another application is actively preventing focus changes or if the window is minimized/unresponsive.", "Focus Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show($"Application '{selectedConfig.ProcessName}' is not running or its window could not be found based on the current configuration (check Window Title Hint).", "Not Running or Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void buttonMinimizeOne_Click(object sender, EventArgs e)
    {
        var selectedConfig = GetValidatedSelectedWindowConfig();
        if(selectedConfig == null) return;

        var windowInfo = windowActionService.FindManagedWindow(selectedConfig);
        if(windowInfo != null && windowInfo.HWnd != IntPtr.Zero)
        {
            Debug.WriteLine($"App '{selectedConfig.ProcessName}' (hWnd:{windowInfo.HWnd}) found. Attempting to minimize.");
            if(!windowActionService.BringWindowToBackground(windowInfo.HWnd))
            {
                MessageBox.Show($"Failed to minimize the window for '{selectedConfig.ProcessName}'.", "Minimize Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        else
        {
            MessageBox.Show($"Application '{selectedConfig.ProcessName}' is not running or its window could not be found.", "Not Running or Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    void buttonCloseApp_Click(object sender, EventArgs e)
    {
        var selectedConfig = GetValidatedSelectedWindowConfig();
        if(selectedConfig == null) return;

        var windowInfo = windowActionService.FindManagedWindow(selectedConfig);
        if(windowInfo?.GetProcess() == null || windowInfo.GetProcess().HasExited)
        {
            MessageBox.Show($"Application '{selectedConfig.ProcessName}' is not currently running.", "Not Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string appIdentifier = $"{selectedConfig.ProcessName} (PID: {windowInfo.GetProcess().Id})";
        DialogResult dr = MessageBox.Show($"Close '{appIdentifier}'?\n\nChoose 'Yes' to force kill if graceful close fails/times out after a short period.\nChoose 'No' for a graceful close attempt only.",
            "Confirm Close Application", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if(dr == DialogResult.Cancel) return;

        bool forceKillIfNeeded = (dr == DialogResult.Yes);
        bool success = windowActionService.CloseApp(selectedConfig, forceKillIfNeeded, 2000);

        if(success)
        {
            Debug.WriteLine($"Successfully initiated close for '{appIdentifier}'. Force kill if needed: {forceKillIfNeeded}");
        }
        else
        {
            Debug.WriteLine($"CloseApp returned false for '{appIdentifier}'. It might already be closed or failed to close gracefully without force.");
        }
    }

    void FetchWindowProperty(Func<WindowConfig, IntPtr> findWindowHandleFunc, Action<WindowConfig, RECT> updateConfigAction, string propertyNameForMessages)
    {
        var selectedConfig = GetValidatedSelectedWindowConfig(checkIsEnabled: false);
        if(selectedConfig == null) return;

        IntPtr hWnd = findWindowHandleFunc(selectedConfig);
        if(hWnd == IntPtr.Zero)
        {
            MessageBox.Show($"Window not found for '{selectedConfig.ProcessName}'. Ensure the application is running and the Window Title Hint is correct.", "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if(Native.GetWindowRect(hWnd, out RECT rect))
        {
            if(propertyNameForMessages == "size" && (rect.Width <= 0 || rect.Height <= 0))
            {
                MessageBox.Show($"Fetched window size for '{selectedConfig.ProcessName}' is invalid (e.g., {rect.Width}x{rect.Height}). The window might be minimized or in an unusual state. If it's an admin app, run this as admin.", "Invalid Size Fetched", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            updateConfigAction(selectedConfig, rect);
            if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bindingList)
            {
                bindingList.ResetItem(bindingList.IndexOf(selectedConfig));
            }
            else
            {
                dataGridViewWindowConfigs.Refresh();
            }
        }
        else
        {
            MessageBox.Show($"Could not get window {propertyNameForMessages} for '{selectedConfig.ProcessName}'. The window might be inaccessible (e.g., an elevated process and this app is not).", $"Fetch {propertyNameForMessages} Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    IntPtr FindWindowForConfig(WindowConfig config) => (config == null) ? IntPtr.Zero : (WindowEnumerationService.FindMostSuitableWindow(config)?.HWnd ?? IntPtr.Zero);

    void buttonFetchPosition_Click(object sender, EventArgs e) => FetchWindowProperty(
           findWindowHandleFunc: FindWindowForConfig,
           updateConfigAction: (config, rect) => { config.TargetX = rect.Left; config.TargetY = rect.Top; },
           propertyNameForMessages: "position"
       );

    void buttonFetchSize_Click(object sender, EventArgs e) => FetchWindowProperty(
           findWindowHandleFunc: FindWindowForConfig,
           updateConfigAction: (config, rect) => { config.TargetWidth = rect.Width; config.TargetHeight = rect.Height; },
           propertyNameForMessages: "size"
       );
    #endregion
    
    #region Tray Icon and Form Visibility
    void settingsToolStripMenuItem_Click(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void notifyIconMain_DoubleClick(object sender, EventArgs e) => TrayIconUIManager.ShowFormFromTrayIcon(this, notifyIconMain);
    void exitToolStripMenuItem_Click(object sender, EventArgs e) => ForceExitApplication();

    void FormMain_FormClosing(object sender, FormClosingEventArgs e)
    {
        if(e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain);
        }
    }

    protected override void WndProc(ref Message m)
    {
        if(TrayIconUIManager.HandleMinimizeToTray(ref m, () => TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain)))
        {
            return;
        }
        base.WndProc(ref m);
    }

    void FormMain_Shown(object sender, EventArgs e)
    {
        if(this.WindowState == FormWindowState.Minimized && (!this.ShowInTaskbar || !this.Visible))
        {
            TrayIconUIManager.HideFormAndShowTrayIcon(this, notifyIconMain);
        }
        else if(notifyIconMain != null && this.Visible)
        {
        }
    }
    #endregion
}