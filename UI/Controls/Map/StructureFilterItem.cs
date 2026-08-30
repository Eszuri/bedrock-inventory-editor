using System;
using System.Windows.Media.Imaging;
using BedrockInventoryEditor.Core.Map.Structure;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BedrockInventoryEditor.UI.Controls.Map;

public partial class StructureFilterItem : ObservableObject
{
    public StructureType Type { get; }
    public string Name { get; }
    public string IconAsset { get; }
    public int DimensionId { get; } // -1 = All, 0 = Overworld, 1 = Nether, 2 = End

    [ObservableProperty]
    private bool _isChecked = true;

    public BitmapImage? IconImage { get; private set; }

    public StructureFilterItem(StructureType type, string name, string iconAsset, int dimensionId = -1, bool isChecked = true)
    {
        Type = type;
        Name = name;
        IconAsset = iconAsset;
        DimensionId = dimensionId;
        _isChecked = isChecked;

        try
        {
            var uri = new Uri($"pack://application:,,,/BedrockInventoryEditor;component/Assets/Structures/{iconAsset}", UriKind.Absolute);
            IconImage = new BitmapImage(uri);
            IconImage.Freeze();
        }
        catch { }
    }
}
