using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using ScreenInk.App.Services;
using ScreenInk.Core.Interaction;

namespace ScreenInk.App;

public partial class MainWindow : Window
{
    private readonly Func<ToggleHotkeyPreset, bool> _changeToggleHotkey;
    private readonly Func<bool, bool> _setStartWithWindows;
    private bool _allowClose;
    private bool _updatingSettings;
    private ToggleHotkeyPreset _selectedHotkey;

    internal MainWindow(
        AppPreferences preferences,
        Func<ToggleHotkeyPreset, bool> changeToggleHotkey,
        Func<bool, bool> setStartWithWindows)
    {
        _changeToggleHotkey = changeToggleHotkey ?? throw new ArgumentNullException(nameof(changeToggleHotkey));
        _setStartWithWindows = setStartWithWindows ?? throw new ArgumentNullException(nameof(setStartWithWindows));
        _selectedHotkey = preferences.ToggleHotkey;

        InitializeComponent();

        _updatingSettings = true;
        ToggleHotkeyComboBox.ItemsSource = Enum.GetValues<ToggleHotkeyPreset>()
            .Select(preset => new HotkeyChoice(preset, preset.GetDefinition().DisplayName))
            .ToArray();
        ToggleHotkeyComboBox.SelectedValuePath = nameof(HotkeyChoice.Preset);
        ToggleHotkeyComboBox.DisplayMemberPath = nameof(HotkeyChoice.Label);
        ToggleHotkeyComboBox.SelectedValue = preferences.ToggleHotkey;
        StartWithWindowsCheckBox.IsChecked = preferences.StartWithWindows;
        _updatingSettings = false;
    }

    public void AllowClose()
    {
        _allowClose = true;
    }

    public void SetStatus(InteractionMode mode, bool toggleHotkeyAvailable, string hotkeyLabel)
    {
        StatusTitle.Text = mode == InteractionMode.Draw ? "Draw mode" : "Pointer mode";
        StatusDescription.Text = mode == InteractionMode.Draw
            ? "Bấm lại nút công cụ để chọn Pen/Highlighter/Eraser. Ctrl+lăn chuột đổi cỡ; Ctrl+Z/Y hoàn tác/làm lại."
            : toggleHotkeyAvailable
                ? $"Nhấn {hotkeyLabel} hoặc chọn Start drawing trong tray để bắt đầu."
                : $"{hotkeyLabel} đang bị ứng dụng khác sử dụng. Hãy chọn shortcut khác bên dưới.";
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_allowClose)
        {
            e.Cancel = true;
            Hide();
        }

        base.OnClosing(e);
    }

    private void OnToggleHotkeySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingSettings || ToggleHotkeyComboBox.SelectedValue is not ToggleHotkeyPreset preset ||
            preset == _selectedHotkey)
        {
            return;
        }

        if (_changeToggleHotkey(preset))
        {
            _selectedHotkey = preset;
            HotkeyFeedbackText.Text = $"Shortcut changed to {preset.GetDefinition().DisplayName}.";
            return;
        }

        _updatingSettings = true;
        ToggleHotkeyComboBox.SelectedValue = _selectedHotkey;
        _updatingSettings = false;
        HotkeyFeedbackText.Text =
            $"{preset.GetDefinition().DisplayName} is already in use. The previous shortcut was restored.";
    }

    private void OnStartWithWindowsChanged(object sender, RoutedEventArgs e)
    {
        if (_updatingSettings)
        {
            return;
        }

        var enabled = StartWithWindowsCheckBox.IsChecked == true;
        if (_setStartWithWindows(enabled))
        {
            HotkeyFeedbackText.Text = enabled
                ? "Screen Ink will start when you sign in to Windows."
                : "Start with Windows is off.";
            return;
        }

        _updatingSettings = true;
        StartWithWindowsCheckBox.IsChecked = !enabled;
        _updatingSettings = false;
        HotkeyFeedbackText.Text = "Windows startup setting could not be changed. See the app log for details.";
    }

    private sealed record HotkeyChoice(ToggleHotkeyPreset Preset, string Label);
}
