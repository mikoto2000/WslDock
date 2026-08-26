using System.Windows;
namespace WslDock.App.Services;
public sealed class MessageBoxConfirmationService : IConfirmationService
{
    public bool Confirm(string message) => MessageBox.Show(message, "WslDock confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
}
