using MapleItemDB.Application.UseCases;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Infrastructure.Cache;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.Infrastructure.Database.Migrations;
using MapleItemDB.Infrastructure.Repositories.Read;
using MapleItemDB.Infrastructure.Repositories.Write;
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

        // 数据库迁移
        services.AddSingleton<IDbMigration, Migration001_AddSetItemId>();
        services.AddSingleton<IDbMigration, Migration002_AddBlobColumns>();
        services.AddSingleton<IDbMigration, Migration003_AddSkillColumns>();
        services.AddSingleton<IDbMigration, Migration004_AddSnColumn>();
        services.AddSingleton<IDbMigration, Migration005_AddTimeLimited>();
        services.AddSingleton<IDbMigration, Migration006_AddReqJob>();
        services.AddSingleton<IDbMigration, Migration007_SplitBlobAssetsWithHashDedup>();
        services.AddSingleton<IDbMigration, Migration008_AddItemSnMapTable>();
        services.AddSingleton<MigrationRunner>();
        services.AddSingleton<DatabaseBootstrapper>();

        // 读仓储
        services.AddSingleton<IItemReadRepository, ItemReadRepository>();
        services.AddSingleton<ISetItemReadRepository, SetItemReadRepository>();
        services.AddSingleton<ISkillReadRepository, SkillReadRepository>();

        // 写仓储
        services.AddSingleton<IItemWriteRepository, ItemWriteRepository>();
        services.AddSingleton<ISetItemWriteRepository, SetItemWriteRepository>();
        services.AddSingleton<ISkillWriteRepository, SkillWriteRepository>();
        services.AddSingleton<IItemSnRepository, ItemSnRepository>();

        services.AddSingleton<ISearchIndex, InMemorySearchIndex>();
        services.AddTransient<IWzExtractor, WzExtractionService>();

        services.AddTransient<IExtractAndImportUseCase, ExtractAndImportUseCase>();
        services.AddTransient<IUpdateSnDataUseCase, UpdateSnDataUseCase>();
        services.AddTransient<IInitializeCatalogUseCase, InitializeCatalogUseCase>();
        services.AddTransient<ISearchItemsUseCase, SearchItemsUseCase>();
        services.AddTransient<ISearchSkillsUseCase, SearchSkillsUseCase>();
        services.AddTransient<IGetItemByIdUseCase, GetItemByIdUseCase>();
        services.AddTransient<IGetStatsUseCase, GetStatsUseCase>();
        services.AddTransient<IGetItemDetailUseCase, GetItemDetailUseCase>();

        return services;
    }
}
