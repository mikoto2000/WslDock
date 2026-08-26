using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.App.ViewModels;
public partial class VolumesViewModel(DistributionContext context, IDockerVolumeClient client, IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerVolume> Volumes { get; } = [];
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RemoveCommand))] private DockerVolume? selectedVolume;
    [RelayCommand] private Task RefreshAsync() => RunAsync(async (d, ct) => Replace(Volumes, await client.GetVolumesAsync(d, ct)));
    [RelayCommand(CanExecute = nameof(CanRemove))] private Task RemoveAsync() => confirmation.Confirm($"Remove volume \"{SelectedVolume!.Name}\"?") ? RunAsync(async (d, ct) => { await client.RemoveAsync(d, SelectedVolume!.Name, ct); Replace(Volumes, await client.GetVolumesAsync(d, ct)); }) : Task.CompletedTask;
    private bool CanRemove() => !IsBusy && SelectedVolume is not null;
    [RelayCommand] private Task PruneAsync() => confirmation.Confirm("Remove all unused Docker volumes?") ? RunAsync(async (d, ct) => { await client.PruneAsync(d, ct); Replace(Volumes, await client.GetVolumesAsync(d, ct)); }) : Task.CompletedTask;
}
