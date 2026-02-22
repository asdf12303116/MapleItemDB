using System.Text;
using System.Windows;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Infrastructure.Cache;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories;
using MapleItemDB.UI.ViewModels;
using MapleItemDB.WzExtraction.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.UI;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* WPF 无控制台窗口时忽略 */ }

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var logger = _serviceProvider.GetRequiredService<ILogger<App>>();
        logger.LogInformation("DI 容器构建完成");

        // 初始化数据库
        var bootstrapper = _serviceProvider.GetRequiredService<DatabaseBootstrapper>();
        await bootstrapper.EnsureCreatedAsync();
        logger.LogInformation("数据库初始化完成");

        // 创建并显示主窗口
        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
        logger.LogInformation("主窗口已显示");

        // 预热搜索索引
        var viewModel = _serviceProvider.GetRequiredService<MainViewModel>();
        await viewModel.InitializeAsync();
        logger.LogInformation("搜索索引预热完成");
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(b =>
        {
            b.AddConsole();
            b.AddDebug();
        });

        var dbPath = System.IO.Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "mapleitemdb.db");

        // Database
        services.AddSingleton(SqliteConnectionFactory.FromFile(dbPath));
        services.AddSingleton<DatabaseBootstrapper>();

        // Repositories
        services.AddSingleton<IItemRepository, ItemRepository>();

        // Cache
        services.AddSingleton<InMemorySearchIndex>();

        // Extraction
        services.AddTransient<IWzExtractor, WzExtractionService>();

        // ViewModels
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddTransient<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
