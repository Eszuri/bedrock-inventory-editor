using System;
using System.Windows;
using System.Windows.Threading;

namespace BedrockInventoryEditor;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan:\n{ex.Message}\n\nDetail:\n{ex.StackTrace}", "Error Tak Terduga", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Terjadi kesalahan UI:\n{args.Exception.Message}\n\nDetail:\n{args.Exception.StackTrace}", "Error UI", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // Prevent app termination
        };
    }
}
