using System.Windows;
using ZenZakuraUI.ViewModels;

namespace ZenZakuraUI.Windows;

public partial class SettingsWindow : Window
{
    public SettingsWindow(SettingsViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        vm.Applied += () => { DialogResult = true; Close(); };
    }

    private void OnCancel(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
