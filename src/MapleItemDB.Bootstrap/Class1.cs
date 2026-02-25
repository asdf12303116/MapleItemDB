using MapleItemDB.Application.UseCases;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Infrastructure.Cache;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Repositories;
using MapleItemDB.WzExtraction.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace MapleItemDB.Bootstrap;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMapleItemDb(
        this IServiceCollection services,
        string databasePath,
        Action<ILoggingBuilder>? configureLogging = null)
    {
        configureLogging ??= builder =>
        {
            builder.AddConsole();
            builder.AddDebug();
        };

        services.AddLogging(configureLogging);

        services.AddSingleton(SqliteConnectionFactory.FromFile(databasePath));
        services.AddSingleton<DatabaseBootstrapper>();

        services.AddSingleton<ItemRepository>();
        services.AddSingleton<IItemRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<IItemReadRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<ISetItemReadRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<ISkillReadRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<IItemWriteRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<ISetItemWriteRepository>(sp => sp.GetRequiredService<ItemRepository>());
        services.AddSingleton<ISkillWriteRepository>(sp => sp.GetRequiredService<ItemRepository>());

        services.AddSingleton<ISearchIndex, InMemorySearchIndex>();
        services.AddTransient<IWzExtractor, WzExtractionService>();

        services.AddTransient<IExtractAndImportUseCase, ExtractAndImportUseCase>();
        services.AddTransient<IInitializeCatalogUseCase, InitializeCatalogUseCase>();
        services.AddTransient<ISearchItemsUseCase, SearchItemsUseCase>();
        services.AddTransient<ISearchSkillsUseCase, SearchSkillsUseCase>();
        services.AddTransient<IGetItemByIdUseCase, GetItemByIdUseCase>();
        services.AddTransient<IGetStatsUseCase, GetStatsUseCase>();
        services.AddTransient<IGetItemDetailUseCase, GetItemDetailUseCase>();

        return services;
    }
}
