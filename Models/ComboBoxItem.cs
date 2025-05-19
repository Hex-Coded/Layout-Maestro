namespace WindowPlacementManager.Models;

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