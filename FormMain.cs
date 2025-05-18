using System;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Microsoft.VisualBasic;
using WindowPositioner.Models;
using WindowPositioner.Services;

namespace WindowPositioner
{
    public partial class FormMain : Form
    {
        private readonly SettingsManager _settingsManager;
        private readonly StartupManager _startupManager;
        private readonly WindowMonitorService _windowMonitorService;

        private AppSettingsData _appSettings;
        private Profile _selectedProfileForEditing;
        private bool _isFormLoaded = false;

        public FormMain()
        {
            InitializeComponent();

            _settingsManager = new SettingsManager();
            _startupManager = new StartupManager();
            _windowMonitorService = new WindowMonitorService(_settingsManager);

        }


        private void listBoxProfiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!_isFormLoaded) return;

            if(listBoxProfiles.SelectedItem is Profile selected)
            {
                _selectedProfileForEditing = selected;
            }
            else
            {
                if(_appSettings.Profiles.Any())
                {
                    _selectedProfileForEditing = _appSettings.Profiles.First();
                    if(listBoxProfiles.SelectedItem == null) listBoxProfiles.SelectedItem = _selectedProfileForEditing;
                }
                else
                {
                    _selectedProfileForEditing = null;
                }
            }
            LoadWindowConfigsForSelectedProfile();
        }
        private void PopulateProfilesList()
        {
            string previouslySelectedProfileNameInListBox = (listBoxProfiles.SelectedItem as Profile)?.Name;

            listBoxProfiles.BeginUpdate();
            listBoxProfiles.Items.Clear();
            listBoxProfiles.DisplayMember = nameof(Profile.Name);

            foreach(var profile in _appSettings.Profiles)
            {
                listBoxProfiles.Items.Add(profile);
            }
            listBoxProfiles.EndUpdate();

            Profile profileToSelectInListBox = null;

            if(!string.IsNullOrEmpty(previouslySelectedProfileNameInListBox))
            {
                profileToSelectInListBox = _appSettings.Profiles.FirstOrDefault(p => p.Name == previouslySelectedProfileNameInListBox);
            }

            if(profileToSelectInListBox == null && _selectedProfileForEditing != null)
            {
                profileToSelectInListBox = _appSettings.Profiles.FirstOrDefault(p => p.Name == _selectedProfileForEditing.Name);
            }

            if(profileToSelectInListBox == null && !string.IsNullOrEmpty(_appSettings.ActiveProfileName))
            {
                profileToSelectInListBox = _appSettings.Profiles.FirstOrDefault(p => p.Name == _appSettings.ActiveProfileName);
            }

            if(profileToSelectInListBox == null && _appSettings.Profiles.Any())
            {
                profileToSelectInListBox = _appSettings.Profiles.First();
            }

            if(profileToSelectInListBox != null)
            {
                listBoxProfiles.SelectedItem = profileToSelectInListBox;
            }

        }

        private void PopulateActiveProfileComboBox()
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


        private void FormMain_Load(object sender, EventArgs e)
        {
            _isFormLoaded = false;

            LoadSettings();
            InitializeDataGridView();

            PopulateProfilesList();
            PopulateActiveProfileComboBox();

            if(listBoxProfiles.SelectedItem is Profile currentListBoxSelection)
            {
                _selectedProfileForEditing = currentListBoxSelection;
            }
            else if(_appSettings.Profiles.Any())
            {
                _selectedProfileForEditing = _appSettings.Profiles.First();
                listBoxProfiles.SelectedItem = _selectedProfileForEditing;
            }
            else
            {
                _selectedProfileForEditing = null;
            }

            if(_selectedProfileForEditing != null)
            {
                LoadWindowConfigsForSelectedProfile();
            }
            else
            {
                dataGridViewWindowConfigs.DataSource = null;
                groupBoxWindowConfigs.Enabled = false;
            }

            UpdateUIFromSettings();
            dataGridViewWindowConfigs_SelectionChanged(null, null);

            _isFormLoaded = true;
        }

        private void FormMain_Shown(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized && this.ShowInTaskbar == false)
            {
                HideForm();
            }
        }


        private void InitializeDataGridView()
        {
            dataGridViewWindowConfigs.AutoGenerateColumns = false;
            dataGridViewWindowConfigs.Columns.Clear();

            var enabledCol = new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.IsEnabled), HeaderText = "On", Width = 40 };
            dataGridViewWindowConfigs.Columns.Add(enabledCol);

            var procNameCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.ProcessName), HeaderText = "Process Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 100 };
            dataGridViewWindowConfigs.Columns.Add(procNameCol);

            var titleHintCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.WindowTitleHint), HeaderText = "Window Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, MinimumWidth = 100 };
            dataGridViewWindowConfigs.Columns.Add(titleHintCol);

            var ctrlPosCol = new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.ControlPosition), HeaderText = "Pos?", ToolTipText="Control Position", Width = 40 };
            dataGridViewWindowConfigs.Columns.Add(ctrlPosCol);

            var xCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetX), HeaderText = "X", Width = 50 };
            dataGridViewWindowConfigs.Columns.Add(xCol);
            var yCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetY), HeaderText = "Y", Width = 50 };
            dataGridViewWindowConfigs.Columns.Add(yCol);

            var ctrlSizeCol = new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.ControlSize), HeaderText = "Size?", ToolTipText="Control Size", Width = 45 };
            dataGridViewWindowConfigs.Columns.Add(ctrlSizeCol);

            var wCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetWidth), HeaderText = "W", Width = 50 };
            dataGridViewWindowConfigs.Columns.Add(wCol);
            var hCol = new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetHeight), HeaderText = "H", Width = 50 };
            dataGridViewWindowConfigs.Columns.Add(hCol);

            xCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            yCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            wCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            hCol.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
        }

        private void LoadSettings()
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

        private void SaveSettings()
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

            _appSettings.StartWithWindows = checkBoxStartWithWindows.Checked;
            _settingsManager.SaveSettings(_appSettings);
            _startupManager.SetStartup(_appSettings.StartWithWindows);
            _windowMonitorService.LoadAndApplySettings();
        }

        private void UpdateUIFromSettings()
        {
            checkBoxStartWithWindows.Checked = _startupManager.IsStartupEnabled();
        }


        private void LoadWindowConfigsForSelectedProfile()
        {
            if(_selectedProfileForEditing != null)
            {
                var bindableList = new SortableBindingList<WindowConfig>(_selectedProfileForEditing.WindowConfigs);
                dataGridViewWindowConfigs.DataSource = bindableList;
                groupBoxWindowConfigs.Text = $"Window Configurations for '{_selectedProfileForEditing.Name}'";
                groupBoxWindowConfigs.Enabled = true;
            }
            else
            {
                dataGridViewWindowConfigs.DataSource = null;
                groupBoxWindowConfigs.Text = "Window Configurations (No Profile Selected)";
                groupBoxWindowConfigs.Enabled = false;
            }
            dataGridViewWindowConfigs_SelectionChanged(null, null);
        }

        private void ShowForm()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
            this.Activate();
        }

        private void HideForm()
        {
            this.Hide();
            this.ShowInTaskbar = false;
            if(notifyIconMain != null) notifyIconMain.Visible = true;
        }

        private void settingsToolStripMenuItem_Click(object sender, EventArgs e) => ShowForm();
        private void notifyIconMain_DoubleClick(object sender, EventArgs e) => ShowForm();

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _windowMonitorService?.Dispose();
            if(notifyIconMain != null)
            {
                notifyIconMain.Visible = false;
                notifyIconMain.Dispose();
            }
            Application.Exit();
        }

        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if(e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
            }
        }

        private void buttonAddProfile_Click(object sender, EventArgs e)
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
                PopulateProfilesList();
                PopulateActiveProfileComboBox();
                listBoxProfiles.SelectedItem = newProfile;
            }
        }

        private void buttonRemoveProfile_Click(object sender, EventArgs e)
        {
            if(listBoxProfiles.SelectedItem is Profile selectedProfile)
            {
                if(_appSettings.Profiles.Count <= 1)
                {
                    MessageBox.Show("Cannot remove the last profile.", "Action Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if(MessageBox.Show($"Are you sure you want to delete profile '{selectedProfile.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _appSettings.Profiles.Remove(selectedProfile);
                    if(_appSettings.ActiveProfileName == selectedProfile.Name)
                    {
                        _appSettings.ActiveProfileName = _appSettings.Profiles.FirstOrDefault()?.Name ?? string.Empty;
                    }
                    PopulateProfilesList();
                    PopulateActiveProfileComboBox();
                }
            }
            else
            {
                MessageBox.Show("Please select a profile to remove.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void buttonRenameProfile_Click(object sender, EventArgs e)
        {
            if(listBoxProfiles.SelectedItem is Profile selectedProfile)
            {
                string newName = Interaction.InputBox("Enter new name for profile:", "Rename Profile", selectedProfile.Name);
                if(!string.IsNullOrWhiteSpace(newName) && newName != selectedProfile.Name)
                {
                    if(_appSettings.Profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != selectedProfile))
                    {
                        MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    bool isActiveProfile = (_appSettings.ActiveProfileName == selectedProfile.Name);
                    string oldName = selectedProfile.Name;
                    selectedProfile.Name = newName;
                    if(isActiveProfile) _appSettings.ActiveProfileName = newName;

                    PopulateProfilesList();
                    PopulateActiveProfileComboBox();
                    listBoxProfiles.SelectedItem = selectedProfile;
                }
            }
        }

        private void comboBoxActiveProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            if(!_isFormLoaded) return;
            if(comboBoxActiveProfile.SelectedItem is Profile selectedActive)
            {
                _appSettings.ActiveProfileName = selectedActive.Name;
                _windowMonitorService.LoadAndApplySettings();
            }
        }


        private void buttonAddWindowConfig_Click(object sender, EventArgs e)
        {
            if(_selectedProfileForEditing == null)
            {
                if(_appSettings.Profiles.Any())
                {
                    _selectedProfileForEditing = _appSettings.Profiles.First();
                    listBoxProfiles.SelectedItem = _selectedProfileForEditing;
                }
                else
                {
                    MessageBox.Show("No profiles exist. Please create a profile first before adding window configurations.",
                                    "No Profile Exists", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            if(_selectedProfileForEditing == null)
            {
                MessageBox.Show("A profile must be selected or created before adding a window configuration.",
                                "Profile Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }


            using(var formSelectProcess = new FormSelectProcess())
            {
                if(formSelectProcess.ShowDialog(this) == DialogResult.OK)
                {
                    Process selectedProcess = formSelectProcess.SelectedProcess;
                    IntPtr selectedHWnd = formSelectProcess.SelectedWindowHandle;
                    string selectedTitle = formSelectProcess.SelectedWindowTitle;

                    if(selectedProcess != null && selectedHWnd != IntPtr.Zero)
                    {
                        try
                        {
                            try
                            {
                                Process.GetProcessById(selectedProcess.Id);
                            }
                            catch(ArgumentException)
                            {
                                MessageBox.Show($"The selected process '{selectedProcess.ProcessName}' (PID: {selectedProcess.Id}) is no longer running.",
                                                "Process Exited", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }


                            if(NativeMethods.GetWindowRect(selectedHWnd, out RECT currentRect))
                            {
                                if(currentRect.Width <= 0 || currentRect.Height <= 0)
                                {
                                    MessageBox.Show($"The selected window for '{selectedProcess.ProcessName}' has invalid dimensions (Width: {currentRect.Width}, Height: {currentRect.Height}). It might be minimized or hidden. Please ensure the window is visible and has a valid size.",
                                                    "Invalid Window Dimensions", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                    return;
                                }

                                var newConfig = new WindowConfig
                                {
                                    IsEnabled = true,
                                    ProcessName = selectedProcess.ProcessName,
                                    WindowTitleHint = selectedTitle,
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
                                        if(dataGridViewWindowConfigs.FirstDisplayedScrollingRowIndex > newRow.Index ||
                                            dataGridViewWindowConfigs.FirstDisplayedScrollingRowIndex + dataGridViewWindowConfigs.DisplayedRowCount(false) <= newRow.Index)
                                        {
                                            dataGridViewWindowConfigs.FirstDisplayedScrollingRowIndex = newRow.Index;
                                        }
                                    }
                                }
                            }
                            else
                            {
                                MessageBox.Show($"Could not get window dimensions for '{selectedProcess.ProcessName}'. The window might have closed or is inaccessible.",
                                                "Dimension Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        catch(Exception ex)
                        {
                            MessageBox.Show($"Error processing selected window: {ex.Message}",
                                            "Processing Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Debug.WriteLine($"Error adding window config from selected process: {ex.Message}");
                        }
                    }
                }
            }
        }

        private void buttonRemoveWindowConfig_Click(object sender, EventArgs e)
        {
            if(_selectedProfileForEditing != null && dataGridViewWindowConfigs.SelectedRows.Count > 0)
            {
                if(dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
                {
                    if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bindingList)
                    {
                        bindingList.Remove(selectedConfig);
                    }
                    else
                    {
                        _selectedProfileForEditing.WindowConfigs.Remove(selectedConfig);
                        LoadWindowConfigsForSelectedProfile();
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a window configuration to remove.", "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void dataGridViewWindowConfigs_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
        }

        private void dataGridViewWindowConfigs_SelectionChanged(object sender, EventArgs e)
        {
            bool rowSelected = dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
                               dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig;

            buttonFetchPosition.Enabled = rowSelected;
            buttonFetchSize.Enabled = rowSelected;
            buttonRemoveWindowConfig.Enabled = rowSelected;
        }

        private IntPtr FindWindowForConfig(WindowConfig config)
        {
            if(string.IsNullOrWhiteSpace(config.ProcessName)) return IntPtr.Zero;
            try
            {
                Process[] processes = Process.GetProcessesByName(config.ProcessName);
                if(!processes.Any()) return IntPtr.Zero;

                foreach(var proc in processes)
                {
                    if(proc.MainWindowHandle == IntPtr.Zero) continue;

                    if(!string.IsNullOrWhiteSpace(config.WindowTitleHint))
                    {
                        string currentTitle = NativeMethods.GetWindowTitle(proc.MainWindowHandle);
                        if(currentTitle.ToLower().Contains(config.WindowTitleHint.ToLower()))
                        {
                            return proc.MainWindowHandle;
                        }
                    }
                    else
                    {
                        return proc.MainWindowHandle;
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error finding window for config '{config.ProcessName}': {ex.Message}");
            }
            return IntPtr.Zero;
        }

        private void buttonFetchPosition_Click(object sender, EventArgs e)
        {
            if(dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
                dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
            {
                IntPtr hWnd = FindWindowForConfig(selectedConfig);
                if(hWnd != IntPtr.Zero)
                {
                    if(NativeMethods.GetWindowRect(hWnd, out RECT currentRect))
                    {
                        selectedConfig.TargetX = currentRect.Left;
                        selectedConfig.TargetY = currentRect.Top;
                        if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl)
                        {
                            bl.ResetItem(bl.IndexOf(selectedConfig));
                        }
                        else
                        {
                            dataGridViewWindowConfigs.Refresh();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not get current window position. The window might have closed or is inaccessible.", "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show($"Could not find a running window for Process: '{selectedConfig.ProcessName}'" +
                                    (!string.IsNullOrWhiteSpace(selectedConfig.WindowTitleHint) ? $" with Window Title: '{selectedConfig.WindowTitleHint}'" : "") +
                                    ".\nEnsure the application is running and the title hint (if any) is correct.",
                                    "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void buttonFetchSize_Click(object sender, EventArgs e)
        {
            if(dataGridViewWindowConfigs.SelectedRows.Count > 0 &&
                dataGridViewWindowConfigs.SelectedRows[0].DataBoundItem is WindowConfig selectedConfig)
            {
                IntPtr hWnd = FindWindowForConfig(selectedConfig);
                if(hWnd != IntPtr.Zero)
                {
                    if(NativeMethods.GetWindowRect(hWnd, out RECT currentRect))
                    {
                        selectedConfig.TargetWidth = currentRect.Width;
                        selectedConfig.TargetHeight = currentRect.Height;
                        if(dataGridViewWindowConfigs.DataSource is SortableBindingList<WindowConfig> bl)
                        {
                            bl.ResetItem(bl.IndexOf(selectedConfig));
                        }
                        else
                        {
                            dataGridViewWindowConfigs.Refresh();
                        }
                    }
                    else
                    {
                        MessageBox.Show("Could not get current window size. The window might have closed or is inaccessible.", "Fetch Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show($"Could not find a running window for Process: '{selectedConfig.ProcessName}'" +
                                    (!string.IsNullOrWhiteSpace(selectedConfig.WindowTitleHint) ? $" with Window Title: '{selectedConfig.WindowTitleHint}'" : "") +
                                    ".\nEnsure the application is running and the title hint (if any) is correct.",
                                    "Window Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void buttonSaveChanges_Click(object sender, EventArgs e)
        {
            SaveSettings();
            MessageBox.Show("Settings saved.", "Window Positioner", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void checkBoxStartWithWindows_CheckedChanged(object sender, EventArgs e)
        {
        }
    }
}