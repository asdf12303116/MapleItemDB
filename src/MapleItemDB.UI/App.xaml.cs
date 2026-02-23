using System.Text;
using System.Windows;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try { Console.OutputEncoding = Encoding.UTF8; } catch { }

        var services = new ServiceCollection();
        ServiceRegistration.Configure(services);
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("DI 容器构建完成");

        var bootstrapper = _serviceProvider.GetRequiredService<DatabaseBootstrapper>();
        await bootstrapper.EnsureCreatedAsync();
        logger.LogInformation("数据库初始化完成");

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        logger.LogInformation("主窗口已显示");

        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        await viewModel.InitializeAsync();
        logger.LogInformation("搜索索引预热完成");
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
