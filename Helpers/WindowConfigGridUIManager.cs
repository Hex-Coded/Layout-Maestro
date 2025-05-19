using WindowPlacementManager.Models;

namespace WindowPlacementManager.Helpers;

public static class WindowConfigGridUIManager
{
    public static void InitializeDataGridView(DataGridView dgv)
    {
        dgv.AutoGenerateColumns = false;
        dgv.Columns.Clear();
        dgv.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.IsEnabled), HeaderText = "Manage", Width = 60, Frozen = true, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.ProcessName), HeaderText = "Process Name", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 20, MinimumWidth = 100 });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.ExecutablePath), HeaderText = "Executable Path", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 30, MinimumWidth = 150, ToolTipText = "Full path to the executable (optional, helps with launching)" });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.WindowTitleHint), HeaderText = "Window Title", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 25, MinimumWidth = 120 });
        dgv.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.LaunchAsAdmin), HeaderText = "Run Adm?", ToolTipText = "Launch this application with Administrator privileges", Width = 65, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgv.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.ControlPosition), HeaderText = "Pos?", ToolTipText = "Control Position", Width = 40, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetX), HeaderText = "X", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetY), HeaderText = "Y", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgv.Columns.Add(new DataGridViewCheckBoxColumn { DataPropertyName = nameof(WindowConfig.ControlSize), HeaderText = "Size?", ToolTipText = "Control Size", Width = 45, AutoSizeMode = DataGridViewAutoSizeColumnMode.None });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetWidth), HeaderText = "W", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
        dgv.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = nameof(WindowConfig.TargetHeight), HeaderText = "H", Width = 50, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleRight } });
    }

    public static void LoadWindowConfigsForProfile(DataGridView dgv, GroupBox gb, Profile profile)
    {
        if(profile != null)
        {
            dgv.DataSource = new SortableBindingList<WindowConfig>(profile.WindowConfigs);
            gb.Text = $"Window Configurations for '{profile.Name}'";
        }
        else
        {
            dgv.DataSource = null;
            gb.Text = "Window Configurations (No Profile Selected)";
        }
    }

    public static void UpdateSelectionDependentButtons(DataGridView dgv, Button removeButton, Button activateLaunchButton, Button closeAppButton, Button fetchPosButton, Button fetchSizeButton)
    {
        bool rowSelected = dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].DataBoundItem is WindowConfig;
        removeButton.Enabled = rowSelected;
        activateLaunchButton.Enabled = rowSelected;
        closeAppButton.Enabled = rowSelected;
        fetchPosButton.Enabled = rowSelected;
        fetchSizeButton.Enabled = rowSelected;
    }

    public static WindowConfig GetSelectedWindowConfig(DataGridView dgv) => (dgv.SelectedRows.Count > 0 && dgv.SelectedRows[0].DataBoundItem is WindowConfig config) ? config : null;

    public static void AddAndSelectWindowConfig(DataGridView dgv, Profile profile, WindowConfig newConfig)
    {
        if(profile == null || newConfig == null) return;
        var bindingList = dgv.DataSource as SortableBindingList<WindowConfig> ?? new SortableBindingList<WindowConfig>(profile.WindowConfigs);
        if(dgv.DataSource == null) profile.WindowConfigs.Clear(); // If it was null, ensure our list is the one

        if(!bindingList.Contains(newConfig)) // ensure it's the correct list from the profile
        {
            if(!profile.WindowConfigs.Contains(newConfig)) profile.WindowConfigs.Add(newConfig);
            bindingList = new SortableBindingList<WindowConfig>(profile.WindowConfigs); // re-bind
            dgv.DataSource = bindingList;
        }
        bindingList.Add(newConfig); // This might cause duplicate if already in profile.WindowConfigs
                                    // Simpler: add to profile.WindowConfigs, then re-create binding list for dgv

        profile.WindowConfigs.Add(newConfig);
        dgv.DataSource = new SortableBindingList<WindowConfig>(profile.WindowConfigs); // Rebind

        if(dgv.Rows.Count > 0)
        {
            dgv.ClearSelection();
            DataGridViewRow newRow = dgv.Rows.Cast<DataGridViewRow>().FirstOrDefault(r => r.DataBoundItem == newConfig);
            if(newRow != null)
            {
                newRow.Selected = true;
                dgv.CurrentCell = newRow.Cells[0];
                if(newRow.Index >= 0 && newRow.Index < dgv.RowCount) dgv.FirstDisplayedScrollingRowIndex = newRow.Index;
            }
        }
    }
}