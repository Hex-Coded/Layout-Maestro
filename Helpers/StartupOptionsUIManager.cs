using WindowPlacementManager.Models;
using WindowPlacementManager.Services;

namespace WindowPlacementManager.Helpers;

public static class StartupOptionsUIManager
{
    public static void InitializeComboBox(ComboBox comboBox)
    {
        comboBox.Items.Clear();
        comboBox.Items.Add(new ComboBoxItem("Don't Boot with Windows", StartupType.None));
        comboBox.Items.Add(new ComboBoxItem("Boot with Windows (User)", StartupType.Normal));
        comboBox.Items.Add(new ComboBoxItem("Boot with Windows (Administrator)", StartupType.Admin));
        comboBox.DisplayMember = nameof(ComboBoxItem.DisplayName);
        comboBox.ValueMember = nameof(ComboBoxItem.Value);
    }

    public static void SelectCurrentOption(ComboBox comboBox, StartupType currentStartupType)
    {
        foreach(ComboBoxItem item in comboBox.Items)
            if((StartupType)item.Value == currentStartupType) { comboBox.SelectedItem = item; return; }
        if(comboBox.Items.Count > 0) comboBox.SelectedIndex = 0;
    }

    public static StartupType GetSelectedStartupType(ComboBox comboBox) =>
        (comboBox.SelectedItem is ComboBoxItem selectedItem) ? (StartupType)selectedItem.Value : StartupType.None;
}