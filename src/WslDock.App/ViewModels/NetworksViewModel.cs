using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class NetworksViewModel(
    DistributionContext context,
    IDockerNetworkClient client,
    IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerNetwork> Networks { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private DockerNetwork? selectedNetwork;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task RefreshAsync() => RunAsync(RefreshCoreAsync);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveAsync()
    {
        if (!CanRemove()) return Task.CompletedTask;
        var id = SelectedNetwork!.Id;
        var message = $"Remove network \"{SelectedNetwork!.Name}\"?";
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm(message)) return;
            token.ThrowIfCancellationRequested();
            await client.RemoveAsync(distribution, id, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    private bool CanRemove() => CanRun() && SelectedNetwork is { IsStandard: false };

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task PruneAsync() => RunAsync(async (distribution, token) =>
    {
        if (!confirmation.Confirm("Remove all unused Docker networks?")) return;
        token.ThrowIfCancellationRequested();
        await client.PruneAsync(distribution, token);
        await RefreshCoreAsync(distribution, token);
    });

    private async Task RefreshCoreAsync(string distribution, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Replace(Networks, await client.GetNetworksAsync(distribution, token), token);
        SelectedNetwork = null;
    }

    protected override void Reset()
    {
        Networks.Clear();
        SelectedNetwork = null;
    }

    protected override void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        PruneCommand.NotifyCanExecuteChanged();
    }
}
