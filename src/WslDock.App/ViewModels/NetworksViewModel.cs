using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.App.ViewModels;
public partial class NetworksViewModel(DistributionContext context, IDockerNetworkClient client, IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerNetwork> Networks { get; } = [];
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RemoveCommand))] private DockerNetwork? selectedNetwork;
    [RelayCommand] private Task RefreshAsync() => RunAsync(async (d, ct) => Replace(Networks, await client.GetNetworksAsync(d, ct)));
    [RelayCommand(CanExecute = nameof(CanRemove))] private Task RemoveAsync() => confirmation.Confirm($"Remove network \"{SelectedNetwork!.Name}\"?") ? RunAsync(async (d, ct) => { await client.RemoveAsync(d, SelectedNetwork!.Id, ct); Replace(Networks, await client.GetNetworksAsync(d, ct)); }) : Task.CompletedTask;
    private bool CanRemove() => !IsBusy && SelectedNetwork is { IsStandard: false };
    [RelayCommand] private Task PruneAsync() => confirmation.Confirm("Remove all unused Docker networks?") ? RunAsync(async (d, ct) => { await client.PruneAsync(d, ct); Replace(Networks, await client.GetNetworksAsync(d, ct)); }) : Task.CompletedTask;
    public void Reset() { Networks.Clear(); SelectedNetwork = null; ErrorMessage = null; }
    protected override void BusyChanged() { RefreshCommand.NotifyCanExecuteChanged(); RemoveCommand.NotifyCanExecuteChanged(); PruneCommand.NotifyCanExecuteChanged(); }
}
