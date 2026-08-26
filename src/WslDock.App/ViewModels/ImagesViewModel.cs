using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;
namespace WslDock.App.ViewModels;
public partial class ImagesViewModel(DistributionContext context, IDockerImageClient client, IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerImage> Images { get; } = [];
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(RemoveCommand))] private DockerImage? selectedImage;
    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(PullCommand))] private string imageReference = "";
    [RelayCommand] private Task RefreshAsync() => RunAsync(async (d, ct) => Replace(Images, await client.GetImagesAsync(d, ct)));
    [RelayCommand(CanExecute = nameof(CanPull))] private Task PullAsync() => RunAsync(async (d, ct) => { await client.PullAsync(d, ImageReference.Trim(), ct); Replace(Images, await client.GetImagesAsync(d, ct)); });
    private bool CanPull() => !IsBusy && !string.IsNullOrWhiteSpace(ImageReference);
    [RelayCommand(CanExecute = nameof(CanRemove))] private Task RemoveAsync() => confirmation.Confirm($"Remove image \"{SelectedImage!.Repository}:{SelectedImage.Tag}\"?") ? RunAsync(async (d, ct) => { await client.RemoveAsync(d, SelectedImage!.Id, ct); Replace(Images, await client.GetImagesAsync(d, ct)); }) : Task.CompletedTask;
    private bool CanRemove() => !IsBusy && SelectedImage is not null;
    [RelayCommand] private Task PruneAsync() => confirmation.Confirm("Remove all unused Docker images?") ? RunAsync(async (d, ct) => { await client.PruneAsync(d, ct); Replace(Images, await client.GetImagesAsync(d, ct)); }) : Task.CompletedTask;
}
