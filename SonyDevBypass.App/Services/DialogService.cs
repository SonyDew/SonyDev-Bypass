namespace SonyDevBypass.App.Services;

public sealed class DialogService
{
    public bool Confirm(string title, string message)
    {
        return System.Windows.MessageBox.Show(
            System.Windows.Application.Current?.MainWindow,
            message,
            title,
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.Yes;
    }

    public void ShowInfo(string title, string message)
    {
        System.Windows.MessageBox.Show(
            System.Windows.Application.Current?.MainWindow,
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    public void ShowError(string title, string message)
    {
        System.Windows.MessageBox.Show(
            System.Windows.Application.Current?.MainWindow,
            message,
            title,
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Error);
    }
}
