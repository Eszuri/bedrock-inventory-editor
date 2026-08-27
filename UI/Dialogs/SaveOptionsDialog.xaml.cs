using System.Windows;

namespace BedrockInventoryEditor.UI.Dialogs;

public enum SaveOptionResult
{
    Cancel,
    SaveDirect,
    SaveWithBackup
}

public partial class SaveOptionsDialog : Window
{
    public SaveOptionResult Result { get; private set; } = SaveOptionResult.Cancel;

    public SaveOptionsDialog()
    {
        InitializeComponent();
    }

    private void OnSaveDirectClick(object sender, RoutedEventArgs e)
    {
        Result = SaveOptionResult.SaveDirect;
        DialogResult = true;
        Close();
    }

    private void OnSaveWithBackupClick(object sender, RoutedEventArgs e)
    {
        Result = SaveOptionResult.SaveWithBackup;
        DialogResult = true;
        Close();
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Result = SaveOptionResult.Cancel;
        DialogResult = false;
        Close();
    }
}
