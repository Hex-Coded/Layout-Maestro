using Microsoft.VisualBasic;
using WindowPlacementManager.Models;

namespace WindowPlacementManager.Helpers;

public static class ProfileUIManager
{
    public static void PopulateActiveProfileComboBox(ComboBox comboBox, IEnumerable<Profile> profiles, string currentActiveProfileName)
    {
        string previouslySelectedName = (comboBox.SelectedItem as Profile)?.Name;
        comboBox.BeginUpdate();
        comboBox.Items.Clear();
        comboBox.DisplayMember = nameof(Profile.Name);
        foreach(var profile in profiles) comboBox.Items.Add(profile);
        comboBox.EndUpdate();

        Profile profileToSelect = profiles.FirstOrDefault(p => p.Name == previouslySelectedName) ??
                                  profiles.FirstOrDefault(p => p.Name == currentActiveProfileName) ??
                                  profiles.FirstOrDefault();
        if(profileToSelect != null) comboBox.SelectedItem = profileToSelect;
    }

    public static void UpdateProfileManagementButtons(Button removeButton, Button renameButton, Button cloneButton, Button addConfigButton, bool profileSelected, int totalProfiles)
    {
        removeButton.Enabled = profileSelected && totalProfiles > 1;
        renameButton.Enabled = profileSelected;
        cloneButton.Enabled = profileSelected;
        addConfigButton.Enabled = profileSelected;
    }

    public static void UpdateProfileSpecificActionButtons(Button launchAllButton, Button focusAllButton, Button closeAllButton, Button testButton, Profile selectedProfile)
    {
        bool profileSelected = selectedProfile != null;
        bool hasEnabledConfigs = profileSelected && selectedProfile.WindowConfigs.Any(wc => wc.IsEnabled);
        launchAllButton.Enabled = hasEnabledConfigs;
        focusAllButton.Enabled = hasEnabledConfigs;
        closeAllButton.Enabled = hasEnabledConfigs;
        testButton.Enabled = hasEnabledConfigs;
    }

    public static Profile HandleAddProfile(List<Profile> profiles)
    {
        string newProfileName = Interaction.InputBox("Enter new profile name:", "Add Profile", "New Profile " + (profiles.Count + 1));
        if(string.IsNullOrWhiteSpace(newProfileName)) return null;
        if(profiles.Any(p => p.Name.Equals(newProfileName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
        var newProfile = new Profile(newProfileName);
        profiles.Add(newProfile);
        return newProfile;
    }

    public static bool HandleRemoveProfile(List<Profile> profiles, Profile profileToRemove, ref string activeProfileName)
    {
        if(profileToRemove == null) { MessageBox.Show("Please select a profile to remove.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return false; }
        if(profiles.Count <= 1) { MessageBox.Show("Cannot remove the last profile.", "Action Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning); return false; }
        if(MessageBox.Show($"Are you sure you want to delete profile '{profileToRemove.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return false;

        string removedProfileName = profileToRemove.Name;
        profiles.Remove(profileToRemove);
        if(activeProfileName == removedProfileName) activeProfileName = profiles.FirstOrDefault()?.Name ?? string.Empty;
        return true;
    }

    public static bool HandleRenameProfile(List<Profile> profiles, Profile profileToRename, ref string activeProfileName)
    {
        if(profileToRename == null) return false;
        string newName = Interaction.InputBox("Enter new name for profile:", "Rename Profile", profileToRename.Name);
        if(string.IsNullOrWhiteSpace(newName) || newName == profileToRename.Name) return false;
        if(profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != profileToRename))
        {
            MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }
        bool isActive = activeProfileName == profileToRename.Name;
        profileToRename.Name = newName;
        if(isActive) activeProfileName = newName;
        return true;
    }

    public static Profile HandleCloneProfile(List<Profile> profiles, Profile profileToClone)
    {
        if(profileToClone == null) { MessageBox.Show("Please select a profile to clone.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return null; }

        string originalName = profileToClone.Name;
        string newNameSuggestion = originalName + " (Copy)";
        int copyCount = 1;
        while(profiles.Any(p => p.Name.Equals(newNameSuggestion, StringComparison.OrdinalIgnoreCase)))
            newNameSuggestion = $"{originalName} (Copy {++copyCount})";

        string newProfileName = Interaction.InputBox("Enter name for the cloned profile:", "Clone Profile", newNameSuggestion);
        if(string.IsNullOrWhiteSpace(newProfileName)) return null;
        if(profiles.Any(p => p.Name.Equals(newProfileName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
        var clonedProfile = new Profile(newProfileName);
        foreach(var wc in profileToClone.WindowConfigs) clonedProfile.WindowConfigs.Add(wc.Clone());
        profiles.Add(clonedProfile);
        return clonedProfile;
    }

    public static string HandleRemoveProfile(List<Profile> profiles, Profile profileToRemove, string currentActiveProfileName)
    {
        if(profileToRemove == null) { MessageBox.Show("Please select a profile to remove.", "No Profile Selected", MessageBoxButtons.OK, MessageBoxIcon.Information); return null; }
        if(profiles.Count <= 1) { MessageBox.Show("Cannot remove the last profile.", "Action Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning); return null; }
        if(MessageBox.Show($"Are you sure you want to delete profile '{profileToRemove.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return null;

        string removedProfileName = profileToRemove.Name;
        profiles.Remove(profileToRemove);
        if(currentActiveProfileName == removedProfileName)
            return profiles.FirstOrDefault()?.Name ?? string.Empty; // Return new active name
        return null; // Active name did not change
    }

    // Returns the new profile name if it was changed and was the active one, otherwise null
    public static string HandleRenameProfile(List<Profile> profiles, Profile profileToRename, string currentActiveProfileName)
    {
        if(profileToRename == null) return null;
        string newName = Interaction.InputBox("Enter new name for profile:", "Rename Profile", profileToRename.Name);
        if(string.IsNullOrWhiteSpace(newName) || newName == profileToRename.Name) return null;
        if(profiles.Any(p => p.Name.Equals(newName, StringComparison.OrdinalIgnoreCase) && p != profileToRename))
        {
            MessageBox.Show("A profile with this name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return null;
        }
        bool isActive = currentActiveProfileName == profileToRename.Name;
        profileToRename.Name = newName;
        if(isActive)
            return newName; // Return new active name
        return null; // Active name did not change, or this wasn't the active profile
    }
}