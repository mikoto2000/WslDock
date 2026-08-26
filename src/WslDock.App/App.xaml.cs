using System.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WslDock.App.ViewModels;
using WslDock.Application;
using WslDock.Application.Processes;
using WslDock.Infrastructure.Docker;
using WslDock.Infrastructure.Process;
using WslDock.Infrastructure.Wsl;

namespace WslDock.App;

public partial class App : System.Windows.Application
{
    private ServiceProvider? services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", true).Build();
        var options = new WslDockOptions { DefaultDistribution = configuration["WslDock:DefaultDistribution"] };
        if (int.TryParse(configuration["WslDock:LogTailLines"], out var tailLines)) options.LogTailLines = tailLines;
        var collection = new ServiceCollection();
        collection.AddLogging(builder => builder.AddDebug());
        collection.AddSingleton(options);
        collection.AddSingleton<IProcessRunner, ProcessRunner>();
        collection.AddSingleton<IWslService, WslService>();
        collection.AddSingleton<IDockerClient, WslDockerCliClient>();
        collection.AddSingleton<MainViewModel>();
        collection.AddSingleton<MainWindow>();
        services = collection.BuildServiceProvider();
        MainWindow = services.GetRequiredService<MainWindow>();
        MainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        services?.Dispose();
        base.OnExit(e);
    }
}
