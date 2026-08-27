using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public partial class EnchantmentEntry : ObservableObject
{
    [ObservableProperty]
    private short _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelDisplay))]
    [NotifyPropertyChangedFor(nameof(LevelRoman))]
    private short _level = 1;

    public string LevelDisplay => $"Lvl {Level}";

    public string LevelRoman => Level switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        _ => $"{Level}"
    };

    public EnchantmentEntry() { }

    public EnchantmentEntry(short id, string name, short level)
    {
        Id = id;
        Name = name;
        Level = level;
    }
}
