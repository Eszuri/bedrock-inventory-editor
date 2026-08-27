using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BedrockInventoryEditor.Core.Models;
using BedrockInventoryEditor.UI.Dialogs;

namespace BedrockInventoryEditor.UI.Controls;

public partial class InventorySlotControl : UserControl
{
    public static readonly RoutedEvent SlotClickedEvent =
        EventManager.RegisterRoutedEvent(nameof(SlotClicked), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(InventorySlotControl));

    public event RoutedEventHandler SlotClicked
    {
        add => AddHandler(SlotClickedEvent, value);
        remove => RemoveHandler(SlotClickedEvent, value);
    }

    public InventorySlotControl()
    {
        InitializeComponent();
    }

    private void OnSlotMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed && DataContext is ItemStack item)
        {
            try
            {
                var parentWindow = Window.GetWindow(this);
                var dialog = new ItemEditorDialog(item)
                {
                    Owner = parentWindow
                };

                if (dialog.ShowDialog() == true)
                {
                    RaiseEvent(new RoutedEventArgs(SlotClickedEvent, this));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat membuka editor item:\n{ex.Message}\n\nDetail:\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
