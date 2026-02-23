using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MapleItemDB.Core.Interfaces;
using MapleItemDB.Core.Models;
using MapleItemDB.Infrastructure.Database;
using MapleItemDB.UI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

Console.OutputEncoding = Encoding.UTF8;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    PrintGlobalHelp();
    return 0;
}

var command = args[0].ToLowerInvariant();
var commandArgs = args.Skip(1).ToArray();

try
{
    return command switch
    {
        "extract" => await RunExtractAsync(commandArgs),
        "search" => await RunSearchAsync(commandArgs),
        "skill" => await RunSkillAsync(commandArgs),
        "get" => await RunGetAsync(commandArgs),
        "stats" => await RunStatsAsync(commandArgs),
        _ => PrintUnknownCommand(command),
    };
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine($"参数错误: {ex.Message}");
    return 1;
}

// ========== 子命令实现 ==========

static async Task<int> RunExtractAsync(string[] args)
{
    var wzDir = CliConfig.DefaultWzDir;
    string? dbPath = null;
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help": showHelp = true; break;
            case "--wz": wzDir = NextArg(args, ref i, "--wz"); break;
            case "--db": dbPath = NextArg(args, ref i, "--db"); break;
            default: throw new ArgumentException($"未知参数: {args[i]}");
        }
    }

    if (showHelp)
    {
        Console.WriteLine("用法: mapleidb extract [--wz <dir>] [--db <path>]");
        Console.WriteLine();
        Console.WriteLine("从 WZ 数据目录提取道具/技能/套装数据并写入数据库。");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine($"  --wz <dir>   WZ 数据目录 (默认 {CliConfig.DefaultWzDir})");
        Console.WriteLine($"  --db <path>  数据库路径 (默认 {CliConfig.DefaultDbPath})");
        return 0;
    }

    if (!Directory.Exists(wzDir))
    {
        Console.Error.WriteLine($"WZ 目录不存在: {wzDir}");
        return 1;
    }

    Console.Error.WriteLine($"WZ 目录: {wzDir}");
    Console.Error.WriteLine($"数据库:  {Path.GetFullPath(dbPath ?? CliConfig.DefaultDbPath)}");

    using var provider = BuildServiceProvider(dbPath);
    await EnsureDatabaseAsync(provider);

    var extractor = provider.GetRequiredService<IWzExtractor>();
    var repository = provider.GetRequiredService<IItemRepository>();

    var progress = new Progress<ExtractionProgress>(p =>
        Console.Error.WriteLine($"[{p.Current * 100.0 / Math.Max(p.Total, 1),6:0.00}%] {p.Phase} {p.Message}"));

    var result = await extractor.ExtractAllAsync(wzDir, progress);

    Console.Error.WriteLine("写入数据库...");
    await repository.BulkUpsertAsync(result.Items);
    await repository.BulkUpsertSetItemsAsync(result.SetItems.Values);
    await repository.BulkUpsertSkillsAsync(result.Skills);

    Console.Error.WriteLine("完成。");
    PrintJson(new { items = result.Items.Count, setItems = result.SetItems.Count, skills = result.Skills.Count });
    return 0;
}

static async Task<int> RunSearchAsync(string[] args)
{
    var filter = new ItemQueryFilter { Limit = 50 };
    string? dbPath = null;
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help": showHelp = true; break;
            case "--db": dbPath = NextArg(args, ref i, "--db"); break;
            case "--category": filter.Category = Enum.Parse<ItemCategory>(NextArg(args, ref i, "--category"), ignoreCase: true); break;
            case "--sub": filter.SubCategory = NextArg(args, ref i, "--sub"); break;
            case "--min-level": filter.MinLevel = int.Parse(NextArg(args, ref i, "--min-level")); break;
            case "--max-level": filter.MaxLevel = int.Parse(NextArg(args, ref i, "--max-level")); break;
            case "--cash": filter.IsCash = true; break;
            case "--has-sn": filter.HasSn = true; break;
            case "--min-boss": filter.MinBossDmg = int.Parse(NextArg(args, ref i, "--min-boss")); break;
            case "--min-ied": filter.MinIed = int.Parse(NextArg(args, ref i, "--min-ied")); break;
            case "--limit": filter.Limit = int.Parse(NextArg(args, ref i, "--limit")); break;
            default:
                if (args[i].StartsWith('-'))
                    throw new ArgumentException($"未知参数: {args[i]}");
                filter.Keyword = args[i];
                break;
        }
    }

    if (showHelp)
    {
        Console.WriteLine("用法: mapleidb search [keyword] [options] [--db <path>]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  [keyword]         模糊搜索名称/描述/ID");
        Console.WriteLine("  --category <cat>  分类: Equip/Consume/Etc/Setup/Cash/Pet");
        Console.WriteLine("  --sub <sub>       子分类: Weapon/Cap/Coat 等");
        Console.WriteLine("  --min-level <n>   最小等级");
        Console.WriteLine("  --max-level <n>   最大等级");
        Console.WriteLine("  --cash            仅商城道具");
        Console.WriteLine("  --has-sn          仅含 SN");
        Console.WriteLine("  --min-boss <n>    最小 Boss 伤害");
        Console.WriteLine("  --min-ied <n>     最小无视防御");
        Console.WriteLine("  --limit <n>       结果数限制 (默认 50)");
        Console.WriteLine($"  --db <path>       数据库路径 (默认 {CliConfig.DefaultDbPath})");
        return 0;
    }

    using var provider = BuildServiceProvider(dbPath);
    await EnsureDatabaseAsync(provider);

    var repository = provider.GetRequiredService<IItemRepository>();
    var items = await repository.QueryAsync(filter);

    PrintJson(items);
    return 0;
}

static async Task<int> RunSkillAsync(string[] args)
{
    string? keyword = null;
    var limit = 50;
    string? dbPath = null;
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help": showHelp = true; break;
            case "--db": dbPath = NextArg(args, ref i, "--db"); break;
            case "--limit": limit = int.Parse(NextArg(args, ref i, "--limit")); break;
            default:
                if (args[i].StartsWith('-'))
                    throw new ArgumentException($"未知参数: {args[i]}");
                keyword = args[i];
                break;
        }
    }

    if (showHelp)
    {
        Console.WriteLine("用法: mapleidb skill [keyword] [--limit <n>] [--db <path>]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine("  [keyword]    技能名称模糊搜索");
        Console.WriteLine("  --limit <n>  结果数限制 (默认 50)");
        Console.WriteLine($"  --db <path>  数据库路径 (默认 {CliConfig.DefaultDbPath})");
        return 0;
    }

    using var provider = BuildServiceProvider(dbPath);
    await EnsureDatabaseAsync(provider);

    var repository = provider.GetRequiredService<IItemRepository>();
    var skills = await repository.SearchSkillsByNameAsync(keyword ?? "", limit);

    PrintJson(skills);
    return 0;
}

static async Task<int> RunGetAsync(string[] args)
{
    int? itemId = null;
    string? dbPath = null;
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help": showHelp = true; break;
            case "--db": dbPath = NextArg(args, ref i, "--db"); break;
            default:
                if (args[i].StartsWith('-'))
                    throw new ArgumentException($"未知参数: {args[i]}");
                itemId = int.Parse(args[i]);
                break;
        }
    }

    if (showHelp)
    {
        Console.WriteLine("用法: mapleidb get <id> [--db <path>]");
        Console.WriteLine();
        Console.WriteLine("按 ID 查询道具详情，输出全部属性、动态属性及套装信息。");
        Console.WriteLine();
        Console.WriteLine("参数:");
        Console.WriteLine("  <id>         道具 ID (必填)");
        Console.WriteLine($"  --db <path>  数据库路径 (默认 {CliConfig.DefaultDbPath})");
        return 0;
    }

    if (itemId is null)
    {
        Console.Error.WriteLine("错误: 缺少道具 ID。用法: mapleidb get <id>");
        return 1;
    }

    using var provider = BuildServiceProvider(dbPath);
    await EnsureDatabaseAsync(provider);

    var repository = provider.GetRequiredService<IItemRepository>();
    var item = await repository.GetByIdAsync(itemId.Value);

    if (item is null)
    {
        Console.Error.WriteLine($"未找到道具 ID: {itemId}");
        return 1;
    }

    SetItemInfo? setItem = null;
    if (item.SetItemId is not null)
    {
        var setItems = await repository.GetAllSetItemsAsync();
        setItems.TryGetValue(item.SetItemId.Value, out setItem);
    }

    PrintJson(new { item, setItem });
    return 0;
}

static async Task<int> RunStatsAsync(string[] args)
{
    string? dbPath = null;
    var showHelp = false;

    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "-h" or "--help": showHelp = true; break;
            case "--db": dbPath = NextArg(args, ref i, "--db"); break;
            default: throw new ArgumentException($"未知参数: {args[i]}");
        }
    }

    if (showHelp)
    {
        Console.WriteLine("用法: mapleidb stats [--db <path>]");
        Console.WriteLine();
        Console.WriteLine("选项:");
        Console.WriteLine($"  --db <path>  数据库路径 (默认 {CliConfig.DefaultDbPath})");
        return 0;
    }

    using var provider = BuildServiceProvider(dbPath);
    await EnsureDatabaseAsync(provider);

    var repository = provider.GetRequiredService<IItemRepository>();

    var allItems = await repository.QueryAsync(new ItemQueryFilter { Limit = 0 });
    var setItems = await repository.GetAllSetItemsAsync();
    var skills = await repository.SearchSkillsByNameAsync("", 0);

    var categories = allItems
        .GroupBy(i => i.Category)
        .OrderBy(g => g.Key)
        .ToDictionary(g => g.Key.ToString(), g => g.Count());

    PrintJson(new
    {
        totalItems = allItems.Count,
        totalSetItems = setItems.Count,
        totalSkills = skills.Count,
        categories,
        cashItems = allItems.Count(i => i.IsCash),
        snItems = allItems.Count(i => i.Sn is not null),
    });
    return 0;
}

// ========== 辅助方法 ==========

static void PrintGlobalHelp()
{
    Console.WriteLine("mapleidb — MapleItemDB 命令行测试工具");
    Console.WriteLine();
    Console.WriteLine("用法: mapleidb <command> [options]");
    Console.WriteLine();
    Console.WriteLine("子命令:");
    Console.WriteLine("  extract   WZ 数据提取并写入数据库");
    Console.WriteLine("  search    道具查询 (支持全部筛选条件)");
    Console.WriteLine("  skill     技能查询");
    Console.WriteLine("  get       按 ID 查询道具详情");
    Console.WriteLine("  stats     数据库统计");
    Console.WriteLine();
    Console.WriteLine("全局选项:");
    Console.WriteLine($"  --db <path>  数据库路径 (默认 {CliConfig.DefaultDbPath})");
    Console.WriteLine("  -h, --help   显示帮助");
    Console.WriteLine();
    Console.WriteLine("输出格式: JSON (blob 字段输出为 true/false 表示是否有值)");
    Console.WriteLine();
    Console.WriteLine("示例:");
    Console.WriteLine("  mapleidb extract --wz D:\\GAME\\MapleStory228\\Data");
    Console.WriteLine("  mapleidb search 阿比斯 --category Equip --min-level 200");
    Console.WriteLine("  mapleidb skill 终极攻击 --limit 10");
    Console.WriteLine("  mapleidb get 1572000");
    Console.WriteLine("  mapleidb stats");
}

static int PrintUnknownCommand(string command)
{
    Console.Error.WriteLine($"未知命令: {command}");
    Console.Error.WriteLine("使用 --help 查看可用命令。");
    return 1;
}

static string NextArg(string[] args, ref int index, string paramName)
{
    if (index + 1 >= args.Length)
        throw new ArgumentException($"参数 {paramName} 缺少值。");
    return args[++index];
}

static void PrintJson<T>(T obj) =>
    Console.WriteLine(JsonSerializer.Serialize(obj, CliConfig.JsonOptions));

static ServiceProvider BuildServiceProvider(string? dbPath)
{
    var services = new ServiceCollection();
    ServiceRegistration.Configure(
        services,
        Path.GetFullPath(dbPath ?? CliConfig.DefaultDbPath),
        logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.None);
        });
    return services.BuildServiceProvider();
}

static async Task EnsureDatabaseAsync(ServiceProvider provider)
{
    var bootstrapper = provider.GetRequiredService<DatabaseBootstrapper>();
    await bootstrapper.EnsureCreatedAsync();
}

// ========== 配置与自定义转换器 ==========

static class CliConfig
{
    public const string DefaultWzDir = @"D:\GAME\MapleStory228\Data";
    public const string DefaultDbPath = @"D:\GAME\MapleStory228\Data\mapleitemdb.db";

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new BlobHasValueConverter(), new JsonStringEnumConverter() },
    };
}

/// <summary>
/// 将 byte[] 序列化为 bool (true=有数据, false=无数据)，
/// 用于 CLI JSON 输出中跳过 blob 二进制内容。
/// </summary>
sealed class BlobHasValueConverter : JsonConverter<byte[]>
{
    public override bool HandleNull => true;

    public override byte[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException();

    public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options)
        => writer.WriteBooleanValue(value is { Length: > 0 });
}
