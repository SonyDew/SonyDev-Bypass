using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Media;
using SonyDevBypass.App.Services;
using SonyDevBypass.App.ViewModels;
using Forms = System.Windows.Forms;

namespace SonyDevBypass.App;

public partial class MainWindow : Window
{
    private const double DesignWidth = 1320;
    private const double DesignHeight = 1000;
    private const double MaxWorkAreaUsage = 0.92;
    private readonly MainViewModel _viewModel;

    public MainWindow()
    {
        InitializeComponent();

        var configuration = SonyDevApiConfiguration.Load();

        _viewModel = new MainViewModel(
            new SonyDevApiClient(configuration),
            new AppUpdateService(configuration),
            new AppSettingsService(),
            new LocalizationService(),
            new DialogService());

        DataContext = _viewModel;
    }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        ApplyAdaptiveBounds();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        ApplyAdaptiveBounds();
        await _viewModel.InitializeAsync();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (!_viewModel.ConfirmClose())
        {
            e.Cancel = true;
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        _viewModel.Dispose();
        base.OnClosed(e);
    }

    private void ApplyAdaptiveBounds()
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        var screen = Forms.Screen.FromHandle(handle);
        var dpi = VisualTreeHelper.GetDpi(this);
        var workAreaWidth = screen.WorkingArea.Width / dpi.DpiScaleX;
        var workAreaHeight = screen.WorkingArea.Height / dpi.DpiScaleY;
        var targetAreaWidth = workAreaWidth * MaxWorkAreaUsage;
        var targetAreaHeight = workAreaHeight * MaxWorkAreaUsage;

        var scale = Math.Min(1.0, Math.Min(targetAreaWidth / DesignWidth, targetAreaHeight / DesignHeight));
        var targetWidth = Math.Floor(DesignWidth * scale);
        var targetHeight = Math.Floor(DesignHeight * scale);

        Width = targetWidth;
        Height = targetHeight;
        MinWidth = targetWidth;
        MaxWidth = targetWidth;
        MinHeight = targetHeight;
        MaxHeight = targetHeight;

        var screenLeft = screen.WorkingArea.Left / dpi.DpiScaleX;
        var screenTop = screen.WorkingArea.Top / dpi.DpiScaleY;

        Left = screenLeft + Math.Max(0, (workAreaWidth - targetWidth) / 2);
        Top = screenTop + Math.Max(0, (workAreaHeight - targetHeight) / 2);
    }
}
