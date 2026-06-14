using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ZenZakuraUI.Models;
using ZenZakuraUI.Services;
using ZenZakuraUI.ViewModels;
using ZenZakuraUI.Windows;

namespace ZenZakuraUI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly MacroStorageService _storage;
    private readonly TrayService _trayService;

    public MainWindow()
    {
        InitializeComponent();

        _storage = new MacroStorageService();
        _trayService = new TrayService(this);

        _vm = new MainViewModel(_storage, _trayService, this);
        DataContext = _vm;

        _vm.SelectedMacroChanged += SyncPlayModeRadioButtons;
        _vm.EventAdded += () =>
        {
            if (_vm.SelectedMacro == null) return;
            var count = _vm.SelectedMacro.Events.Count;
            if (count > 0)
            {
                var scroll = FindScrollViewer(EventListBox);
                if (scroll == null || scroll.VerticalOffset >= scroll.ScrollableHeight - 10)
                    EventListBox?.ScrollIntoView(_vm.SelectedMacro.Events[count - 1]);
            }
        };

        Loaded += (_, _) =>
        {
            _vm.HookKeyEvents();
            _vm.LoadSavedMacros();
        };
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
        Hide();
        _trayService?.Show();
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        _vm.Cleanup();
        _trayService?.Dispose();
        Application.Current.Shutdown();
    }

    private void OnTitleBarMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var settingsVm = new SettingsViewModel();
        settingsVm.Load();

        var settingsWindow = new SettingsWindow(settingsVm);
        settingsWindow.Owner = this;
        settingsWindow.ShowDialog();
    }

    private void OnRemoveEventClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is KeyEvent evt && _vm.SelectedMacro != null)
        {
            _vm.SelectedMacro.Events.Remove(evt);
            _vm.ReapplyBindingIfActive();
        }
    }

    private void OnPlayModeChecked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton rb && rb.Tag is string mode && _vm.SelectedMacro != null)
        {
            _vm.SelectedMacro.PlayMode = mode switch
            {
                "Once" => PlaybackMode.Once,
                "RepeatWhileHeld" => PlaybackMode.RepeatWhileHeld,
                "ToggleRepeat" => PlaybackMode.ToggleRepeat,
                _ => PlaybackMode.Once
            };
            _vm.ReapplyBindingIfActive();
        }
    }

    private void SyncPlayModeRadioButtons()
    {
        if (_vm.SelectedMacro == null) return;
        var mode = _vm.SelectedMacro.PlayMode;
        foreach (var rb in new[] { PlayModeOnce, PlayModeRepeatHeld, PlayModeToggle })
        {
            if (rb == null) continue;
            var tag = rb.Tag as string;
            var shouldCheck = tag switch
            {
                "Once" => mode == PlaybackMode.Once,
                "RepeatWhileHeld" => mode == PlaybackMode.RepeatWhileHeld,
                "ToggleRepeat" => mode == PlaybackMode.ToggleRepeat,
                _ => false
            };
            if (rb.IsChecked != shouldCheck)
                rb.IsChecked = shouldCheck;
        }
    }

    private Point _dragStartPoint;

    private void OnEventListPreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(sender as IInputElement);
    }

    private void OnEventListMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (sender is not ListBox listBox) return;
        if (e.OriginalSource is TextBox or Button) return;

        var pos = e.GetPosition(listBox);
        if (Math.Abs(pos.X - _dragStartPoint.X) < 5 && Math.Abs(pos.Y - _dragStartPoint.Y) < 5)
            return;

        var item = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (item == null) return;

        var data = listBox.ItemContainerGenerator.ItemFromContainer(item);
        if (data == null) return;

        var dataObj = new DataObject(typeof(KeyEvent), data);
        DragDrop.DoDragDrop(item, dataObj, DragDropEffects.Move);
    }

    private void OnEventListDragOver(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(typeof(KeyEvent)))
        {
            e.Effects = DragDropEffects.Move;
            UpdateDragAdorner(sender, e);
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }
        e.Handled = true;
    }

    private void OnEventListDrop(object sender, DragEventArgs e)
    {
        ClearDragAdorner();

        if (!e.Data.GetDataPresent(typeof(KeyEvent))) return;
        var droppedData = e.Data.GetData(typeof(KeyEvent)) as KeyEvent;
        if (droppedData == null || _vm.SelectedMacro == null) return;

        var listBox = sender as ListBox;
        if (listBox == null) return;

        var targetItem = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (targetItem == null) return;
        var targetData = listBox.ItemContainerGenerator.ItemFromContainer(targetItem) as KeyEvent;
        if (targetData == null) return;

        var sourceIndex = _vm.SelectedMacro.Events.IndexOf(droppedData);
        var targetIndex = _vm.SelectedMacro.Events.IndexOf(targetData);
        if (sourceIndex < 0 || targetIndex < 0 || sourceIndex == targetIndex) return;

        _vm.SelectedMacro.Events.Move(sourceIndex, targetIndex);
        _vm.ReapplyBindingIfActive();

        Dispatcher.BeginInvoke(() =>
            EventListBox?.ScrollIntoView(droppedData));

        e.Handled = true;
    }

    private ListBoxItem? _dragAdornerItem;

    private void UpdateDragAdorner(object sender, DragEventArgs e)
    {
        ClearDragAdorner();
        if (sender is not ListBox listBox) return;
        var targetItem = FindVisualParent<ListBoxItem>(e.OriginalSource as DependencyObject);
        if (targetItem == null) return;

        if (targetItem.Template.FindName("ItemBorder", targetItem) is Border border)
        {
            _dragAdornerItem = targetItem;
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2B4BC"));
            border.BorderThickness = new Thickness(2);
        }
    }

    private void ClearDragAdorner()
    {
        if (_dragAdornerItem != null)
        {
            if (_dragAdornerItem.Template.FindName("ItemBorder", _dragAdornerItem) is Border border)
            {
                border.BorderBrush = Brushes.Transparent;
                border.BorderThickness = new Thickness(0);
            }
            _dragAdornerItem = null;
        }
    }

    private void OnHotkeyToggled(object sender, RoutedEventArgs e)
    {
        if (sender is CheckBox cb && cb.DataContext is Macro macro)
        {
            var enabled = cb.IsChecked == true;
            macro.IsHotkeyEnabled = enabled;
            _vm.ToggleMacroHotkey(macro, enabled);
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            _trayService?.Show();
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? root)
    {
        if (root == null) return null;
        var viewer = root as ScrollViewer;
        if (viewer != null) return viewer;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var result = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (result != null) return result;
        }
        return null;
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null && child is not T)
            child = VisualTreeHelper.GetParent(child);
        return child as T;
    }
}
