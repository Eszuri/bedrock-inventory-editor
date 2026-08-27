using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public partial class EnchantmentEntry : ObservableObject
{
    [ObservableProperty]
    private short _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private short _level = 1;

    public EnchantmentEntry() { }

    public EnchantmentEntry(short id, string name, short level)
    {
        Id = id;
        Name = name;
        Level = level;
    }
}
