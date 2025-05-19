namespace WindowPlacementManager.Helpers;

public static class GeneralUIManager
{
    public static void UpdateProgramActivityUI(CheckBox checkBoxDisable, GroupBox groupBoxConfigs, GroupBox groupBoxProfiles, bool isProgramDisabled)
    {
        groupBoxConfigs.Enabled = !isProgramDisabled;
        groupBoxProfiles.Enabled = !isProgramDisabled;
        groupBoxConfigs.BackColor = isProgramDisabled ? SystemColors.ControlDark : SystemColors.Control;
        checkBoxDisable.ForeColor = isProgramDisabled ? Color.Red : SystemColors.ControlText;
    }
}