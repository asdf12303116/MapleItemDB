using System.IO;
using MapleItemDB.Bootstrap;
using MapleItemDB.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.UI;

public static class ServiceRegistration
{
    public static void Configure(
        IServiceCollection services,
        string? databasePath = null,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        var dbPath = databasePath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "mapleitemdb.db");

        services.AddMapleItemDb(dbPath, configureLogging);
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
