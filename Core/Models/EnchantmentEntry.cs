using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.Core.Models;

public enum EnchantmentDiffStatus
{
    Unchanged,
    New,
    Modified
}

public partial class EnchantmentEntry : ObservableObject
{
    [ObservableProperty]
    private short _id;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelDisplay))]
    [NotifyPropertyChangedFor(nameof(LevelRoman))]
    [NotifyPropertyChangedFor(nameof(LevelChangeText))]
    [NotifyPropertyChangedFor(nameof(ChangeTag))]
    [NotifyPropertyChangedFor(nameof(DiffStatus))]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(HasLevelChange))]
    [NotifyPropertyChangedFor(nameof(IsUnchanged))]
    private short _level = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LevelChangeText))]
    [NotifyPropertyChangedFor(nameof(ChangeTag))]
    [NotifyPropertyChangedFor(nameof(DiffStatus))]
    [NotifyPropertyChangedFor(nameof(IsNew))]
    [NotifyPropertyChangedFor(nameof(HasLevelChange))]
    [NotifyPropertyChangedFor(nameof(IsUnchanged))]
    private short? _originalLevel;

    public EnchantmentDiffStatus DiffStatus
    {
        get
        {
            if (!OriginalLevel.HasValue) return EnchantmentDiffStatus.New;
            if (OriginalLevel.Value != Level) return EnchantmentDiffStatus.Modified;
            return EnchantmentDiffStatus.Unchanged;
        }
    }

    public bool IsNew => DiffStatus == EnchantmentDiffStatus.New;
    public bool HasLevelChange => DiffStatus == EnchantmentDiffStatus.Modified;
    public bool IsUnchanged => DiffStatus == EnchantmentDiffStatus.Unchanged;

    public string LevelDisplay => $"Lvl {Level}";

    public string LevelChangeText => HasLevelChange ? $"Lvl {OriginalLevel} ➔ Lvl {Level}" : LevelDisplay;

    public string ChangeTag => DiffStatus switch
    {
        EnchantmentDiffStatus.New => "+ BARU",
        EnchantmentDiffStatus.Modified => $"Sebelumnya: Lvl {OriginalLevel}",
        _ => ""
    };

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

    public EnchantmentEntry(short id, string name, short level, short? originalLevel = null)
    {
        Id = id;
        Name = name;
        Level = level;
        OriginalLevel = originalLevel;
    }
}
