using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class VolumesViewModel(
    DistributionContext context,
    IDockerVolumeClient client,
    IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerVolume> Volumes { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private DockerVolume? selectedVolume;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task RefreshAsync() => RunAsync(RefreshCoreAsync);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveAsync()
    {
        if (!CanRemove()) return Task.CompletedTask;
        var id = SelectedVolume!.Name;
        var message = $"Remove volume \"{SelectedVolume!.Name}\"?";
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm(message)) return;
            token.ThrowIfCancellationRequested();
            await client.RemoveAsync(distribution, id, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    private bool CanRemove() => CanRun() && SelectedVolume is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task PruneAsync() => RunAsync(async (distribution, token) =>
    {
        if (!confirmation.Confirm("Remove all unused Docker volumes?")) return;
        token.ThrowIfCancellationRequested();
        await client.PruneAsync(distribution, token);
        await RefreshCoreAsync(distribution, token);
    });

    private async Task RefreshCoreAsync(string distribution, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Replace(Volumes, await client.GetVolumesAsync(distribution, token), token);
        SelectedVolume = null;
    }

    protected override void Reset()
    {
        Volumes.Clear();
        SelectedVolume = null;
    }

    protected override void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        PruneCommand.NotifyCanExecuteChanged();
    }
}
