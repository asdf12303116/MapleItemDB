using System.IO;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Infrastructure.Cache;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories;
using MapleItemDB.UI.ViewModels;
using MapleItemDB.WzExtraction.Services;
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
        configureLogging ??= builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        };

        services.AddLogging(configureLogging);

        var dbPath = databasePath ?? Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "mapleitemdb.db");

        services.AddSingleton(SqliteConnectionFactory.FromFile(dbPath));
        services.AddSingleton<DatabaseBootstrapper>();
        services.AddSingleton<IItemRepository, ItemRepository>();
        services.AddSingleton<InMemorySearchIndex>();
        services.AddTransient<IWzExtractor, WzExtractionService>();
        services.AddSingleton<MainViewModel>();
        services.AddTransient<MainWindow>();
    }
}
