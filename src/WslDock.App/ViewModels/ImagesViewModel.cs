using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WslDock.App.Services;
using WslDock.Application;
using WslDock.Domain;

namespace WslDock.App.ViewModels;

public partial class ImagesViewModel(
    DistributionContext context,
    IDockerImageClient client,
    IConfirmationService confirmation) : ResourceViewModel(context)
{
    public ObservableCollection<DockerImage> Images { get; } = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveCommand))]
    private DockerImage? selectedImage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(PullCommand))]
    private string imageReference = "";

    [RelayCommand(CanExecute = nameof(CanPull))]
    private Task PullAsync()
    {
        if (!CanPull()) return Task.CompletedTask;
        var reference = ImageReference.Trim();
        return RunAsync(async (distribution, token) =>
        {
            await client.PullAsync(distribution, reference, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    private bool CanPull() => CanRun() && !string.IsNullOrWhiteSpace(ImageReference);

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task RefreshAsync() => RunAsync(RefreshCoreAsync);

    [RelayCommand(CanExecute = nameof(CanRemove))]
    private Task RemoveAsync()
    {
        if (!CanRemove()) return Task.CompletedTask;
        var id = SelectedImage!.Id;
        var message = $"Remove image \"{SelectedImage!.Repository}:{SelectedImage.Tag}\"?";
        return RunAsync(async (distribution, token) =>
        {
            if (!confirmation.Confirm(message)) return;
            token.ThrowIfCancellationRequested();
            await client.RemoveAsync(distribution, id, token);
            await RefreshCoreAsync(distribution, token);
        });
    }

    private bool CanRemove() => CanRun() && SelectedImage is not null;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private Task PruneAsync() => RunAsync(async (distribution, token) =>
    {
        if (!confirmation.Confirm("Remove all unused Docker images?")) return;
        token.ThrowIfCancellationRequested();
        await client.PruneAsync(distribution, token);
        await RefreshCoreAsync(distribution, token);
    });

    private async Task RefreshCoreAsync(string distribution, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        Replace(Images, await client.GetImagesAsync(distribution, token), token);
        SelectedImage = null;
    }

    protected override void Reset()
    {
        Images.Clear();
        SelectedImage = null;
    }

    protected override void NotifyCommands()
    {
        RefreshCommand.NotifyCanExecuteChanged();
        PullCommand.NotifyCanExecuteChanged();
        RemoveCommand.NotifyCanExecuteChanged();
        PruneCommand.NotifyCanExecuteChanged();
    }
}
